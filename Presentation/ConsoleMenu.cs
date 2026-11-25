using System.Globalization;
using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;
using QLNCKH_OOP_FINAL.Business.Services;
using QLNCKH_OOP_FINAL.Data;

namespace QLNCKH_OOP_FINAL.Presentation;

//Giao dien dong lenh: chi nhap/xuat, quy tac dc goi qua service
public sealed class ConsoleMenu
{
    private readonly JsonResearchRepository _data = new();
    private readonly PeopleService _people;
    private readonly ProjectService _projects;
    private readonly FundingService _funding;
    private readonly EvaluationService _evaluation;
    private readonly OutputService _outputs;
    private readonly ProjectLookupService _lookup;
    private readonly ReportService _reports = new();
    private CurrentUserContext _user = new(UserRole.Officer);

    public ConsoleMenu()
    {
        _people  = new PeopleService(_data);
        _projects = new ProjectService(_data, _data);
        _funding = new FundingService(_data);
        _evaluation = new EvaluationService(_data, _data, _data);
        _outputs = new OutputService(_data, _data, _data);
        _lookup = new ProjectLookupService(_data, _data);
    }

    //Vòng lặp menu; bắt lỗi dự kiến để người dùng sửa dữ liệu nhập và thử lại
    public void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║        HỆ THỐNG QUẢN LÝ NGHIÊN CỨU KHOA HỌC          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        Console.WriteLine("Dữ liệu được lưu vào data/nckh.json sau mỗi thao tác thành công.");
        while (true)
        {
            PrintMenu();
            var choice = Console.ReadLine()?.Trim();
            if (choice == "0" || choice is null) return;
            try { Dispatch(choice); }
            // Save có thể đã thay đổi bộ nhớ trước khi ghi JSON thất bại.
            // Không tiếp tục phiên với trạng thái dữ liệu không còn đáng tin cậy.
            catch (DataPersistenceException e)
            {
                Console.Error.WriteLine($"[Lỗi nghiêm trọng] {e.Message}");
                Console.Error.WriteLine("Phiên làm việc đã dừng. Kiểm tra file JSON và quyền ghi rồi mở lại ứng dụng.");
                Environment.ExitCode = 1;
                return;
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException
                or UnauthorizedAccessException or FormatException
                or IOException or InvalidDataException)
            { Console.WriteLine($"[Lỗi] {e.Message}"); }
        }
    }

    private void PrintMenu()
    {
        var roleLabel = _user.Role switch
        {
            UserRole.Officer      => "Chuyên viên",
            UserRole.Researcher   => $"Nhà nghiên cứu [{_user.ResearcherId}]",
            UserRole.CouncilChair => $"Chủ tịch hội đồng [{_user.ResearcherId}]",
            _                     => _user.Role.ToString()
        };
        Console.WriteLine($"\nVai trò hiện tại: {roleLabel}");
        Console.WriteLine("─────────────────────────────────────────────────────");
        Console.WriteLine("  [Nhân sự & Đơn vị]");
        Console.WriteLine("   1  Xem danh sách đề tài         2  Đổi vai trò");
        Console.WriteLine("   3  Lưu/cập nhật đơn vị          4  Lưu/cập nhật nhà nghiên cứu");
        Console.WriteLine("  10  Xem lịch sử lãnh đạo đơn vị");
        Console.WriteLine("  [Đề tài & Phê duyệt]");
        Console.WriteLine("   5  Đề xuất đề tài mới           6  Gán thành viên/chủ nhiệm");
        Console.WriteLine("   7  Phê duyệt đề tài");
        Console.WriteLine("  [Kinh phí]");
        Console.WriteLine("   8  Thêm nguồn kinh phí          9  Phân bổ kinh phí");
        Console.WriteLine("  [Nghiệm thu & Đầu ra]");
        Console.WriteLine("  13  Lập hội đồng nghiệm thu     14  Gán thành viên hội đồng");
        Console.WriteLine("  15  Ghi điểm nghiệm thu         16  Hoàn thành đề tài");
        Console.WriteLine("  17  Ghi bài báo                 18  Ghi sản phẩm bàn giao");
        Console.WriteLine("  [Báo cáo & Tra cứu]");
        Console.WriteLine("  11  Báo cáo kinh phí theo đề tài");
        Console.WriteLine("  12  Báo cáo kinh phí theo nguồn tài trợ");
        Console.WriteLine("  19  Báo cáo đầu ra theo đề tài  20  Báo cáo đầu ra theo loại");
        Console.WriteLine("  21  Tra cứu đề tài (mã/trạng thái)");
        Console.WriteLine("─────────────────────────────────────────────────────");
        Console.WriteLine("   0  Thoát");
        Console.Write("Chọn: ");
    }

    //lựa chọn menu sang use case, không tự cài quy tắc nghiệp vụ tại đây
    private void Dispatch(string choice)
    {
        switch (choice)
        {
            // Xem tổng quan đề tài
            case "1":
                var projects = _data.Projects.OrderBy(p => p.Id).ToList();
                if (!projects.Any()) { Console.WriteLine("Chưa có đề tài nào trong hệ thống."); break; }
                Console.WriteLine($"\n{"Mã",-6} {"Tên đề tài",-42} {"Trạng thái",-10} {"SL thành viên",-14} {"Tổng kinh phí",14}");
                Console.WriteLine(new string('─', 92));
                foreach (var p in projects)
                    Console.WriteLine($"{p.Id,-6} {Truncate(p.Title, 42),-42} {p.Status,-10} {p.Members.Count,-14} {p.TotalFunding,14:N0} đ");
                break;

            // Đổi vai trò
            case "2":
                Console.WriteLine("  1 = Chuyên viên (Officer)");
                Console.WriteLine("  2 = Nhà nghiên cứu (Researcher)");
                Console.WriteLine("  3 = Chủ tịch hội đồng (CouncilChair)");
                var roleChoice = Read("Chọn vai trò: ");
                if (roleChoice == "1")
                {
                    _user = new(UserRole.Officer);
                    Console.WriteLine("Đã chuyển sang vai trò Chuyên viên.");
                }
                else if (roleChoice == "2")
                {
                    var rid = Read("Nhập mã nhà nghiên cứu (R001/R002/R003/R004): ");
                    _user = new(UserRole.Researcher, rid.ToUpperInvariant());
                    Console.WriteLine($"Đã chuyển sang vai trò Nhà nghiên cứu [{_user.ResearcherId}].");
                }
                else if (roleChoice == "3")
                {
                    var rid = Read("Nhập mã nhà nghiên cứu (mã Chủ tịch): ");
                    _user = new(UserRole.CouncilChair, rid.ToUpperInvariant());
                    Console.WriteLine($"Đã chuyển sang vai trò Chủ tịch hội đồng [{_user.ResearcherId}].");
                }
                else throw new ArgumentException("Lựa chọn vai trò không hợp lệ.");
                break;

            // UC01: Lưu đơn vị
            case "3":
                _people.SaveDepartment(_user, Read("Mã đơn vị: "), Read("Tên đơn vị: "),
                    ReadDate("Ngày thành lập (yyyy-MM-dd): "));
                Console.WriteLine("Đã lưu thông tin đơn vị.");
                break;

            // UC01: Lưu nhà nghiên cứu 
            case "4":
                var researcherId = Read("Mã nhà nghiên cứu: ");
                var existingResearcher = _data.FindResearcher(researcherId);
                // Khi chỉ sửa thông tin cơ bản, giữ nguyên học hàm/học vị và cờ giảng viên.
                _people.SaveResearcher(_user, researcherId, Read("Mã đơn vị: "),
                    Read("Họ và tên: "), Read("Email: "),
                    academicTitle: existingResearcher?.AcademicTitle,
                    phone: existingResearcher?.Phone,
                    organization: existingResearcher?.Organization,
                    isInstructor: existingResearcher?.IsInstructor ?? false);
                Console.WriteLine("Đã lưu thông tin nhà nghiên cứu.");
                break;

            // UC02: Đề xuất đề tài 
            case "5":
                var newProject = _projects.ProposeProject(_user,
                    Read("Mã đề tài mới: "),
                    Read("Mã loại đề tài (C01 = Cấp trường, C02 = Cấp bộ): "),
                    Read("Tên đề tài: "),
                    ReadDate("Ngày bắt đầu (yyyy-MM-dd): "),
                    ReadOptionalDate("Ngày kết thúc dự kiến (yyyy-MM-dd, Enter để bỏ qua): "));
                Console.WriteLine($"Đã tạo đề tài [{newProject.Id}] ở trạng thái [{newProject.Status}].");
                break;

            // UC03: Gán thành viên
            case "6":
                _projects.AssignMember(_user,
                    Read("Mã đề tài: "),
                    Read("Mã nhà nghiên cứu: "),
                    Read("Vai trò: 1 = Chủ nhiệm, 2 = Thành viên: ") switch
                    {
                        "1" => ProjectRole.Leader,
                        "2" => ProjectRole.Member,
                        _   => throw new ArgumentException("Vai trò trong đề tài không hợp lệ.")
                    });
                Console.WriteLine("Đã gán thành viên vào đề tài.");
                break;

            // UC03: Phê duyệt đề tài
            case "7":
                var approveId = Read("Mã đề tài cần phê duyệt: ");
                _projects.Approve(_user, approveId);
                Console.WriteLine($"Đề tài [{approveId}] đã được phê duyệt và chuyển sang trạng thái Active.");
                break;

            // UC04: Thêm nguồn kinh phí
            case "8":
                _funding.SaveSource(_user, Read("Mã nguồn: "), Read("Tên nguồn: "), Read("Loại nguồn (Nội bộ / Ngân sách nhà nước / Tài trợ ngoài): "));
                Console.WriteLine("Đã thêm nguồn kinh phí.");
                break;

            // UC04: Phân bổ kinh phí
            case "9":
                var allocId = Read("Mã đợt phân bổ: ");
                var allocProjId = Read("Mã đề tài: ");
                var allocSourceId = Read("Mã nguồn kinh phí (F01/F02): ");
                var allocAmount = ReadPositiveDecimal("Số tiền (VND): ");
                var allocDate = ReadDate("Ngày phân bổ (yyyy-MM-dd): ");
                _funding.Allocate(_user, allocId, allocProjId, allocSourceId, allocAmount, allocDate);
                Console.WriteLine($"Đã phân bổ kinh phí. Tổng kinh phí đề tài [{allocProjId}]: {_funding.TotalForProject(allocProjId):N0} đ");
                break;

            // UC01: Xem lịch sử lãnh đạo
            case "10":
                var deptId = Read("Mã đơn vị: ");
                var history = _people.GetHeadHistory(_user, deptId).ToList();
                if (!history.Any()) { Console.WriteLine("Đơn vị này chưa có lịch sử lãnh đạo."); break; }
                Console.WriteLine($"\nLịch sử lãnh đạo đơn vị [{deptId}]:");
                foreach (var h in history)
                    Console.WriteLine($"  {h.ResearcherId}: {h.From:yyyy-MM-dd} → {(h.To is null ? "nay" : h.To.Value.ToString("yyyy-MM-dd"))}");
                break;

            // Báo cáo kinh phí theo đề tài (Strategy Pattern)
            case "11":
                PrintReport(new FundingByProjectStrategy(_data));
                break;

            // Báo cáo kinh phí theo nguồn
            case "12":
                PrintReport(new FundingBySourceStrategy(_data));
                break;

            // UC05: Lập hội đồng nghiệm thu
            case "13":
                var councilId = Read("Mã hội đồng: ");
                var council = _evaluation.CreateCouncil(_user, councilId, Read("Mã đề tài: "),
                    ReadDate("Ngày nghiệm thu (yyyy-MM-dd): "));
                Console.WriteLine($"Đã lập hội đồng [{council.Id}] cho đề tài [{council.ProjectId}].");
                break;

            // UC05: Gán thành viên hội đồng
            case "14":
                _evaluation.AssignMember(_user, Read("Mã hội đồng: "), Read("Mã nhà nghiên cứu: "),
                    Read("Vai trò: 1 = Chủ tịch, 2 = Ủy viên: ") switch
                    {
                        "1" => CouncilRole.Chairman,
                        "2" => CouncilRole.Member,
                        _   => throw new ArgumentException("Vai trò trong hội đồng không hợp lệ.")
                    });
                Console.WriteLine("Đã gán thành viên vào hội đồng.");
                break;

            // UC06: Chủ tịch ghi điểm
            case "15":
                _evaluation.RecordScore(_user, Read("Mã hội đồng: "), ReadScore("Điểm nghiệm thu (0-10): "));
                Console.WriteLine("Đã ghi điểm nghiệm thu.");
                break;

            // UC06: Chuyên viên hoàn thành đề tài, bằng chứng do phân hệ B cung cấp
            case "16":
                var completeId = Read("Mã đề tài cần hoàn thành: ");
                _projects.Complete(_user, completeId, new ResultCompletionEvidence(_data));
                Console.WriteLine($"Đề tài [{completeId}] đã hoàn thành.");
                break;

            // UC07: Ghi bài báo (PaperCreator)
            case "17":
                var paperId = Read("Mã bài báo: ");
                var paperTitle = Read("Tên bài báo: ");
                var paperProject = Read("Mã đề tài liên kết: ");
                var doi = Read("DOI (Enter để bỏ qua): ");
                var publisher = Read("Nhà xuất bản/tạp chí: ");
                var year = ReadInt("Năm xuất bản: ");
                var authorCount = ReadInt("Số tác giả: ");
                var authors = new List<AuthorDraft>();
                for (var i = 1; i <= authorCount; i++)
                    authors.Add(new AuthorDraft(Read($"  Tác giả {i} - mã nhà nghiên cứu: "),
                        ReadInt($"  Tác giả {i} - thứ tự: "),
                        Read($"  Tác giả {i} - là tác giả liên hệ? (y/n): ").Equals("y", StringComparison.OrdinalIgnoreCase)));
                var paper = _outputs.AddPaper(_user, new PaperDraft(paperId, paperTitle, paperProject,
                    doi.Length == 0 ? null : doi, publisher, year, authors));
                Console.WriteLine($"Đã ghi {paper.GetDisplaySummary()}.");
                break;

            // UC07: Ghi sản phẩm bàn giao (DeliverableCreator)
            case "18":
                var deliverable = _outputs.AddDeliverable(_user, new DeliverableDraft(
                    Read("Mã sản phẩm: "), Read("Tên sản phẩm: "), Read("Mã đề tài: "),
                    Read("Mã loại (DT01 = Phần mềm, DT02 = Báo cáo kỹ thuật): "), ReadInt("Số lượng: ")));
                Console.WriteLine($"Đã ghi {deliverable.GetDisplaySummary()}.");
                break;

            // UC08: Báo cáo đầu ra (Strategy Pattern)
            case "19":
                PrintReport(new OutputByProjectStrategy(_data, _data));
                break;

            case "20":
                PrintReport(new OutputByTypeStrategy(_data));
                break;

            // UC08: Tra cứu đề tài theo mã hoặc trạng thái
            case "21":
                var lookupId = Read("Mã đề tài (Enter = tất cả): ");
                var lookupStatus = Read("Trạng thái Pending/Active/Completed (Enter = tất cả): ");
                ProjectStatus? statusFilter = lookupStatus.Length == 0 ? null
                    : Enum.TryParse<ProjectStatus>(lookupStatus, true, out var parsed) && Enum.IsDefined(parsed) ? parsed
                    : throw new ArgumentException("Trạng thái không hợp lệ.");
                var found = _lookup.Search(_user, lookupId, statusFilter);
                if (found.Count == 0) { Console.WriteLine("Không có đề tài phù hợp."); break; }
                foreach (var item in found)
                {
                    Console.WriteLine($"\n[{item.Id}] {item.Title} | {item.Status} | kinh phí {item.TotalFunding:N0} đ | " +
                                      $"điểm hội đồng: {(item.CouncilScore is null ? "chưa có" : item.CouncilScore.Value.ToString("0.##", CultureInfo.InvariantCulture))}");
                    if (item.Outputs.Count == 0) Console.WriteLine("    (chưa có đầu ra)");
                    foreach (var output in item.Outputs) Console.WriteLine($"    - {output}");
                }
                break;

            default:
                Console.WriteLine("Lựa chọn không hợp lệ. Vui lòng chọn lại.");
                break;
        }
    }

    //Strategy Pattern: menu ko biet thuat toan tinh ben trong
    private void PrintReport(IReportStrategy strategy)
    {
        // Kiểm quyền ở service, không gọi BuildRows() trực tiếp từ Presentation.
        var rows = _reports.Generate(_user, strategy);
        Console.WriteLine($"\n══ {strategy.Title.ToUpperInvariant()} ══");
        if (!rows.Any()) { Console.WriteLine("  Chưa có dữ liệu để tổng hợp."); return; }
        Console.WriteLine($"  {"Mã",-8} {"Tên",-38} {"Tổng",18} {"Ghi chú"}");
        Console.WriteLine("  " + new string('─', 72));
        foreach (var row in rows)
            Console.WriteLine($"  {row.Key,-8} {Truncate(row.Label, 38),-38} {row.Value,14:N0} {row.Unit}  {row.Detail}");
        Console.WriteLine("  " + new string('─', 72));
        // Chỉ cộng tổng khi mọi dòng cùng đơn vị (đ, đầu ra...); báo cáo theo loại có đơn vị khác nhau thì bỏ dòng tổng.
        var units = rows.Select(r => r.Unit).Distinct().ToList();
        if (units.Count == 1)
            Console.WriteLine($"  {"TỔNG CỘNG",-47} {rows.Sum(r => r.Value),14:N0} {units[0]}");
    }

    private static string Read(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine()?.Trim() ?? throw new InvalidOperationException("Không có dữ liệu nhập.");
    }

    private static decimal ReadPositiveDecimal(string prompt)
    {
        while (true)
        {
            var input = Read(prompt);
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                && amount > 0)
                return amount;

            Console.WriteLine("[Lỗi] Số tiền phải là số dương hợp lệ trong phạm vi kiểu decimal. Vui lòng nhập lại.");
        }
    }

    private static int ReadInt(string prompt) =>
        int.Parse(Read(prompt), NumberStyles.Integer, CultureInfo.InvariantCulture);

    //chap nhan ca dau cham va dau phay thap phan (8.5 hoac 8,5); khoang [0,10] do EvaluationCouncil kiem
    private static decimal ReadScore(string prompt) =>
        decimal.Parse(Read(prompt).Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);

    private static DateOnly ReadDate(string prompt) =>
        DateOnly.ParseExact(Read(prompt), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly? ReadOptionalDate(string prompt)
    {
        var value = Read(prompt);
        return value.Length == 0 ? null : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";
}