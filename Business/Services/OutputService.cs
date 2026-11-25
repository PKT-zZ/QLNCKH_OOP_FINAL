using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

//UC07: ghi bai bao / san pham ban giao. Lenh AddPaper chon PaperCreator, AddDeliverable chon DeliverableCreator.
public sealed class OutputService(IPeopleRepository people, IProjectRepository projects, IResultRepository results)
{
    private readonly ResearchOutputCreator _paperCreator = new PaperCreator();
    private readonly ResearchOutputCreator _deliverableCreator = new DeliverableCreator();

    public Paper AddPaper(CurrentUserContext user, PaperDraft draft)
    {
        Access.Require(user, UserRole.Researcher);
        ArgumentNullException.ThrowIfNull(draft);
        var project = RequireParticipatedActiveProject(user, draft.ProjectId);
        ArgumentNullException.ThrowIfNull(draft.Authors);
        if (!draft.Authors.Any(a => a.ResearcherId.Trim().Equals(user.ResearcherId, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Chỉ tác giả của bài báo mới được ghi nhận bài báo.");
        if (draft.Authors.Any(a => people.FindResearcher(a.ResearcherId) is null))
            throw new InvalidOperationException("Có tác giả không tồn tại trong hệ thống.");

        var paper = (Paper)_paperCreator.Build(draft);
        if (results.FindPaper(paper.Id) is not null)
            throw new InvalidOperationException("Mã bài báo đã tồn tại.");
        //R9: DOI khong trung neu duoc nhap
        if (paper.Doi is not null && results.Papers.Any(p => paper.Doi.Equals(p.Doi, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("DOI đã tồn tại.");

        results.SavePaperWithLink(paper, project.Id);
        return paper;
    }

    public ProjectDeliverable AddDeliverable(CurrentUserContext user, DeliverableDraft draft)
    {
        Access.Require(user, UserRole.Researcher);
        ArgumentNullException.ThrowIfNull(draft);
        var project = RequireParticipatedActiveProject(user, draft.ProjectId);
        if (results.FindDeliverableType(draft.TypeId) is null)
            throw new InvalidOperationException("Loại sản phẩm không tồn tại.");

        var deliverable = (ProjectDeliverable)_deliverableCreator.Build(draft);   //R10 duoc kiem trong Validate()
        if (results.FindDeliverable(deliverable.Id) is not null)
            throw new InvalidOperationException("Mã sản phẩm đã tồn tại.");
        if (!deliverable.ProjectId.Equals(project.Id, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Sản phẩm tham chiếu sai đề tài.");

        results.Save(deliverable);
        return deliverable;
    }

    //nguoi ghi nhan phai la thanh vien cua de tai; de tai phai dang thuc hien
    private ResearchProject RequireParticipatedActiveProject(CurrentUserContext user, string projectId)
    {
        if (string.IsNullOrWhiteSpace(user.ResearcherId) || people.FindResearcher(user.ResearcherId) is null)
            throw new UnauthorizedAccessException("Hồ sơ nhà nghiên cứu không tồn tại.");
        var project = projects.FindProject(projectId) ?? throw new InvalidOperationException("Đề tài không tồn tại.");
        if (!project.Members.Any(m => m.ResearcherId.Equals(user.ResearcherId, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Chỉ nhà nghiên cứu tham gia đề tài mới được ghi nhận đầu ra.");
        if (project.Status != ProjectStatus.Active)
            throw new InvalidOperationException("Chỉ ghi nhận đầu ra cho đề tài đang thực hiện.");
        return project;
    }
}
