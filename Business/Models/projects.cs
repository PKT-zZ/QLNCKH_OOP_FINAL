namespace QLNCKH_OOP_FINAL.Business.Models;

//loai de tai duoc chon khi researcher gui de xuat
public sealed class ProjectCategory
{
    public string Id { get; }
    public string Name { get; }

    public ProjectCategory(string id, string name)
    {
        Id = Guard.Required(id, nameof(id));
        Name = Guard.Required(name, nameof(name));
    }
}

//vai tro cua researcher trong de tai
public sealed class ProjectParticipation
{
    public string ProjectId { get; }
    public string ResearcherId { get; }
    public ProjectRole Role { get; }

    public ProjectParticipation(string projectId, string researcherId, ProjectRole role)
    {
        if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role), "Vai trò đề tài không hợp lệ.");
        ProjectId = Guard.Required(projectId, nameof(projectId));
        ResearcherId = Guard.Required(researcherId, nameof(researcherId));
        Role = role;
    }
}

//ghi nhan ai da chuyen trang thai cua de tai
public sealed record ProjectStatusHistory(
    string ProjectId, ProjectStatus FromStatus, ProjectStatus ToStatus,
    DateTimeOffset ChangedAt, string ActorId);

//quan ly thanh vien, kinh phi va vong doi cua 1 de tai
public sealed class ResearchProject
{
    //chi sua qua phuong thuc nghiep vu, ben ngoai chi doc
    private readonly List<ProjectParticipation> _members = [];
    private readonly List<ProjectStatusHistory> _history = [];
    private readonly List<FundingAllocation> _allocations = [];

    public string Id { get; }
    public string CategoryId { get; }
    public string Title { get; }
    public DateOnly StartDate { get; }
    public DateOnly? EndDate { get; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Pending;
    public IReadOnlyList<ProjectParticipation> Members => _members.AsReadOnly();
    public IReadOnlyList<ProjectStatusHistory> History => _history.AsReadOnly();
    public IReadOnlyList<FundingAllocation> Allocations => _allocations.AsReadOnly();
    //cong cac dot phan bo
    public decimal TotalFunding => _allocations.Sum(a => a.Amount);

    public ResearchProject(string id, string categoryId, string title, DateOnly startDate, DateOnly? endDate)
    {
        Guard.DateRange(startDate, endDate);
        Id = Guard.Required(id, nameof(id));
        CategoryId = Guard.Required(categoryId, nameof(categoryId));
        Title = Guard.Required(title, nameof(title));
        StartDate = startDate;
        EndDate = endDate;
    }

    //chi cho sua thanh vien khi de tai chua duyet xong
    public void AddMember(string researcherId, ProjectRole role)
    {
        researcherId = Guard.Required(researcherId, nameof(researcherId));
        if (Status != ProjectStatus.Pending) throw new InvalidOperationException("Chỉ sửa thành viên khi đề tài đang chờ duyệt.");
        if (_members.Any(m => m.ResearcherId.Equals(researcherId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Nhà nghiên cứu đã có trong đề tài.");
        if (role == ProjectRole.Leader && _members.Any(m => m.Role == ProjectRole.Leader))
            throw new InvalidOperationException("Đề tài đã có chủ nhiệm.");
        _members.Add(new ProjectParticipation(Id, researcherId, role));
    }

    public void Approve(string actorId, DateTimeOffset at) => StateFor(Status).Approve(this, actorId, at);
    public void Complete(string actorId, DateTimeOffset at) => StateFor(Status).Complete(this, actorId, at);

    //doi trang thai + ghi lai lich su trong 1 lan thao tac
    internal void ChangeStatus(ProjectStatus next, string actorId, DateTimeOffset at)
    {
        _history.Add(new ProjectStatusHistory(Id, Status, next, at, Guard.Required(actorId, nameof(actorId))));
        Status = next;
    }

    //them 1 lan cap tien
    public void AddAllocation(FundingAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        if (!allocation.ProjectId.Equals(Id, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Phân bổ không thuộc đề tài này.", nameof(allocation));
        if (_allocations.Any(a => a.Id.Equals(allocation.Id, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Mã phân bổ đã tồn tại trong đề tài.");
        _allocations.Add(allocation);
    }

    //khoi phuc tu json ma ko phat sinh them skien chuyen state moi
    internal static ResearchProject Restore(string id, string categoryId, string title,
        DateOnly startDate, DateOnly? endDate, ProjectStatus status,
        IEnumerable<ProjectParticipation> members, IEnumerable<FundingAllocation> allocations,
        IEnumerable<ProjectStatusHistory> history)
    {
        var project = new ResearchProject(id, categoryId, title, startDate, endDate);
        foreach (var member in members)
        {
            if (!member.ProjectId.Equals(id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Thành viên tham chiếu sai đề tài.");
            project.AddMember(member.ResearcherId, member.Role);
        }
        foreach (var allocation in allocations) project.AddAllocation(allocation);

        var previous = ProjectStatus.Pending;
        foreach (var change in history)
        {
            if (!change.ProjectId.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                change.FromStatus != previous ||
                (previous == ProjectStatus.Pending && change.ToStatus != ProjectStatus.Active) ||
                (previous == ProjectStatus.Active && change.ToStatus != ProjectStatus.Completed) ||
                previous == ProjectStatus.Completed || string.IsNullOrWhiteSpace(change.ActorId))
                throw new InvalidDataException("Lịch sử trạng thái đề tài không hợp lệ.");
            project._history.Add(change);
            previous = change.ToStatus;
        }
        if (status != previous ||
            (status != ProjectStatus.Pending && project._members.Count(m => m.Role == ProjectRole.Leader) != 1))
            throw new InvalidDataException("Trạng thái đề tài không khớp lịch sử hoặc chủ nhiệm.");
        project.Status = status;
        return project;
    }

    //tao doi tuong State theo ma 
    private static IProjectState StateFor(ProjectStatus status) => status switch
    {
        ProjectStatus.Pending => new PendingState(),
        ProjectStatus.Active => new ActiveState(),
        ProjectStatus.Completed => new CompletedState(),
        _ => throw new InvalidOperationException("Trạng thái đề tài không hợp lệ.")
    };
}