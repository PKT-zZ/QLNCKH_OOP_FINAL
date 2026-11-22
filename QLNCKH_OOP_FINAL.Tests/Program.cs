using QLNCKH_OOP_FINAL.Business.Models;
using QLNCKH_OOP_FINAL.Business.Services;
using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Data;

// Bộ kiểm thử không dùng NuGet để chạy được ngay cả khi máy không có mạng.
// Mỗi ca tạo kho dữ liệu mới nên không phụ thuộc thứ tự chạy.
ATests.Run();

internal static class ATests
{
    private static readonly CurrentUserContext Officer = new(UserRole.Officer);
    private static readonly CurrentUserContext Researcher = new(UserRole.Researcher, "R001");

    public static void Run()
    {
        var cases = new (string Code, string Name, Action Run)[]
        {
            ("A-T01", "Email nhà nghiên cứu không được trùng", DuplicateEmail),
            ("A-T02", "Tên đề tài không được rỗng", BlankTitle),
            ("A-T03", "Ngày kết thúc không trước ngày bắt đầu", InvalidDates),
            ("A-T04", "Không gán cùng người hai lần", DuplicateMember),
            ("A-T05", "Không duyệt đề tài thiếu chủ nhiệm", MissingLeader),
            ("A-T06", "Không gán chủ nhiệm thứ hai", SecondLeader),
            ("A-T07", "Duyệt đúng trạng thái và lịch sử", ValidApproval),
            ("A-T08", "Không duyệt hai lần", InvalidTransition),
            ("A-T09", "Số tiền phân bổ phải dương", InvalidAmount),
            ("A-T10", "Nhiều đợt cùng nguồn được cộng đúng", MultipleAllocations),
            ("A-PERM", "Service tự kiểm tra vai trò", ServiceAuthorization),
            ("A-BRIDGE", "Hoàn thành qua bằng chứng do B cung cấp", CompletionBridge),
            ("A-SOURCE", "Strategy cộng kinh phí theo nguồn", FundingBySource),
            ("A-VALID", "Dữ liệu sai không làm thay đổi đối tượng", InvalidInputDoesNotMutate),
            ("A-ID-PROJECT-SPACE", "Mã đề tài trùng có khoảng trắng giữ nguyên hồ sơ", () => RejectDuplicateProject(" P003 ")),
            ("A-ID-PROJECT-CASE", "Mã đề tài trùng khác hoa thường giữ nguyên hồ sơ", () => RejectDuplicateProject("p003")),
            ("A-ID-PROJECT-NEW", "Mã đề tài mới được loại khoảng trắng và giữ nguyên hoa thường", AcceptTrimmedProject),
            ("A-ID-FUND-SAME", "Mã phân bổ trùng trong cùng đề tài giữ nguyên tổng", () => RejectDuplicateAllocation(" A004 ", "P003")),
            ("A-ID-FUND-CROSS", "Mã phân bổ trùng có khoảng trắng ở đề tài khác giữ nguyên tổng", () => RejectDuplicateAllocation(" A004 ", "P004")),
            ("A-ID-FUND-CASE", "Mã phân bổ khác hoa thường ở đề tài khác giữ nguyên tổng", () => RejectDuplicateAllocation("a004", "P004")),
            ("A-ID-FUND-NEW", "Hai đợt hợp lệ cùng nguồn được chuẩn hóa và cộng đúng", AcceptDistinctAllocations),
            ("A-ID-SOURCE-SPACE", "Mã nguồn trùng có khoảng trắng giữ nguyên hồ sơ", RejectDuplicateSource),
            ("A-REVIEW-DEPARTMENT", "Cập nhật mã đơn vị có khoảng trắng giữ nguyên ngày thành lập", UpdateDepartmentWithPaddedId),
            ("A-REVIEW-RESEARCHER", "Cập nhật mã nhà nghiên cứu có khoảng trắng giữ nguyên hồ sơ", UpdateResearcherWithPaddedId),
            ("A-REVIEW-FUND-RANGE", "Từ chối khoản cấp làm tổng kinh phí vượt phạm vi decimal", RejectFundingOverflow),
            ("A-REVIEW-STATE", "Gọi sai đối tượng State không làm đổi trạng thái hoặc lịch sử", RejectMismatchedState)
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
        Console.WriteLine($"Kết quả: {cases.Length - failures}/{cases.Length} ca đạt.");
        Environment.ExitCode = failures == 0 ? 0 : 1;
    }

    // Service nhận repository qua giao diện như lúc chạy thật; mỗi test có dữ liệu riêng.
    private static (InMemoryResearchRepository Data, PeopleService People, ProjectService Projects, FundingService Funding) Setup()
    {
        var data = new InMemoryResearchRepository(); 
        data.Save(new Department("D01", "Khoa CNTT", new DateOnly(2000, 1, 1))); 
        data.Save(new Researcher("R001", "D01", "Nguyễn An", "an@university.edu.vn")); 
        data.Save(new Researcher("R002", "D01", "Trần Bình", "binh@university.edu.vn")); 
        data.AddSeed(new ProjectCategory("C01", "Cấp trường")); 
        data.Save(new FundingSource("F01", "Quỹ trường", "Nội bộ"));
        return (data, new PeopleService(data), new ProjectService(data, data), new FundingService(data));
    }

    private static ResearchProject NewProject(ProjectService service, string id = "P001") =>
        service.ProposeProject(Researcher, id, "C01", "Nghiên cứu dữ liệu", new DateOnly(2026, 11, 6), null);

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

    private static void DuplicateEmail()
    {
        var x = Setup();
        MustThrow<InvalidOperationException>(() => x.People.SaveResearcher(Officer, "R004", "D01", "Người mới", "AN@university.edu.vn"));
    }

    private static void BlankTitle()
    {
        var x = Setup();
        MustThrow<ArgumentException>(() => x.Projects.ProposeProject(Researcher, "P001", "C01", " ", new DateOnly(2026, 11, 6), null));
    }

    private static void InvalidDates()
    {
        var x = Setup();
        MustThrow<ArgumentException>(() => x.Projects.ProposeProject(Researcher, "P001", "C01", "Đề tài", new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 5)));
    }

