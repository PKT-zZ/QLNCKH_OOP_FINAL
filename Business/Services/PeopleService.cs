using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

public sealed class PeopleService(IPeopleRepository people)
{
    public Department SaveDepartment(CurrentUserContext user, string id, string name, DateOnly createdAt)
    {
        Access.Require(user, UserRole.Officer);
        var normalizedId = Guard.Required(id, nameof(id));
        var department = people.FindDepartment(normalizedId);
        if (department is null) department = new Department(normalizedId, name, createdAt);
        else department.Rename(name);
        people.Save(department);
        return department;
    }

    public Researcher SaveResearcher(CurrentUserContext user, string id, string departmentId, string fullName,
        string email, string? academicTitle = null, string? phone = null, string? organization = null,
        bool isInstructor = false)
    {
        Access.Require(user, UserRole.Officer);
        if (people.FindDepartment(departmentId) is null)
            throw new InvalidOperationException("Đơn vị không tồn tại.");
        // Chuẩn hóa mã để bỏ qua đúng hồ sơ đang cập nhật khi kiểm tra email.
        var normalizedId = Guard.Required(id, nameof(id));
        var normalizedEmail = Guard.Required(email, nameof(email));
        if (people.Researchers.Any(r => !r.Id.Equals(normalizedId, StringComparison.OrdinalIgnoreCase) &&
                                        r.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Email đã được sử dụng.");
        var researcher = people.FindResearcher(normalizedId);
        if (researcher is null)
            researcher = new Researcher(normalizedId, departmentId, fullName, normalizedEmail, academicTitle, phone, organization, isInstructor);
        else
            researcher.Update(departmentId, fullName, normalizedEmail, academicTitle, phone, organization, isInstructor);
        people.Save(researcher);
        return researcher;
    }

    public IReadOnlyList<DepartmentHeadTerm> GetHeadHistory(CurrentUserContext user, string departmentId)
    {
        Access.Require(user, UserRole.Officer);
        if (people.FindDepartment(departmentId) is null)
            throw new InvalidOperationException("Đơn vị không tồn tại.");
        return people.HeadTerms.Where(x => x.DepartmentId.Equals(departmentId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.From).ToList();
    }
}
