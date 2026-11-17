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
    private CurrentUserContext _user = new(UserRole.Officer);

    public ConsoleMenu()
    {
        _people  = new PeopleService(_data);
        _projects = new ProjectService(_data, _data);
        _funding = new FundingService(_data);
    }

    //Vòng lặp menu; bắt lỗi dự kiến để người dùng sửa dữ liệu nhập và thử lại
    public void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║   HỆ THỐNG QUẢN LÝ NGHIÊN CỨU KHOA HỌC — PHẦN A   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        Console.WriteLine("Dữ liệu được lưu vào data/nckh.json sau mỗi thao tác thành công.");
        while (true)
        {
            PrintMenu();
            var choice = Console.ReadLine()?.Trim();
            if (choice == "0" || choice is null) return;
            try { Dispatch(choice); }
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
        Console.WriteLine("  [Báo cáo]");
        Console.WriteLine("  11  Báo cáo kinh phí theo đề tài");
        Console.WriteLine("  12  Báo cáo kinh phí theo nguồn tài trợ");
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
                _people.SaveResearcher(_user, Read("Mã nhà nghiên cứu: "), Read("Mã đơn vị: "),
                    Read("Họ và tên: "), Read("Email: "));
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
                var allocAmount = decimal.Parse(Read("Số tiền (VND): "), CultureInfo.InvariantCulture);
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

            default:
                Console.WriteLine("Lựa chọn không hợp lệ. Vui lòng chọn lại.");
                break;
        }
    }

    //Strategy Pattern: menu ko biet thuat toan tinh ben trong
    private static void PrintReport(IReportStrategy strategy)
    {
        var rows = strategy.BuildRows();
        Console.WriteLine($"\n══ {strategy.Title.ToUpperInvariant()} ══");
        if (!rows.Any()) { Console.WriteLine("  Chưa có dữ liệu để tổng hợp."); return; }
        Console.WriteLine($"  {"Mã",-8} {"Tên",-38} {"Tổng",18} {"Ghi chú"}");
        Console.WriteLine("  " + new string('─', 72));
        foreach (var row in rows)
            Console.WriteLine($"  {row.Key,-8} {Truncate(row.Label, 38),-38} {row.Value,14:N0} {row.Unit}  {row.Detail}");
        Console.WriteLine("  " + new string('─', 72));
        Console.WriteLine($"  {"TỔNG CỘNG",-47} {rows.Sum(r => r.Value),14:N0} đ");
    }

    private static string Read(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine()?.Trim() ?? throw new InvalidOperationException("Không có dữ liệu nhập.");
    }

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