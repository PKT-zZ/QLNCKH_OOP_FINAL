using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

//UC01: ktra quyen va tinh duy nhat trc khi luu
public sealed class PeopleService(IPeopleRepository people)
{
    //tao don vi moi hoac doi ten dvi cu
    public Department SaveDepartment(CurrentUserContext user, string id, string name, DateOnly createdAt)
    {
        Access.Require(user, UserRole.Officer);
        var department = people.FindDepartment(id);
        if (department is null) department = new Department(id, name, createdAt);
        else department.Rename(name);
        people.Save(department);
        return department;
    }

    //ktra dvi va email trc khi them/update researcher
    public Researcher SaveResearcher(CurrentUserContext user, string id, string departmentId, string fullName,
        string email, string? academicTitle = null, string? phone = null, string? organization = null,
        bool isInstructor = false)
    {
        Access.Require(user, UserRole.Officer);
        if (people.FindDepartment(departmentId) is null)
            throw new InvalidOperationException("Đơn vị không tồn tại.");
        //ktra string trc khi so sanh
        var normalizedEmail = Guard.Required(email, nameof(email));
        if (people.Researchers.Any(r => !r.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                                        r.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Email đã được sử dụng.");
        var researcher = people.FindResearcher(id);
        if (researcher is null)
            researcher = new Researcher(id, departmentId, fullName, normalizedEmail, academicTitle, phone, organization, isInstructor);
        else
            researcher.Update(departmentId, fullName, normalizedEmail, academicTitle, phone, organization, isInstructor);
        people.Save(researcher);
        return researcher;
    }

    //list lich su nhiem ky lanh dao dvi
    public IReadOnlyList<DepartmentHeadTerm> GetHeadHistory(CurrentUserContext user, string departmentId)
    {
        Access.Require(user, UserRole.Officer);
        if (people.FindDepartment(departmentId) is null)
            throw new InvalidOperationException("Đơn vị không tồn tại.");
        return people.HeadTerms.Where(x => x.DepartmentId.Equals(departmentId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.From).ToList();
    }
}