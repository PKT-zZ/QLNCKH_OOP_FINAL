using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;
namespace QLNCKH_OOP_FINAL.Business.Services;

public sealed class ProjectService(IPeopleRepository people, IProjectRepository projects)
{
    public ResearchProject ProposeProject(CurrentUserContext user, string id, string categoryId,
        string title, DateOnly startDate, DateOnly? endDate)
    {
        Access.Require(user, UserRole.Researcher);
        if (string.IsNullOrWhiteSpace(user.ResearcherId) || people.FindResearcher(user.ResearcherId) is null)
            throw new UnauthorizedAccessException("Hồ sơ nhà nghiên cứu không tồn tại.");
        if (projects.FindCategory(categoryId) is null)
            throw new InvalidOperationException("Loại đề tài không tồn tại.");
        // Dùng cùng mã đã chuẩn hóa để tra trùng và tạo đề tài.
        var normalizedId = Guard.Required(id, nameof(id));
        if (projects.FindProject(normalizedId) is not null)
            throw new InvalidOperationException("Mã đề tài đã tồn tại.");
        var project = new ResearchProject(normalizedId, categoryId, title, startDate, endDate);
        projects.Save(project);
        return project;
    }

    public void AssignMember(CurrentUserContext user, string projectId, string researcherId, ProjectRole role)
    {
        Access.Require(user, UserRole.Officer);
        if (people.FindResearcher(researcherId) is null)
            throw new InvalidOperationException("Nhà nghiên cứu không tồn tại.");
        var project = RequiredProject(projectId);
        project.AddMember(researcherId, role);
        projects.Save(project);
    }

    public void Approve(CurrentUserContext user, string projectId)
    {
        Access.Require(user, UserRole.Officer);
        var project = RequiredProject(projectId);
        project.Approve("OFFICER", DateTimeOffset.Now);
        projects.Save(project);
    }

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

    public ResearchProject RequiredProject(string id) =>
        projects.FindProject(id) ?? throw new InvalidOperationException("Đề tài không tồn tại.");
}