    private static void DuplicateMember()
    {
        var x = Setup();
        NewProject(x.Projects);
        x.Projects.AssignMember(Officer, "P001", "R001", ProjectRole.Member);
        MustThrow<InvalidOperationException>(() => x.Projects.AssignMember(Officer, "P001", "r001", ProjectRole.Member));
    }

    private static void MissingLeader()
    {
        var x = Setup();
        NewProject(x.Projects);
        MustThrow<InvalidOperationException>(() => x.Projects.Approve(Officer, "P001"));
    }

    private static void SecondLeader()
    {
        var x = Setup();
        NewProject(x.Projects);
        x.Projects.AssignMember(Officer, "P001", "R001", ProjectRole.Leader);
        MustThrow<InvalidOperationException>(() => x.Projects.AssignMember(Officer, "P001", "R002", ProjectRole.Leader));
    }

    private static void ValidApproval()
    {
        var x = Setup();
        var project = NewProject(x.Projects);
        x.Projects.AssignMember(Officer, "P001", "R001", ProjectRole.Leader);
        x.Projects.Approve(Officer, "P001");
        Check(project.Status == ProjectStatus.Active, "Đề tài chưa chuyển sang Active.");
        Check(project.History.Count == 1 && project.History[0].FromStatus == ProjectStatus.Pending &&
              project.History[0].ToStatus == ProjectStatus.Active, "Lịch sử trạng thái không đúng.");
    }

