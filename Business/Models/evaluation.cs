namespace QLNCKH_OOP_FINAL.Business.Models;

//vai tro cua thanh vien trong hoi dong nghiem thu
public enum CouncilRole { Chairman, Member }

//1 thanh vien trong hoi dong va vai tro cua ho
public sealed class CouncilMembership
{
    public string CouncilId { get; }
    public string ResearcherId { get; }
    public CouncilRole Role { get; }

    public CouncilMembership(string councilId, string researcherId, CouncilRole role)
    {
        if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role), "Vai trò trong hội đồng không hợp lệ.");
        CouncilId = Guard.Required(councilId, nameof(councilId));
        ResearcherId = Guard.Required(researcherId, nameof(researcherId));
        Role = role;
    }
}

//hoi dong nghiem thu cua 1 de tai; diem chi ghi qua RecordScore
public sealed class EvaluationCouncil
{
    private readonly List<CouncilMembership> _members = [];

    public string Id { get; }
    public string ProjectId { get; }
    public DateOnly EvaluationDate { get; }
    public decimal? Score { get; private set; }
    public IReadOnlyList<CouncilMembership> Members => _members.AsReadOnly();
    public string? ChairmanId => _members.FirstOrDefault(m => m.Role == CouncilRole.Chairman)?.ResearcherId;

    public EvaluationCouncil(string id, string projectId, DateOnly evaluationDate)
    {
        Id = Guard.Required(id, nameof(id));
        ProjectId = Guard.Required(projectId, nameof(projectId));
        EvaluationDate = evaluationDate;
    }

    //R6: moi hoi dong 1 Chairman, 1 nguoi khong duoc lap
    public void AddMember(string researcherId, CouncilRole role)
    {
        researcherId = Guard.Required(researcherId, nameof(researcherId));
        if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role), "Vai trò trong hội đồng không hợp lệ.");
        if (_members.Any(m => m.ResearcherId.Equals(researcherId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Nhà nghiên cứu đã có trong hội đồng.");
        if (role == CouncilRole.Chairman && _members.Any(m => m.Role == CouncilRole.Chairman))
            throw new InvalidOperationException("Hội đồng đã có Chủ tịch.");
        _members.Add(new CouncilMembership(Id, researcherId, role));
    }

    //R7: diem trong [0,10], chi Chairman cua chinh hoi dong nay duoc nhap
    public void RecordScore(string actorResearcherId, decimal score)
    {
        if (ChairmanId is null || !ChairmanId.Equals(actorResearcherId?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Chỉ Chủ tịch của hội đồng này mới được ghi điểm.");
        if (score < 0m || score > 10m)
            throw new ArgumentOutOfRangeException(nameof(score), "Điểm nghiệm thu phải nằm trong khoảng 0 đến 10.");
        Score = score;
    }

    //khoi phuc tu json, kiem tra lai R6 va khoang diem
    internal static EvaluationCouncil Restore(string id, string projectId, DateOnly date, decimal? score,
        IEnumerable<CouncilMembership> members)
    {
        var council = new EvaluationCouncil(id, projectId, date);
        foreach (var member in members)
        {
            if (!member.CouncilId.Equals(council.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Thành viên tham chiếu sai hội đồng.");
            council.AddMember(member.ResearcherId, member.Role);
        }
        if (score is not null)
        {
            if (council.ChairmanId is null) throw new InvalidDataException("Hội đồng có điểm nhưng không có Chủ tịch.");
            council.RecordScore(council.ChairmanId, score.Value);
        }
        return council;
    }
}
