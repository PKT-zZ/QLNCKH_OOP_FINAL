using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

//UC04: khai bao nguon va cac lan cap kinh phi
public sealed class FundingService(IProjectRepository projects)
{
    //them nguon moi
    public FundingSource SaveSource(CurrentUserContext user, string id, string name, string type)
    {
        Access.Require(user, UserRole.Officer);
        if (projects.FindSource(id) is not null) throw new InvalidOperationException("Mã nguồn kinh phí đã tồn tại.");
        var source = new FundingSource(id, name, type);
        projects.Save(source);
        return source;
    }

    //kiem tra cac nguon, de tai ton tai co trung ko
    public FundingAllocation Allocate(CurrentUserContext user, string id, string projectId,
        string sourceId, decimal amount, DateOnly allocatedAt)
    {
        Access.Require(user, UserRole.Officer);
        var project = projects.FindProject(projectId) ?? throw new InvalidOperationException("Đề tài không tồn tại.");
        var source = projects.FindSource(sourceId) ?? throw new InvalidOperationException("Nguồn kinh phí không tồn tại.");
        if (projects.Projects.SelectMany(p => p.Allocations).Any(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Mã phân bổ đã tồn tại.");
        var allocation = new FundingAllocation(id, project.Id, source.Id, amount, allocatedAt);
        project.AddAllocation(allocation);
        projects.Save(project);
        return allocation;
    }

    //tong so tien duoc cap cua project
    public decimal TotalForProject(string projectId) =>
        (projects.FindProject(projectId) ?? throw new InvalidOperationException("Đề tài không tồn tại.")).TotalFunding;
}