    private static void InvalidTransition()
    {
        var x = Setup();
        NewProject(x.Projects);
        x.Projects.AssignMember(Officer, "P001", "R001", ProjectRole.Leader);
        x.Projects.Approve(Officer, "P001");
        MustThrow<InvalidOperationException>(() => x.Projects.Approve(Officer, "P001"));
    }

    private static void InvalidAmount()
    {
        var x = Setup();
        NewProject(x.Projects);
        MustThrow<ArgumentOutOfRangeException>(() => x.Funding.Allocate(Officer, "A001", "P001", "F01", 0, new DateOnly(2026, 11, 6)));
        MustThrow<ArgumentOutOfRangeException>(() => x.Funding.Allocate(Officer, "A002", "P001", "F01", -1, new DateOnly(2026, 11, 6)));
    }

    private static void MultipleAllocations()
    {
        var x = Setup();
        NewProject(x.Projects);
        x.Funding.Allocate(Officer, "A001", "P001", "F01", 250_000m, new DateOnly(2026, 11, 6));
        x.Funding.Allocate(Officer, "A002", "P001", "F01", 150_000m, new DateOnly(2026, 11, 7));
        Check(x.Funding.TotalForProject("P001") == 400_000m, "Tổng tiền cấp không đúng.");
        var report = new FundingByProjectStrategy(x.Data).BuildRows();
        Check(report.Count == 1 && report[0].Value == 400_000m, "Strategy báo cáo sai tổng tiền.");
    }

    private static void ServiceAuthorization()
    {
        var x = Setup();
        // Gọi thẳng từng thao tác UC01–04 để chứng minh quyền được kiểm trong service.
        MustThrow<UnauthorizedAccessException>(() => x.Projects.ProposeProject(Officer, "P001", "C01", "Đề tài", new DateOnly(2026, 11, 6), null));
        MustThrow<UnauthorizedAccessException>(() => x.People.SaveDepartment(Researcher, "D03", "Khoa mới", new DateOnly(2026, 11, 6)));
        MustThrow<UnauthorizedAccessException>(() => x.People.SaveResearcher(Researcher, "R004", "D01", "Người mới", "new@uni.edu.vn"));
        MustThrow<UnauthorizedAccessException>(() => x.People.GetHeadHistory(Researcher, "D01"));
        NewProject(x.Projects);
        MustThrow<UnauthorizedAccessException>(() => x.Projects.AssignMember(Researcher, "P001", "R001", ProjectRole.Leader));
        MustThrow<UnauthorizedAccessException>(() => x.Projects.Approve(Researcher, "P001"));
        MustThrow<UnauthorizedAccessException>(() => x.Funding.SaveSource(Researcher, "F02", "Nguồn mới", "Trường"));
        MustThrow<UnauthorizedAccessException>(() => x.Funding.Allocate(Researcher, "A001", "P001", "F01", 100m, new DateOnly(2026, 11, 6)));
    }

    // Giả lập adapter của B để kiểm hợp đồng hoàn thành trước khi hai module được ghép.
    private sealed class FakeCompletionEvidence(bool scoredCouncil, bool researchOutput) : ICompletionEvidence
    {
        public bool HasScoredCouncil(string projectId) => scoredCouncil;
        public bool HasResearchOutput(string projectId) => researchOutput;
    }

