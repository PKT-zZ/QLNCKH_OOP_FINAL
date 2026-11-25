using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;
using QLNCKH_OOP_FINAL.Business.Services;
using QLNCKH_OOP_FINAL.Data;

// Bộ kiểm thử phân hệ B (B-T11..B-T20 + ca kiểm quyền/pattern). Không dùng NuGet, mỗi ca có kho dữ liệu riêng.
internal static class BTests
{
    private static readonly CurrentUserContext Officer = new(UserRole.Officer);
    private static readonly CurrentUserContext Leader = new(UserRole.Researcher, "R001");   // chủ nhiệm P001
    private static readonly CurrentUserContext Outsider = new(UserRole.Researcher, "R003"); // không thuộc P001
    private static readonly CurrentUserContext Chair = new(UserRole.CouncilChair, "R003");  // Chủ tịch hội đồng trong test

    public static void Run()
    {
        var cases = new List<(string Code, string Name, Action Run)>
        {
            ("B-T11", "Lập hội đồng hợp lệ thành công", ValidCouncil),
            ("B-T12", "Gán Chairman thứ hai bị từ chối", SecondChairman),
            ("B-T13", "Một nhà nghiên cứu lặp trong hội đồng bị từ chối", DuplicateCouncilMember),
            ("B-ROSTER-LOCK", "Điểm 0 cũng khóa danh sách hội đồng", LockCouncilRosterAtFirstScore),
            ("B-T14a", "Điểm -0.1 bị từ chối", () => RejectScore(-0.1m)),
            ("B-T14b", "Điểm 10.1 bị từ chối", () => RejectScore(10.1m)),
            ("B-T14c", "Điểm biên 0 và 10 được chấp nhận", BoundaryScores),
            ("B-T15", "Người không phải Chairman của hội đồng ghi điểm bị từ chối", NonChairmanScore),
            ("B-T16", "Hội đồng thứ hai cho cùng đề tài bị từ chối", SecondCouncilSameProject),
            ("B-T17a", "Hoàn thành đề tài chưa có đầu ra bị từ chối", CompleteWithoutOutput),
            ("B-T17b", "Hoàn thành đề tài chưa có điểm bị từ chối", CompleteWithoutScore),
            ("B-T18a", "Thứ tự tác giả trùng bị từ chối", DuplicateAuthorOrder),
            ("B-T18b", "Hai tác giả liên hệ bị từ chối", TwoCorrespondingAuthors),
            ("B-T18c", "DOI trùng (khác hoa thường) bị từ chối", DuplicateDoi),
            ("B-T18d", "Bài báo không có tác giả liên hệ bị từ chối", NoCorrespondingAuthor),
            ("B-T19a", "Số lượng sản phẩm 0 bị từ chối", () => RejectQuantity(0)),
            ("B-T19b", "Số lượng sản phẩm -1 bị từ chối", () => RejectQuantity(-1)),
            ("B-T20", "Báo cáo sau luồng liên thông đúng tổng tiền và danh sách đầu ra", EndToEndReport),
            ("B-PERM", "Service tự kiểm tra vai trò UC05-08", ServiceAuthorization),
            ("B-FACTORY", "Hai creator tạo hai lớp con, đa hình GetDisplaySummary", FactoryMethod),
            ("B-PAPER-SAVE", "Ghi bài báo thất bại không tạo bài hoặc liên kết riêng lẻ", RejectFailedPaperSave),
            ("B-STRATEGY-TYPE", "Strategy theo loại gom đúng bài báo và sản phẩm", OutputByType),
            ("B-STRATEGY-TYPE-RANGE", "Tổng sản phẩm theo loại có thể vượt giới hạn int", LargeOutputQuantityTotal),
            ("B-LOOKUP", "Tra cứu theo mã và trạng thái", Lookup)
        };

        var failures = 0;
        foreach (var (code, name, run) in cases)
        {
            try
            {
                run();
                Console.WriteLine($"PASS {code}: {name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine($"FAIL {code}: {name} — {ex.Message}");
            }
        }
        Console.WriteLine($"Kết quả B: {cases.Count - failures}/{cases.Count} ca đạt.");
        if (failures > 0) Environment.ExitCode = 1;
    }

    private sealed record Ctx(InMemoryResearchRepository Data, ProjectService Projects, FundingService Funding,
        EvaluationService Evaluation, OutputService Outputs);

    // P001 đang Active, chủ nhiệm R001, thành viên R002. R003, R004 ngoài đề tài (dùng cho hội đồng).
    private static Ctx Setup(InMemoryResearchRepository? data = null)
    {
        data ??= new InMemoryResearchRepository();
        data.Save(new Department("D01", "Khoa CNTT", new DateOnly(2000, 1, 1)));
        data.Save(new Researcher("R001", "D01", "Nguyễn An", "an@university.edu.vn"));
        data.Save(new Researcher("R002", "D01", "Trần Bình", "binh@university.edu.vn"));
        data.Save(new Researcher("R003", "D01", "Lê Cường", "cuong@university.edu.vn"));
        data.Save(new Researcher("R004", "D01", "Phạm Hương", "huong@university.edu.vn"));
        data.AddSeed(new ProjectCategory("C01", "Cấp trường"));
        data.AddSeed(new DeliverableType("DT01", "Phần mềm"));
        data.AddSeed(new DeliverableType("DT02", "Báo cáo kỹ thuật"));
        data.Save(new FundingSource("F01", "Quỹ trường", "Nội bộ"));
        var projects = new ProjectService(data, data);
        projects.ProposeProject(Leader, "P001", "C01", "Nghiên cứu dữ liệu", new DateOnly(2026, 11, 6), null);
        projects.AssignMember(Officer, "P001", "R001", ProjectRole.Leader);
        projects.AssignMember(Officer, "P001", "R002", ProjectRole.Member);
        projects.Approve(Officer, "P001");
        return new Ctx(data, projects, new FundingService(data),
            new EvaluationService(data, data, data), new OutputService(data, data, data));
    }

    // Mô phỏng kho từ chối thao tác ghi cả cặp để phát hiện nếu service quay lại hai lần Save.
    private sealed class FailingPaperRepository : InMemoryResearchRepository
    {
        public override void SavePaperWithLink(Paper paper, string projectId) =>
            throw new DataPersistenceException("Lỗi ghi giả lập.");
    }

    private static void RejectFailedPaperSave()
    {
        var x = Setup(new FailingPaperRepository());
        MustThrow<DataPersistenceException>(() => x.Outputs.AddPaper(Leader, Paper()));
        Check(x.Data.Papers.Count == 0 && x.Data.PaperLinks.Count == 0,
            "Lỗi ghi bài báo đã để lại dữ liệu dở dang.");
    }

    private static EvaluationCouncil NewCouncil(Ctx x, string id = "HD01")
    {
        var council = x.Evaluation.CreateCouncil(Officer, id, "P001", new DateOnly(2026, 11, 20));
        x.Evaluation.AssignMember(Officer, id, "R003", CouncilRole.Chairman);
        x.Evaluation.AssignMember(Officer, id, "R004", CouncilRole.Member);
        return council;
    }

    private static PaperDraft Paper(string id = "PA01", string? doi = "10.1/abc", params AuthorDraft[] authors) =>
        new(id, "Bài báo thử", "P001", doi, "Tạp chí thử", 2026,
            authors.Length > 0 ? authors : [new AuthorDraft("R001", 1, true), new AuthorDraft("R002", 2, false)]);

    private static DeliverableDraft Deliverable(string id = "SP01", int quantity = 1, string typeId = "DT01") =>
        new(id, "Phần mềm thử", "P001", typeId, quantity);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void MustThrow<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (Exception ex) when (ex is T) { return; }
        throw new Exception($"Đã chờ ngoại lệ {typeof(T).Name} nhưng thao tác vẫn thành công.");
    }

