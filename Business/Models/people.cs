namespace QLNCKH_OOP_FINAL.Business.Models;

//don vi cong tac voi ma va ngay thanh lap const
public sealed class Department
{
    public string Id { get; }
    public string Name { get; private set; }
    public DateOnly CreatedAt { get; }

    public Department(string id, string name, DateOnly createdAt)
    {
        Id = Guard.Required(id, nameof(id));
        Name = Guard.Required(name, nameof(name));
        CreatedAt = createdAt;
    }

    //chi doi ten don vi khi da kiem tra ten moi
    public void Rename(string name) => Name = Guard.Required(name, nameof(name));
}

//ho so cua Researcher, dung chung cho de tai, hoi dong, tac gia
public sealed class Researcher
{
    public string Id { get; }
    public string DepartmentId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? AcademicTitle { get; private set; }
    public string? Phone { get; private set; }
    public string? Organization { get; private set; }
    public bool IsInstructor { get; private set; }

    public Researcher(string id, string departmentId, string fullName, string email,
        string? academicTitle = null, string? phone = null, string? organization = null, bool isInstructor = false)
    {
        Id = Guard.Required(id, nameof(id));
        Update(departmentId, fullName, email, academicTitle, phone, organization, isInstructor);
    }
    //kiem tra input truoc khi update ho so
    public void Update(string departmentId, string fullName, string email,
        string? academicTitle, string? phone, string? organization, bool isInstructor)
    {
        var validDepartmentId = Guard.Required(departmentId, nameof(departmentId));
        var validName = Guard.Required(fullName, nameof(fullName));
        var normalizedEmail = Guard.Required(email, nameof(email));
        if (!normalizedEmail.Contains('@') || normalizedEmail.StartsWith('@') || normalizedEmail.EndsWith('@'))
            throw new ArgumentException("Email không hợp lệ.", nameof(email));
        DepartmentId = validDepartmentId;
        FullName = validName;
        Email = normalizedEmail;
        AcademicTitle = academicTitle?.Trim();
        Phone = phone?.Trim();
        Organization = organization?.Trim();
        IsInstructor = isInstructor;
    }
}

///1 nhiem ky lanh dao cua 1 researcher tai 1 don vi
public sealed class DepartmentHeadTerm
{
    public string Id { get; }
    public string DepartmentId { get; }
    public string ResearcherId { get; }
    public DateOnly From { get; }
    public DateOnly? To { get; }

    public DepartmentHeadTerm(string id, string departmentId, string researcherId, DateOnly from, DateOnly? to)
    {
        Guard.DateRange(from, to);
        Id = Guard.Required(id, nameof(id));
        DepartmentId = Guard.Required(departmentId, nameof(departmentId));
        ResearcherId = Guard.Required(researcherId, nameof(researcherId));
        From = from;
        To = to;
    }
}