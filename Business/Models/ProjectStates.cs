namespace QLNCKH_OOP_FINAL.Business.Models;

//moi trang thai tu quyet dinh thao tac nao dc phep
public interface IProjectState
{
    void Approve(ResearchProject project, string actorId, DateTimeOffset at);
    void Complete(ResearchProject project, string actorId, DateTimeOffset at);
}

//de tai can co 1 leader trc khi duyet
public sealed class PendingState : IProjectState
{
    public void Approve(ResearchProject project, string actorId, DateTimeOffset at)
    {
        if (project.Members.Count(m => m.Role == ProjectRole.Leader) != 1)
            throw new InvalidOperationException("Đề tài cần đúng một chủ nhiệm trước khi duyệt.");
        project.ChangeStatus(ProjectStatus.Active, actorId, at);
    }

    public void Complete(ResearchProject project, string actorId, DateTimeOffset at) =>
        throw new InvalidOperationException("Đề tài chờ duyệt không thể hoàn thành.");
}

//de tai da duyet
public sealed class ActiveState : IProjectState
{
    public void Approve(ResearchProject project, string actorId, DateTimeOffset at) =>
        throw new InvalidOperationException("Đề tài đã được duyệt.");

    public void Complete(ResearchProject project, string actorId, DateTimeOffset at) =>
        project.ChangeStatus(ProjectStatus.Completed, actorId, at);
}

//trang thai ket thuc(ko duyet hay hoan thanh dc nua)
public sealed class CompletedState : IProjectState
{
    public void Approve(ResearchProject project, string actorId, DateTimeOffset at) =>
        throw new InvalidOperationException("Đề tài đã hoàn thành.");

    public void Complete(ResearchProject project, string actorId, DateTimeOffset at) =>
        throw new InvalidOperationException("Đề tài đã hoàn thành.");
}