    private static void ValidCouncil()
    {
        var x = Setup();
        var council = NewCouncil(x);
        Check(ReferenceEquals(x.Data.FindCouncil("HD01"), council) && ReferenceEquals(x.Data.FindCouncilByProject("P001"), council),
            "Hội đồng chưa được lưu theo mã và theo đề tài.");
        Check(council.ChairmanId == "R003" && council.Members.Count == 2 && council.Score is null,
            "Chủ tịch/thành viên/điểm ban đầu không đúng.");
    }

    private static void SecondChairman()
    {
        var x = Setup();
        var council = NewCouncil(x);
        MustThrow<InvalidOperationException>(() => x.Evaluation.AssignMember(Officer, "HD01", "R002", CouncilRole.Chairman));
        Check(council.Members.Count == 2 && council.ChairmanId == "R003", "Chairman thứ hai vẫn được lưu.");
    }

    private static void DuplicateCouncilMember()
    {
        var x = Setup();
        var council = NewCouncil(x);
        MustThrow<InvalidOperationException>(() => x.Evaluation.AssignMember(Officer, "HD01", "r004", CouncilRole.Member));
        MustThrow<InvalidOperationException>(() => x.Evaluation.AssignMember(Officer, "HD01", "R004", CouncilRole.Chairman));
        Check(council.Members.Count == 2, "Thành viên lặp vẫn được lưu.");
    }

