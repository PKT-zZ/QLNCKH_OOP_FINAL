namespace QLNCKH_OOP_FINAL.Business.Models;

public interface IProjectState
{
    void Approve(ResearchProject project, string actorId, DateTimeOffset at);
    void Complete(ResearchProject project, string actorId, DateTimeOffset at);
}

public sealed class PendingState : IProjectState
{
    public void Approve(ResearchProject project, string actorId, DateTimeOffset at)
    {
        // State là public, nên lời gọi trực tiếp cũng phải khớp trạng thái hiện tại.
        if (project.Status != ProjectStatus.Pending)
            throw new InvalidOperationException("Chỉ đề tài đang chờ duyệt mới được phê duyệt.");
        if (project.Members.Count(m => m.Role == ProjectRole.Leader) != 1)
            throw new InvalidOperationException("Đề tài cần đúng một chủ nhiệm trước khi duyệt.");
        project.ChangeStatus(ProjectStatus.Active, actorId, at);
    }

    public void Complete(ResearchProject project, string actorId, DateTimeOffset at) =>
        throw new InvalidOperationException("Đề tài chờ duyệt không thể hoàn thành.");
}

public sealed class ActiveState : IProjectState
{
    public void Approve(ResearchProject project, string actorId, DateTimeOffset at) =>
        throw new InvalidOperationException("Đề tài đã được duyệt.");

    public void Complete(ResearchProject project, string actorId, DateTimeOffset at)
    {
        if (project.Status != ProjectStatus.Active)
            throw new InvalidOperationException("Chỉ đề tài đang thực hiện mới được hoàn thành.");
        project.ChangeStatus(ProjectStatus.Completed, actorId, at);
    }
}

public sealed class CompletedState : IProjectState
{
    public void Approve(ResearchProject project, string actorId, DateTimeOffset at) =>
        throw new InvalidOperationException("Đề tài đã hoàn thành.");

    public void Complete(ResearchProject project, string actorId, DateTimeOffset at) =>
        throw new InvalidOperationException("Đề tài đã hoàn thành.");
}