    private static void CompletionBridge()
    {
        var x = Setup();
        var project = NewProject(x.Projects);
        x.Projects.AssignMember(Officer, "P001", "R001", ProjectRole.Leader);
        x.Projects.Approve(Officer, "P001");

        // Service phải từ chối nếu chỉ có một trong hai điều kiện nghiệm thu.
        MustThrow<InvalidOperationException>(() => x.Projects.Complete(Officer, "P001", new FakeCompletionEvidence(false, true)));
        MustThrow<InvalidOperationException>(() => x.Projects.Complete(Officer, "P001", new FakeCompletionEvidence(true, false)));
        Check(project.Status == ProjectStatus.Active, "Đề tài đổi trạng thái dù chưa đủ bằng chứng.");

        MustThrow<UnauthorizedAccessException>(() => x.Projects.Complete(Researcher, "P001", new FakeCompletionEvidence(true, true)));
        x.Projects.Complete(Officer, "P001", new FakeCompletionEvidence(true, true));
        Check(project.Status == ProjectStatus.Completed && project.History.Count == 2 &&
              project.History[1].FromStatus == ProjectStatus.Active &&
              project.History[1].ToStatus == ProjectStatus.Completed,
            "Hoàn thành không cập nhật trạng thái và lịch sử đúng.");
        MustThrow<InvalidOperationException>(() => x.Projects.Complete(Officer, "P001", new FakeCompletionEvidence(true, true)));
    }

    private static void FundingBySource()
    {
        var x = Setup();
        NewProject(x.Projects);
        NewProject(x.Projects, "P002");
        x.Funding.SaveSource(Officer, "F02", "Nguồn doanh nghiệp", "Bên ngoài");

        // Hai đợt từ F01 ở hai đề tài phải gộp, F02 vẫn là dòng riêng.
        x.Funding.Allocate(Officer, "A001", "P001", "F01", 250_000m, new DateOnly(2026, 11, 6));
        x.Funding.Allocate(Officer, "A002", "P002", "F01", 150_000m, new DateOnly(2026, 11, 7));
        x.Funding.Allocate(Officer, "A003", "P001", "F02", 80_000m, new DateOnly(2026, 11, 8));

        IReportStrategy report = new FundingBySourceStrategy(x.Data);
        var rows = report.BuildRows();
        Check(report.Title == "Tổng kinh phí theo nguồn", "Tiêu đề báo cáo theo nguồn không đúng.");
        Check(rows.Count == 2 && rows[0].Key == "F01" && rows[0].Value == 400_000m &&
              rows[0].Detail == "2 đợt cấp" && rows[1].Key == "F02" && rows[1].Value == 80_000m,
            "Strategy chưa cộng đúng theo nguồn hoặc thứ tự dòng.");
    }

    private static void InvalidInputDoesNotMutate()
    {
        var x = Setup();
        var project = NewProject(x.Projects);
        MustThrow<ArgumentOutOfRangeException>(() =>
            x.Projects.AssignMember(Officer, "P001", "R001", (ProjectRole)999));
        Check(project.Members.Count == 0, "Vai trò không hợp lệ vẫn được lưu vào đề tài.");

        MustThrow<ArgumentException>(() =>
            x.People.SaveResearcher(Officer, "R004", "D01", "Người mới", " "));
        Check(x.Data.FindResearcher("R004") is null, "Email rỗng vẫn tạo nhà nghiên cứu.");
    }

    // Giữ một đề tài đã duyệt để phát hiện cả việc thay đối tượng lẫn mất trạng thái.
    private static void RejectDuplicateProject(string duplicateId)
    {
        var x = Setup();
        var original = NewProject(x.Projects, "P003");
        x.Projects.AssignMember(Officer, "P003", "R001", ProjectRole.Leader);
        x.Projects.Approve(Officer, "P003");
        var previousTitle = original.Title;

        var rejected = false;
        try
        {
            x.Projects.ProposeProject(Researcher, duplicateId, "C01", "Tên thay thế", new DateOnly(2026, 11, 6), null);
        }
        catch (InvalidOperationException ex)
        {
            Check(ex.Message == "Mã đề tài đã tồn tại.", ex.Message);
            rejected = true;
        }

        var stored = x.Data.FindProject("P003");
        Check(x.Data.Projects.Count == 1 && ReferenceEquals(stored, original) &&
              original.Title == previousTitle && original.Status == ProjectStatus.Active &&
              original.Members.Count == 1 && original.History.Count == 1,
            $"Hồ sơ P003 trước: {previousTitle}, Active; sau: {stored?.Title}, {stored?.Status}; số đề tài: {x.Data.Projects.Count}.");
        Check(rejected, "Đề xuất có mã trùng phải bị từ chối.");
    }

