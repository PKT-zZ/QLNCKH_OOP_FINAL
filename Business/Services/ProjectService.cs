using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;
namespace QLNCKH_OOP_FINAL.Business.Services;

//xu ly de xuat, gan thanh vien, duyet, hoan thanh de tai
public sealed class ProjectService(IPeopleRepository people, IProjectRepository projects)
{
    //researcher tao de tai, auto cho duyet
    public ResearchProject ProposeProject(CurrentUserContext user, string id, string categoryId,
        string title, DateOnly startDate, DateOnly? endDate)
    {
        Access.Require(user, UserRole.Researcher);
        if (string.IsNullOrWhiteSpace(user.ResearcherId) || people.FindResearcher(user.ResearcherId) is null)
            throw new UnauthorizedAccessException("Hồ sơ nhà nghiên cứu không tồn tại.");
        if (projects.FindCategory(categoryId) is null)
            throw new InvalidOperationException("Loại đề tài không tồn tại.");
        if (projects.FindProject(id) is not null)
            throw new InvalidOperationException("Mã đề tài đã tồn tại.");
        var project = new ResearchProject(id, categoryId, title, startDate, endDate);
        projects.Save(project);
        return project;
    }

    //chuyen vien gan thanh vien
    public void AssignMember(CurrentUserContext user, string projectId, string researcherId, ProjectRole role)
    {
        Access.Require(user, UserRole.Officer);
        if (people.FindResearcher(researcherId) is null)
            throw new InvalidOperationException("Nhà nghiên cứu không tồn tại.");
        var project = RequiredProject(projectId);
        project.AddMember(researcherId, role);
        projects.Save(project);
    }

    //duyet de tai, luu trang thai moi
    public void Approve(CurrentUserContext user, string projectId)
    {
        Access.Require(user, UserRole.Officer);
        var project = RequiredProject(projectId);
        project.Approve("OFFICER", DateTimeOffset.Now);
        projects.Save(project);
    }

    //hoan thanh voi dieu kien nghiem thu
    public void Complete(CurrentUserContext user, string projectId, ICompletionEvidence evidence)
    {
        Access.Require(user, UserRole.Officer);
        ArgumentNullException.ThrowIfNull(evidence);
        var project = RequiredProject(projectId);
        if (!evidence.HasScoredCouncil(projectId) || !evidence.HasResearchOutput(projectId))
            throw new InvalidOperationException("Cần điểm nghiệm thu và ít nhất một đầu ra để hoàn thành đề tài.");
        project.Complete("OFFICER", DateTimeOffset.Now);
        projects.Save(project);
    }

    //lay de tai
    public ResearchProject RequiredProject(string id) =>
        projects.FindProject(id) ?? throw new InvalidOperationException("Đề tài không tồn tại.");
}
