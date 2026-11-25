using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

//UC05 + UC06 (phan ghi diem): lap hoi dong, phan vai, ghi diem
public sealed class EvaluationService(IPeopleRepository people, IProjectRepository projects, IResultRepository results)
{
    //R8: moi de tai toi da 1 hoi dong; chi lap cho de tai dang Active
    public EvaluationCouncil CreateCouncil(CurrentUserContext user, string id, string projectId, DateOnly evaluationDate)
    {
        Access.Require(user, UserRole.Officer);
        var project = projects.FindProject(projectId) ?? throw new InvalidOperationException("Đề tài không tồn tại.");
        if (project.Status != ProjectStatus.Active)
            throw new InvalidOperationException("Chỉ lập hội đồng cho đề tài đang thực hiện.");
        var normalizedId = Guard.Required(id, nameof(id));
        if (results.FindCouncil(normalizedId) is not null)
            throw new InvalidOperationException("Mã hội đồng đã tồn tại.");
        if (results.FindCouncilByProject(project.Id) is not null)
            throw new InvalidOperationException("Đề tài đã có hội đồng nghiệm thu.");
        var council = new EvaluationCouncil(normalizedId, project.Id, evaluationDate);
        results.Save(council);
        return council;
    }

    //R6 duoc kiem trong EvaluationCouncil.AddMember
    public void AssignMember(CurrentUserContext user, string councilId, string researcherId, CouncilRole role)
    {
        Access.Require(user, UserRole.Officer);
        if (people.FindResearcher(researcherId) is null)
            throw new InvalidOperationException("Nhà nghiên cứu không tồn tại.");
        var council = RequiredCouncil(councilId);
        council.AddMember(researcherId, role);
        results.Save(council);
    }

    //R7: Chairman cua dung hoi dong, diem [0,10]; khong sua diem khi de tai da Completed
    public void RecordScore(CurrentUserContext user, string councilId, decimal score)
    {
        Access.Require(user, UserRole.CouncilChair);
        var council = RequiredCouncil(councilId);
        var project = projects.FindProject(council.ProjectId) ?? throw new InvalidOperationException("Đề tài không tồn tại.");
        if (project.Status != ProjectStatus.Active)
            throw new InvalidOperationException("Chỉ ghi điểm cho đề tài đang thực hiện.");
        council.RecordScore(user.ResearcherId ?? string.Empty, score);
        results.Save(council);
    }

    public EvaluationCouncil RequiredCouncil(string id) =>
        results.FindCouncil(id) ?? throw new InvalidOperationException("Hội đồng không tồn tại.");
}