    private static void AcceptTrimmedProject()
    {
        var x = Setup();
        var created = NewProject(x.Projects, " p003 ");
        Check(created.Id == "p003" && created.Status == ProjectStatus.Pending &&
              x.Data.Projects.Count == 1 && ReferenceEquals(x.Data.FindProject("P003"), created),
            "Đề tài mới phải dùng mã đã loại khoảng trắng và được lưu đúng một lần.");
    }

    // Hai đề tài giúp kiểm tra tính duy nhất của mã phân bổ trên toàn kho.
    private static void RejectDuplicateAllocation(string duplicateId, string targetProjectId)
    {
        var x = Setup();
        var first = NewProject(x.Projects, "P003");
        var second = NewProject(x.Projects, "P004");
        var day = new DateOnly(2026, 11, 7);
        var original = x.Funding.Allocate(Officer, "A004", first.Id, "F01", 250_000m, day);

        var rejected = false;
        try
        {
            x.Funding.Allocate(Officer, duplicateId, targetProjectId, "F01", 150_000m, day);
        }
        catch (InvalidOperationException ex)
        {
            Check(ex.Message == "Mã phân bổ đã tồn tại." ||
                  ex.Message == "Mã phân bổ đã tồn tại trong đề tài.", ex.Message);
            rejected = true;
        }

        Check(first.Allocations.Count == 1 && ReferenceEquals(first.Allocations[0], original) &&
              first.TotalFunding == 250_000m && second.Allocations.Count == 0 && second.TotalFunding == 0m,
            $"Tổng trước 250000/0, sau {first.TotalFunding}/{second.TotalFunding}; mã P003: {string.Join(", ", first.Allocations.Select(a => a.Id))}; mã P004: {string.Join(", ", second.Allocations.Select(a => a.Id))}.");
        Check(rejected, "Phân bổ có mã trùng phải bị từ chối.");
    }

    private static void AcceptDistinctAllocations()
    {
        var x = Setup();
        var project = NewProject(x.Projects, "P003");
        var day = new DateOnly(2026, 11, 7);
        var first = x.Funding.Allocate(Officer, " A004 ", project.Id, "F01", 250_000m, day);
        var second = x.Funding.Allocate(Officer, " a005 ", project.Id, "F01", 150_000m, day);
        Check(first.Id == "A004" && second.Id == "a005" &&
              project.Allocations.SequenceEqual(new[] { first, second }) && project.TotalFunding == 400_000m,
            "Hai mã mới phải được chuẩn hóa và tổng kinh phí phải bằng 400000.");
    }

    private static void RejectDuplicateSource()
    {
        var x = Setup();
        var original = x.Data.FindSource("F01");
        var rejected = false;
        try
        {
            x.Funding.SaveSource(Officer, " F01 ", "Tên thay thế", "Tài trợ ngoài");
        }
        catch (InvalidOperationException ex)
        {
            Check(ex.Message == "Mã nguồn kinh phí đã tồn tại.", ex.Message);
            rejected = true;
        }

        var stored = x.Data.FindSource("F01");
        Check(x.Data.Sources.Count == 1 && ReferenceEquals(stored, original) && stored?.Name == "Quỹ trường",
            $"Nguồn F01 trước: Quỹ trường; sau: {stored?.Name}; số nguồn: {x.Data.Sources.Count}.");
        Check(rejected, "Nguồn kinh phí có mã trùng phải bị từ chối.");
    }