    private static void LockCouncilRosterAtFirstScore()
    {
        var x = Setup();
        var council = NewCouncil(x);
        x.Evaluation.RecordScore(Chair, "HD01", 0m);
        MustThrow<InvalidOperationException>(() => x.Evaluation.AssignMember(Officer, "HD01", "R002", CouncilRole.Member));
        Check(council.Members.Count == 2 && council.Members.All(m => m.ResearcherId != "R002"),
            "Danh sách hội đồng bị thay đổi sau khi chấm điểm.");
    }

    private static void RejectScore(decimal score)
    {
        var x = Setup();
        var council = NewCouncil(x);
        MustThrow<ArgumentOutOfRangeException>(() => x.Evaluation.RecordScore(Chair, "HD01", score));
        Check(council.Score is null, "Điểm sai vẫn bị ghi vào hội đồng.");
    }

    private static void BoundaryScores()
    {
        var x = Setup();
        var council = NewCouncil(x);
        x.Evaluation.RecordScore(Chair, "HD01", 0m);
        Check(council.Score == 0m, "Điểm 0 chưa được ghi.");
        x.Evaluation.RecordScore(Chair, "HD01", 10m);
        Check(council.Score == 10m, "Điểm 10 chưa được ghi.");
    }

    private static void NonChairmanScore()
    {
        var x = Setup();
        var council = NewCouncil(x);
        MustThrow<UnauthorizedAccessException>(() => x.Evaluation.RecordScore(new(UserRole.CouncilChair, "R004"), "HD01", 8m)); // ủy viên
        MustThrow<UnauthorizedAccessException>(() => x.Evaluation.RecordScore(new(UserRole.CouncilChair, "R999"), "HD01", 8m)); // người lạ
        MustThrow<UnauthorizedAccessException>(() => x.Evaluation.RecordScore(new(UserRole.CouncilChair), "HD01", 8m));          // thiếu mã
        MustThrow<UnauthorizedAccessException>(() => x.Evaluation.RecordScore(Officer, "HD01", 8m));                             // sai vai trò
        Check(council.Score is null, "Người không phải Chairman vẫn ghi được điểm.");
    }

    private static void SecondCouncilSameProject()
    {
        var x = Setup();
        NewCouncil(x);
        MustThrow<InvalidOperationException>(() => x.Evaluation.CreateCouncil(Officer, "HD02", "P001", new DateOnly(2026, 11, 21)));
        Check(x.Data.Councils.Count == 1 && x.Data.FindCouncil("HD02") is null, "Hội đồng thứ hai vẫn được lưu.");
    }

    private static void CompleteWithoutOutput()
    {
        var x = Setup();
        NewCouncil(x);
        x.Evaluation.RecordScore(Chair, "HD01", 8.5m);
        MustThrow<InvalidOperationException>(() => x.Projects.Complete(Officer, "P001", new ResultCompletionEvidence(x.Data)));
        Check(x.Projects.RequiredProject("P001").Status == ProjectStatus.Active, "Đề tài đổi trạng thái dù chưa có đầu ra.");
    }

    private static void CompleteWithoutScore()
    {
        var x = Setup();
        NewCouncil(x);
        x.Outputs.AddDeliverable(Leader, Deliverable());
        MustThrow<InvalidOperationException>(() => x.Projects.Complete(Officer, "P001", new ResultCompletionEvidence(x.Data)));
        Check(x.Projects.RequiredProject("P001").Status == ProjectStatus.Active, "Đề tài đổi trạng thái dù chưa có điểm.");
    }

    private static void DuplicateAuthorOrder()
    {
        var x = Setup();
        MustThrow<InvalidOperationException>(() => x.Outputs.AddPaper(Leader,
            Paper(authors: [new AuthorDraft("R001", 1, true), new AuthorDraft("R002", 1, false)])));
        Check(x.Data.Papers.Count == 0 && x.Data.PaperLinks.Count == 0, "Bài báo sai thứ tự tác giả vẫn được lưu.");
    }

    private static void TwoCorrespondingAuthors()
    {
        var x = Setup();
        MustThrow<InvalidOperationException>(() => x.Outputs.AddPaper(Leader,
            Paper(authors: [new AuthorDraft("R001", 1, true), new AuthorDraft("R002", 2, true)])));
        Check(x.Data.Papers.Count == 0, "Bài báo có hai tác giả liên hệ vẫn được lưu.");
    }

