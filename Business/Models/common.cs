namespace QLNCKH_OOP_FINAL.Business.Models;

//kiem tra dlieu co ban dung chung khi tao va sua thuc the
public static class Guard
{
    public static string Required(string? value, string name) => !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new ArgumentException($"{name} không được rỗng.", name);

    public static void DateRange(DateOnly start, DateOnly? end)
    {
        if (end < start) throw new ArgumentException("Ngày kết thúc phải từ ngày bắt đầu trở đi.");
    }
}
//vai tro cua ho so dang dung
public enum UserRole { Researcher, Officer, CouncilChair }
//vong doi cua de tai
public enum ProjectStatus { Pending, Active, Completed }
//vai tro cua researcher trong de tai
public enum ProjectRole { Leader, Member }
//kiem tra quyen
public sealed record CurrentUserContext(UserRole Role, string? ResearcherId = null);