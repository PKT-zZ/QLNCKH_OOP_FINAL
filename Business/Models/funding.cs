namespace QLNCKH_OOP_FINAL.Business.Models;

//nguon cap kinh phi, 1 nguon co the cap nhieu de tai
public sealed class FundingSource
{
    public string Id { get; }
    public string Name { get; }
    public string Type { get; }

    public FundingSource(string id, string name, string type)
    {
        Id = Guard.Required(id, nameof(id));
        Name = Guard.Required(name, nameof(name));
        Type = Guard.Required(type, nameof(type));
    }
}

//1 lan cap tien cu the. 1 nguon co the cap nhieu lan cho 1 de tai
public sealed class FundingAllocation
{
    public string Id { get; }
    public string ProjectId { get; }
    public string SourceId { get; }
    public decimal Amount { get; }
    public DateOnly AllocatedAt { get; }

    public FundingAllocation(string id, string projectId, string sourceId, decimal amount, DateOnly allocatedAt)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền phân bổ phải lớn hơn 0.");
        Id = Guard.Required(id, nameof(id));
        ProjectId = Guard.Required(projectId, nameof(projectId));
        SourceId = Guard.Required(sourceId, nameof(sourceId));
        Amount = amount;
        AllocatedAt = allocatedAt;
    }
}