    private static void DuplicateDoi()
    {
        var x = Setup();
        x.Outputs.AddPaper(Leader, Paper("PA01", "10.1/ABC"));
        MustThrow<InvalidOperationException>(() => x.Outputs.AddPaper(Leader, Paper("PA02", "10.1/abc")));
        Check(x.Data.Papers.Count == 1 && x.Data.FindPaper("PA02") is null, "Bài báo trùng DOI vẫn được lưu.");
        x.Outputs.AddPaper(Leader, Paper("PA03", null));   // không nhập DOI thì không bị coi là trùng
        x.Outputs.AddPaper(Leader, Paper("PA04", null));
        Check(x.Data.Papers.Count == 3, "Bài báo không DOI phải được chấp nhận.");
    }

    private static void NoCorrespondingAuthor()
    {
        var x = Setup();
        MustThrow<InvalidOperationException>(() => x.Outputs.AddPaper(Leader,
            Paper(authors: [new AuthorDraft("R001", 1, false), new AuthorDraft("R002", 2, false)])));
        Check(x.Data.Papers.Count == 0, "Bài báo thiếu tác giả liên hệ vẫn được lưu.");
    }

    private static void RejectQuantity(int quantity)
    {
        var x = Setup();
        MustThrow<ArgumentOutOfRangeException>(() => x.Outputs.AddDeliverable(Leader, Deliverable(quantity: quantity)));
        Check(x.Data.Deliverables.Count == 0, "Sản phẩm số lượng sai vẫn được lưu.");
    }

    // Luồng liên thông: kinh phí (A) + hội đồng/điểm/đầu ra (B) -> hoàn thành -> báo cáo.
    private static void EndToEndReport()
    {
        var x = Setup();
        x.Funding.Allocate(Officer, "A001", "P001", "F01", 250_000m, new DateOnly(2026, 11, 6));
        x.Funding.Allocate(Officer, "A002", "P001", "F01", 150_000m, new DateOnly(2026, 11, 7));
        NewCouncil(x);
        x.Evaluation.RecordScore(Chair, "HD01", 9m);
        x.Outputs.AddPaper(Leader, Paper());
        x.Outputs.AddDeliverable(Leader, Deliverable(quantity: 2));
        x.Projects.Complete(Officer, "P001", new ResultCompletionEvidence(x.Data));
        Check(x.Projects.RequiredProject("P001").Status == ProjectStatus.Completed, "Đề tài chưa Completed.");

        var reports = new ReportService();
        var funding = reports.Generate(Officer, new FundingByProjectStrategy(x.Data));
        Check(funding.Count == 1 && funding[0].Key == "P001" && funding[0].Value == 400_000m, "Báo cáo kinh phí sai tổng tiền.");

        var outputs = reports.Generate(Officer, new OutputByProjectStrategy(x.Data, x.Data));
        Check(outputs.Count == 1 && outputs[0].Key == "P001" && outputs[0].Value == 2 && outputs[0].Unit == "đầu ra",
            "Báo cáo đầu ra sai số lượng.");
        Check(outputs[0].Detail!.Contains("1 bài báo") && outputs[0].Detail!.Contains("1 sản phẩm") &&
              outputs[0].Detail!.Contains("Bài báo thử") && outputs[0].Detail!.Contains("Phần mềm thử"),
            $"Báo cáo đầu ra thiếu danh sách đầu ra: {outputs[0].Detail}");
        // Sau khi hoàn thành không ghi thêm đầu ra hay sửa điểm được nữa
        MustThrow<InvalidOperationException>(() => x.Outputs.AddDeliverable(Leader, Deliverable("SP02")));
        MustThrow<InvalidOperationException>(() => x.Evaluation.RecordScore(Chair, "HD01", 5m));
    }

