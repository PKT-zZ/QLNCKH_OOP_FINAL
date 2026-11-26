using System.Text.Json;
using System.Text.Json.Serialization;
using QLNCKH_OOP_FINAL.Business.Models;
using QLNCKH_OOP_FINAL.Business.Services;

namespace QLNCKH_OOP_FINAL.Data;

//nạp dữ liệu từ JSON vào bộ nhớ và ghi lại sau mỗi thay đổi thành công
public sealed class JsonResearchRepository : InMemoryResearchRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _dataPath;
    private readonly Dictionary<string, JsonElement>? _otherModules;
    private bool _persistenceFailed;

    //Đọc cấu hình cạnh file chạy, tạo dữ liệu từ seed nếu mở ứng dụng lần đầu
    public JsonResearchRepository()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config", "appsettings.json");
        if (!File.Exists(configPath)) throw new FileNotFoundException("Thiếu file cấu hình ứng dụng.", configPath);
        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(configPath), JsonOptions)
            ?? throw new InvalidDataException("File cấu hình rỗng.");
        _dataPath = RelativeToApp(settings.DataFile);
        var seedPath = RelativeToApp(settings.SeedFile);
        Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
        if (!File.Exists(_dataPath))
        {
            if (!File.Exists(seedPath)) throw new FileNotFoundException("Thiếu dữ liệu mẫu.", seedPath);
            File.Copy(seedPath, _dataPath);
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<AppSnapshot>(File.ReadAllText(_dataPath), JsonOptions)
                ?? throw new InvalidDataException("File dữ liệu rỗng.");
            Load(snapshot);
            _otherModules = snapshot.OtherModules;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or NullReferenceException)
        {
            throw new InvalidDataException("File dữ liệu không hợp lệ; hãy kiểm tra hoặc khôi phục từ seed.json.", ex);
        }
    }

    // Nếu Persist lỗi, bộ nhớ có thể khác file. Cấm mọi Save tiếp theo trong phiên
    public override void Save(Department value) { EnsureWritable(); base.Save(value); Persist(); }
    public override void Save(Researcher value) { EnsureWritable(); base.Save(value); Persist(); }
    public override void Save(ResearchProject value) { EnsureWritable(); base.Save(value); Persist(); }
    public override void Save(FundingSource value) { EnsureWritable(); base.Save(value); Persist(); }
    public override void Save(EvaluationCouncil value) { EnsureWritable(); base.Save(value); Persist(); }
    public override void Save(Paper value) { EnsureWritable(); base.Save(value); Persist(); }
    public override void SavePaperWithLink(Paper paper, string projectId)
    {
        EnsureWritable();
        base.SavePaperWithLink(paper, projectId);
        try
        {
            // Một lần thay tệp chính cho cả bài báo và liên kết.
            Persist();
        }
        catch (DataPersistenceException)
        {
            UndoNewPaper(paper.Id);
            throw;
        }
    }
    public override void Save(ProjectDeliverable value) { EnsureWritable(); base.Save(value); Persist(); }
    public override void Save(ProjectPaperLink value) { EnsureWritable(); base.Save(value); Persist(); }

    private void EnsureWritable()
    {
        if (_persistenceFailed)
            throw new DataPersistenceException("Phiên lưu dữ liệu trước đó đã thất bại; phải khởi động lại ứng dụng.");
    }

    //Ghép đường dẫn với thư mục ứng dụng để không phụ thuộc nơi gọi lệnh
    private static string RelativeToApp(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath) || Path.IsPathRooted(configuredPath))
            throw new InvalidDataException("Đường dẫn dữ liệu trong cấu hình phải là đường dẫn tương đối.");
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }

    //Khôi phục theo thứ tự quan hệ và từ chối tham chiếu không tồn tại
    private void Load(AppSnapshot snapshot)
    {
        ValidateSnapshot(snapshot);
        foreach (var department in snapshot.Departments) base.Save(department);
        foreach (var researcher in snapshot.Researchers)
        {
            if (FindDepartment(researcher.DepartmentId) is null)
                throw new InvalidDataException("Nhà nghiên cứu tham chiếu đơn vị không tồn tại.");
            base.Save(researcher);
        }
        foreach (var head in snapshot.HeadTerms)
        {
            if (FindDepartment(head.DepartmentId) is null || FindResearcher(head.ResearcherId) is null)
                throw new InvalidDataException("Nhiệm kỳ lãnh đạo tham chiếu sai.");
            AddSeed(head);
        }
        foreach (var category in snapshot.Categories) AddSeed(category);
        foreach (var source in snapshot.Sources) base.Save(source);
        foreach (var dto in snapshot.Projects)
        {
            if (FindCategory(dto.CategoryId) is null)
                throw new InvalidDataException("Đề tài tham chiếu loại không tồn tại.");
            var project = ResearchProject.Restore(dto.Id, dto.CategoryId, dto.Title, dto.StartDate,
                dto.EndDate, dto.Status, dto.Members, dto.Allocations, dto.History);
            if (project.Members.Any(m => FindResearcher(m.ResearcherId) is null) ||
                project.Allocations.Any(a => FindSource(a.SourceId) is null))
                throw new InvalidDataException("Đề tài có thành viên hoặc nguồn kinh phí không tồn tại.");
            base.Save(project);
        }
        LoadResults(snapshot);
        // Chỉ đối chiếu nghiệm thu khi đã nạp đủ hội đồng, bài báo và sản phẩm.
        var evidence = new ResultCompletionEvidence(this);
        foreach (var project in Projects.Where(p => p.Status == ProjectStatus.Completed))
        {
            if (!evidence.HasScoredCouncil(project.Id) || !evidence.HasResearchOutput(project.Id))
                throw new InvalidDataException($"Đề tài {project.Id} đã hoàn thành nhưng thiếu điểm hội đồng hoặc đầu ra.");
        }
        try
        {
            // Mọi khoản cấp đều dương. Tổng này bảo đảm các phép cộng ở cả hai báo cáo đều nằm trong miền decimal.
            _ = Projects.Sum(project => project.TotalFunding);
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException("Tổng kinh phí trong tệp dữ liệu vượt giới hạn decimal.", ex);
        }
    }

    //phan he B: nap sau de tai vi tham chieu de tai, nguoi, loai san pham
    private void LoadResults(AppSnapshot snapshot)
    {
        foreach (var type in snapshot.DeliverableTypes) AddSeed(type);
        foreach (var dto in snapshot.Councils)
        {
            var project = FindProject(dto.ProjectId);
            if (project is null)
                throw new InvalidDataException("Hội đồng tham chiếu đề tài không tồn tại.");
            EnsureStarted(project, "hội đồng");
            var council = EvaluationCouncil.Restore(dto.Id, dto.ProjectId, dto.EvaluationDate, dto.Score, dto.Members);
            if (council.Members.Any(m => FindResearcher(m.ResearcherId) is null))
                throw new InvalidDataException("Hội đồng có thành viên không tồn tại.");
            base.Save(council);
        }
        foreach (var dto in snapshot.Papers)
        {
            var paper = Paper.Restore(dto.Id, dto.Title, dto.Doi, dto.Publisher, dto.PublishedYear, dto.Authors);
            if (paper.Authors.Any(a => FindResearcher(a.ResearcherId) is null))
                throw new InvalidDataException("Bài báo có tác giả không tồn tại.");
            base.Save(paper);
        }
        var linkedPaperIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var link in snapshot.PaperLinks)
        {
            var project = FindProject(link.ProjectId);
            if (project is null || FindPaper(link.PaperId) is null)
                throw new InvalidDataException("Liên kết đề tài–bài báo tham chiếu sai.");
            EnsureStarted(project, "liên kết bài báo");
            base.Save(link);
            linkedPaperIds.Add(link.PaperId);
        }
        // Báo cáo theo loại và tra cứu đề tài phải dùng cùng các bài đã có liên kết.
        foreach (var paper in Papers)
        {
            if (!linkedPaperIds.Contains(paper.Id))
                throw new InvalidDataException($"Bài báo {paper.Id} chưa liên kết với đề tài.");
        }

        foreach (var dto in snapshot.Deliverables)
        {
            var project = FindProject(dto.ProjectId);
            if (project is null || FindDeliverableType(dto.TypeId) is null)
                throw new InvalidDataException("Sản phẩm tham chiếu đề tài hoặc loại không tồn tại.");
            EnsureStarted(project, "sản phẩm");
            var deliverable = new ProjectDeliverable(dto.Id, dto.ProjectId, dto.TypeId, dto.Title, dto.Quantity);
            deliverable.Validate();
            base.Save(deliverable);
        }
    }

    // Đề tài đã hoàn thành vẫn giữ hội đồng và đầu ra được tạo khi đang thực hiện.
    private static void EnsureStarted(ResearchProject project, string result)
    {
        if (project.Status == ProjectStatus.Pending)
            throw new InvalidDataException($"Đề tài {project.Id} đang chờ duyệt nên không thể có {result}.");
    }

    // Từ chối snapshot có mã/email trùng trước khi Load thay đổi bộ nhớ
    private static void ValidateSnapshot(AppSnapshot snapshot)
    {
        if (snapshot.Departments is null || snapshot.Researchers is null ||
            snapshot.HeadTerms is null || snapshot.Categories is null ||
            snapshot.Projects is null || snapshot.Sources is null)
            throw new InvalidDataException("Snapshot thiếu một hoặc nhiều danh sách bắt buộc.");

        if (snapshot.Departments.Any(x => x is null) ||
            snapshot.Researchers.Any(x => x is null) ||
            snapshot.HeadTerms.Any(x => x is null) ||
            snapshot.Categories.Any(x => x is null) ||
            snapshot.Sources.Any(x => x is null) ||
            snapshot.Projects.Any(x => x is null || x.Members is null ||
                                       x.Allocations is null || x.History is null))
            throw new InvalidDataException("Snapshot chứa bản ghi hoặc danh sách con null.");

        if (snapshot.Projects.Any(p => p.Members.Any(m => m is null) ||
                                       p.Allocations.Any(a => a is null) ||
                                       p.History.Any(h => h is null)))
            throw new InvalidDataException("Đề tài chứa bản ghi thành viên, phân bổ hoặc lịch sử null.");

        // So sánh không phân biệt hoa/thường, loại bỏ khoảng trắng đầu/cuối khi kiểm trùng
        RequireUnique(snapshot.Departments.Select(x => x.Id), "mã đơn vị");
        RequireUnique(snapshot.Researchers.Select(x => x.Id), "mã nhà nghiên cứu");
        RequireUnique(snapshot.Researchers.Select(x => x.Email), "email nhà nghiên cứu");
        RequireUnique(snapshot.HeadTerms.Select(x => x.Id), "mã nhiệm kỳ lãnh đạo");
        RequireUnique(snapshot.Categories.Select(x => x.Id), "mã loại đề tài");
        RequireUnique(snapshot.Sources.Select(x => x.Id), "mã nguồn kinh phí");
        RequireUnique(snapshot.Projects.Select(x => x.Id), "mã đề tài");

        // Mã phân bổ là duy nhất trong toàn hệ thống, không chỉ trong một đề tài
        RequireUnique(snapshot.Projects.SelectMany(p => p.Allocations.Select(a => a.Id)),
            "mã phân bổ kinh phí");

        // Phân hệ B: danh sách thiếu trong file cũ được hiểu là rỗng; phần tử null bị từ chối
        if (snapshot.DeliverableTypes is null || snapshot.Councils is null || snapshot.Papers is null ||
            snapshot.PaperLinks is null || snapshot.Deliverables is null)
            throw new InvalidDataException("Danh sách hội đồng và đầu ra phải là một mảng JSON.");
        if (snapshot.DeliverableTypes.Any(x => x is null) || snapshot.Councils.Any(x => x is null || x.Members is null) ||
            snapshot.Papers.Any(x => x is null || x.Authors is null) || snapshot.PaperLinks.Any(x => x is null) ||
            snapshot.Deliverables.Any(x => x is null))
            throw new InvalidDataException("Dữ liệu hội đồng, bài báo hoặc sản phẩm chứa bản ghi null.");
        RequireUnique(snapshot.DeliverableTypes.Select(x => x.Id), "mã loại sản phẩm");
        RequireUnique(snapshot.Councils.Select(x => x.Id), "mã hội đồng");
        // R8: mỗi đề tài tối đa một hội đồng
        RequireUnique(snapshot.Councils.Select(x => x.ProjectId), "đề tài có hội đồng nghiệm thu");
        RequireUnique(snapshot.Papers.Select(x => x.Id), "mã bài báo");
        // R9: DOI không trùng nếu có nhập
        RequireUnique(snapshot.Papers.Where(x => !string.IsNullOrWhiteSpace(x.Doi)).Select(x => x.Doi), "DOI bài báo");
        RequireUnique(snapshot.Deliverables.Select(x => x.Id), "mã sản phẩm bàn giao");
    }

    private static void RequireUnique(IEnumerable<string?> values, string label)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in values)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidDataException($"Dữ liệu có {label} rỗng.");
            if (!seen.Add(raw.Trim()))
                throw new InvalidDataException($"Dữ liệu có {label} trùng lặp: '{raw.Trim()}'.");
        }
    }

    // Ghi vào file tạm, thay file chính khi ghi xong. Nếu lỗi, dừng phiên ngay
    private void Persist()
    {
        var tempPath = _dataPath + ".tmp";
        try
        {
            var snapshot = new AppSnapshot
            {
                Departments = Departments.ToList(),
                Researchers = Researchers.ToList(),
                HeadTerms = HeadTerms.ToList(),
                Categories = Categories.ToList(),
                Projects = Projects.Select(ProjectData.FromModel).ToList(),
                Sources = Sources.ToList(),
                DeliverableTypes = DeliverableTypes.ToList(),
                Councils = Councils.Select(CouncilData.FromModel).ToList(),
                Papers = Papers.Select(PaperData.FromModel).ToList(),
                PaperLinks = PaperLinks.ToList(),
                Deliverables = Deliverables.Select(DeliverableData.FromModel).ToList(),
                OtherModules = _otherModules
            };
            File.WriteAllText(tempPath, JsonSerializer.Serialize(snapshot, JsonOptions));
            File.Move(tempPath, _dataPath, true);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _persistenceFailed = true;
            throw new DataPersistenceException(
                "Không ghi được JSON; trạng thái bộ nhớ có thể khác dữ liệu trên đĩa.", ex);
        }
    }

    //Hai đường dẫn tương đối trong file cấu hình
    public sealed class AppSettings
    {
        public string DataFile { get; set; } = "data/nckh.json";
        public string SeedFile { get; set; } = "data/seed.json";
    }

    public sealed class AppSnapshot
    {
        // Mảng rỗng là hợp lệ, nhưng thiếu khóa lõi là dấu hiệu tệp không đầy đủ.
        [JsonRequired] public List<Department> Departments { get; set; } = [];
        [JsonRequired] public List<Researcher> Researchers { get; set; } = [];
        [JsonRequired] public List<DepartmentHeadTerm> HeadTerms { get; set; } = [];
        [JsonRequired] public List<ProjectCategory> Categories { get; set; } = [];
        [JsonRequired] public List<ProjectData> Projects { get; set; } = [];
        [JsonRequired] public List<FundingSource> Sources { get; set; } = [];
        //phan he B
        public List<DeliverableType> DeliverableTypes { get; set; } = [];
        public List<CouncilData> Councils { get; set; } = [];
        public List<PaperData> Papers { get; set; } = [];
        public List<ProjectPaperLink> PaperLinks { get; set; } = [];
        public List<DeliverableData> Deliverables { get; set; } = [];
        [JsonExtensionData] public Dictionary<string, JsonElement>? OtherModules { get; set; }
    }

    //DTO của đề tài: tách dữ liệu lưu trữ khỏi các phương thức nghiệp vụ và State
    public sealed class ProjectData
    {
        public string Id { get; set; } = "";
        public string CategoryId { get; set; } = "";
        public string Title { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public ProjectStatus Status { get; set; }
        public List<ProjectParticipation> Members { get; set; } = [];
        public List<FundingAllocation> Allocations { get; set; } = [];
        public List<ProjectStatusHistory> History { get; set; } = [];

        public static ProjectData FromModel(ResearchProject project) => new()
        {
            Id = project.Id,
            CategoryId = project.CategoryId,
            Title = project.Title,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = project.Status,
            Members = project.Members.ToList(),
            Allocations = project.Allocations.ToList(),
            History = project.History.ToList()
        };
    }

    //DTO hoi dong: tach du lieu luu tru khoi logic R6/R7
    public sealed class CouncilData
    {
        public string Id { get; set; } = "";
        public string ProjectId { get; set; } = "";
        public DateOnly EvaluationDate { get; set; }
        public decimal? Score { get; set; }
        public List<CouncilMembership> Members { get; set; } = [];

        public static CouncilData FromModel(EvaluationCouncil council) => new()
        {
            Id = council.Id,
            ProjectId = council.ProjectId,
            EvaluationDate = council.EvaluationDate,
            Score = council.Score,
            Members = council.Members.ToList()
        };
    }

    //DTO bai bao kem danh sach tac gia
    public sealed class PaperData
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Doi { get; set; }
        public string Publisher { get; set; } = "";
        public int PublishedYear { get; set; }
        public List<PaperAuthor> Authors { get; set; } = [];

        public static PaperData FromModel(Paper paper) => new()
        {
            Id = paper.Id,
            Title = paper.Title,
            Doi = paper.Doi,
            Publisher = paper.Publisher,
            PublishedYear = paper.PublishedYear,
            Authors = paper.Authors.ToList()
        };
    }

    //DTO san pham ban giao
    public sealed class DeliverableData
    {
        public string Id { get; set; } = "";
        public string ProjectId { get; set; } = "";
        public string TypeId { get; set; } = "";
        public string Title { get; set; } = "";
        public int Quantity { get; set; }

        public static DeliverableData FromModel(ProjectDeliverable item) => new()
        {
            Id = item.Id,
            ProjectId = item.ProjectId,
            TypeId = item.TypeId,
            Title = item.Title,
            Quantity = item.Quantity
        };
    }
}

public sealed class DataPersistenceException : IOException
{
    public DataPersistenceException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