    private static void UpdateDepartmentWithPaddedId()
    {
        var x = Setup();
        var original = x.Data.FindDepartment("D01")!;
        var createdAt = original.CreatedAt;

        var updated = x.People.SaveDepartment(Officer, " d01 ", "Tên mới", createdAt.AddYears(1));

        Check(ReferenceEquals(original, updated) && ReferenceEquals(updated, x.Data.FindDepartment("D01")) &&
              updated.Name == "Tên mới" && updated.CreatedAt == createdAt && x.Data.Departments.Count == 1,
            $"D01 trước: {createdAt:yyyy-MM-dd}; sau: {updated.CreatedAt:yyyy-MM-dd}; cùng hồ sơ: {ReferenceEquals(original, updated)}.");
    }

    private static void UpdateResearcherWithPaddedId()
    {
        var x = Setup();
        var original = x.Data.FindResearcher("R001")!;

        var updated = x.People.SaveResearcher(Officer, " r001 ", "D01", "Tên mới", "moi@university.edu.vn",
            academicTitle: "Tiến sĩ", phone: "0123", organization: "Khoa CNTT", isInstructor: true);

        Check(ReferenceEquals(original, updated) && ReferenceEquals(updated, x.Data.FindResearcher("R001")) &&
              updated.Id == "R001" && updated.FullName == "Tên mới" && updated.IsInstructor &&
              updated.AcademicTitle == "Tiến sĩ" && updated.Phone == "0123" &&
              updated.Organization == "Khoa CNTT" && x.Data.Researchers.Count == 2,
            $"R001 trước: {original.Id}; sau: {updated.Id}; cùng hồ sơ: {ReferenceEquals(original, updated)}.");

        var sameEmail = x.People.SaveResearcher(Officer, " r001 ", "D01", "Tên khác", updated.Email,
            updated.AcademicTitle, updated.Phone, updated.Organization, updated.IsInstructor);
        Check(ReferenceEquals(updated, sameEmail) && sameEmail.FullName == "Tên khác",
            "Email của chính hồ sơ phải được chấp nhận.");
    }

    private static void RejectFundingOverflow()
    {
        foreach (var secondProjectId in new[] { "P001", "P002" })
        {
            var x = Setup();
            var first = NewProject(x.Projects, "P001");
            var second = NewProject(x.Projects, "P002");
            var day = new DateOnly(2026, 11, 7);

            // decimal.MaxValue vẫn là số tiền hợp lệ khi chưa có khoản cấp nào.
            var original = x.Funding.Allocate(Officer, "A001", "P001", "F01", decimal.MaxValue, day);
            var rejected = false;
            try
            {
                x.Funding.Allocate(Officer, "A002", secondProjectId, "F01", 1m, day);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            Check(first.Allocations.Count == 1 && second.Allocations.Count == 0 &&
                  ReferenceEquals(first.Allocations[0], original) && first.TotalFunding == decimal.MaxValue &&
                  second.TotalFunding == 0m,
                $"Sau yêu cầu tới {secondProjectId}: số khoản P001/P002 là {first.Allocations.Count}/{second.Allocations.Count}, trước đó là 1/0.");
            Check(rejected, "Khoản cấp làm tổng không biểu diễn được phải bị từ chối.");
        }
    }


    private static void RejectMismatchedState()
    {
        var x = Setup();
        var project = NewProject(x.Projects);
        x.Projects.AssignMember(Officer, project.Id, "R001", ProjectRole.Leader);
        var at = DateTimeOffset.Now;

        var rejected = false;
        try { new ActiveState().Complete(project, "OFFICER", at); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected && project.Status == ProjectStatus.Pending && project.History.Count == 0,
            $"Gọi ActiveState trên Pending cho trạng thái {project.Status}, lịch sử có {project.History.Count} bản ghi.");

        x.Projects.Approve(Officer, project.Id);
        var approval = project.History[0];
        MustThrow<InvalidOperationException>(() => new PendingState().Approve(project, "OFFICER", at));
        Check(project.Status == ProjectStatus.Active && project.History.Count == 1 && project.History[0] == approval,
            "Duyệt trực tiếp qua PendingState không được thay lịch sử của đề tài đã duyệt.");
    }

}