    private static void ServiceAuthorization()
    {
        var x = Setup();
        // UC05: chỉ Chuyên viên
        MustThrow<UnauthorizedAccessException>(() => x.Evaluation.CreateCouncil(Leader, "HD01", "P001", new DateOnly(2026, 11, 20)));
        NewCouncil(x, "HD02");
        MustThrow<UnauthorizedAccessException>(() => x.Evaluation.AssignMember(Leader, "HD02", "R002", CouncilRole.Member));
        // UC06: chỉ CouncilChair ghi điểm; chỉ Chuyên viên hoàn thành
        MustThrow<UnauthorizedAccessException>(() => x.Projects.Complete(Leader, "P001", new ResultCompletionEvidence(x.Data)));
        // UC07: chỉ nhà nghiên cứu, phải tham gia đề tài, bài báo phải có tên mình
        MustThrow<UnauthorizedAccessException>(() => x.Outputs.AddDeliverable(Officer, Deliverable()));
        MustThrow<UnauthorizedAccessException>(() => x.Outputs.AddDeliverable(Outsider, Deliverable()));
        MustThrow<UnauthorizedAccessException>(() => x.Outputs.AddPaper(Outsider, Paper()));
        MustThrow<UnauthorizedAccessException>(() => x.Outputs.AddPaper(new(UserRole.Researcher, "R002"),
            Paper(authors: [new AuthorDraft("R001", 1, true)])));   // R002 thuộc đề tài nhưng không có tên tác giả
        // UC08: chỉ Chuyên viên xem báo cáo và tra cứu
        MustThrow<UnauthorizedAccessException>(() => new ReportService().Generate(Leader, new OutputByTypeStrategy(x.Data)));
        MustThrow<UnauthorizedAccessException>(() => new ProjectLookupService(x.Data, x.Data).Search(Leader));
        Check(x.Data.Papers.Count == 0 && x.Data.Deliverables.Count == 0, "Thao tác sai vai trò vẫn tạo dữ liệu.");
    }

    private static void FactoryMethod()
    {
        ResearchOutputCreator paperCreator = new PaperCreator();
        ResearchOutputCreator deliverableCreator = new DeliverableCreator();
        var paper = paperCreator.Build(Paper());
        var item = deliverableCreator.Build(Deliverable());
        Check(paper is Paper && item is ProjectDeliverable, "Creator tạo sai lớp con.");
        Check(paper.GetDisplaySummary().StartsWith("Bài báo") && item.GetDisplaySummary().StartsWith("Sản phẩm"),
            "GetDisplaySummary không đa hình theo lớp con.");
        MustThrow<ArgumentException>(() => paperCreator.Build(Deliverable()));      // sai creator cho dữ liệu
        MustThrow<ArgumentOutOfRangeException>(() => deliverableCreator.Build(Deliverable(quantity: 0)));
    }

    private static void OutputByType()
    {
        var x = Setup();
        x.Outputs.AddPaper(Leader, Paper("PA01", "10.1/a"));
        x.Outputs.AddPaper(Leader, Paper("PA02", "10.1/b"));
        x.Outputs.AddDeliverable(Leader, Deliverable("SP01", 2, "DT01"));
        x.Outputs.AddDeliverable(Leader, Deliverable("SP02", 3, "DT01"));
        x.Outputs.AddDeliverable(Leader, Deliverable("SP03", 1, "DT02"));
        IReportStrategy strategy = new OutputByTypeStrategy(x.Data);
        var rows = strategy.BuildRows();
        Check(strategy.Title == "Kết quả đầu ra theo loại", "Tiêu đề báo cáo theo loại không đúng.");
        Check(rows.Count == 3 && rows[0].Key == "PAPER" && rows[0].Value == 2 &&
              rows[1].Key == "DT01" && rows[1].Value == 5 && rows[1].Detail == "2 lần ghi nhận" &&
              rows[2].Key == "DT02" && rows[2].Value == 1,
            "Strategy chưa gom đúng theo loại.");
    }

    private static void LargeOutputQuantityTotal()
    {
        var x = Setup();
        x.Outputs.AddDeliverable(Leader, Deliverable("SP01", int.MaxValue));
        x.Outputs.AddDeliverable(Leader, Deliverable("SP02", 1));
        var row = new OutputByTypeStrategy(x.Data).BuildRows().Single(r => r.Key == "DT01");
        Check(row.Value == 2147483648m && row.Detail == "2 lần ghi nhận",
            "Tổng số lượng hoặc số lần ghi nhận sai khi tổng vượt giới hạn int.");
    }

    private static void Lookup()
    {
        var x = Setup();
        x.Projects.ProposeProject(Leader, "P002", "C01", "Đề tài chờ duyệt", new DateOnly(2026, 11, 6), null);
        var lookup = new ProjectLookupService(x.Data, x.Data);
        Check(lookup.Search(Officer).Count == 2, "Không lọc phải trả cả hai đề tài.");
        Check(lookup.Search(Officer, "p001").Count == 1, "Lọc theo mã không phân biệt hoa thường.");
        var pending = lookup.Search(Officer, status: ProjectStatus.Pending);
        Check(pending.Count == 1 && pending[0].Id == "P002", "Lọc theo trạng thái Pending sai.");
    }
}
