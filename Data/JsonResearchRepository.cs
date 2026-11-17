using System.Text.Json;
using System.Text.Json.Serialization;
using QLNCKH_OOP_FINAL.Business.Models;

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

    //Service gọi Save sau khi sửa; lớp này cập nhật bộ nhớ rồi ghi toàn bộ snapshot
    public override void Save(Department value) { base.Save(value); Persist(); }
    public override void Save(Researcher value) { base.Save(value); Persist(); }
    public override void Save(ResearchProject value) { base.Save(value); Persist(); }
    public override void Save(FundingSource value) { base.Save(value); Persist(); }

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
    }

    //Ghi vào file tạm trước, rồi thay file chính để hạn chế dữ liệu bị ghi dở
    private void Persist()
    {
        var snapshot = new AppSnapshot
        {
            Departments = Departments.ToList(),
            Researchers = Researchers.ToList(),
            HeadTerms = HeadTerms.ToList(),
            Categories = Categories.ToList(),
            Projects = Projects.Select(ProjectData.FromModel).ToList(),
            Sources = Sources.ToList(),
            OtherModules = _otherModules
        };
        var tempPath = _dataPath + ".tmp";
        try
        {
            File.WriteAllText(tempPath, JsonSerializer.Serialize(snapshot, JsonOptions));
            File.Move(tempPath, _dataPath, true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
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
        public List<Department> Departments { get; set; } = [];
        public List<Researcher> Researchers { get; set; } = [];
        public List<DepartmentHeadTerm> HeadTerms { get; set; } = [];
        public List<ProjectCategory> Categories { get; set; } = [];
        public List<ProjectData> Projects { get; set; } = [];
        public List<FundingSource> Sources { get; set; } = [];
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
}