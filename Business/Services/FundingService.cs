using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

public sealed class FundingService(IProjectRepository projects)
{
    public FundingSource SaveSource(CurrentUserContext user, string id, string name, string type)
    {
        Access.Require(user, UserRole.Officer);
        var normalizedId = Guard.Required(id, nameof(id));
        if (projects.FindSource(normalizedId) is not null) throw new InvalidOperationException("Mã nguồn kinh phí đã tồn tại.");
        var source = new FundingSource(normalizedId, name, type);
        projects.Save(source);
        return source;
    }

    public FundingAllocation Allocate(CurrentUserContext user, string id, string projectId,
        string sourceId, decimal amount, DateOnly allocatedAt)
    {
        Access.Require(user, UserRole.Officer);
        var project = projects.FindProject(projectId) ?? throw new InvalidOperationException("Đề tài không tồn tại.");
        // Sau khi nghiệm thu, giữ nguyên các khoản đã cấp cho đề tài.
        if (project.Status == ProjectStatus.Completed)
            throw new InvalidOperationException("Không thể phân bổ thêm kinh phí cho đề tài đã hoàn thành.");

        var source = projects.FindSource(sourceId) ?? throw new InvalidOperationException("Nguồn kinh phí không tồn tại.");
        // Mã đợt cấp phải duy nhất trên toàn bộ đề tài.
        var normalizedId = Guard.Required(id, nameof(id));
        if (projects.Projects.SelectMany(p => p.Allocations).Any(a => a.Id.Equals(normalizedId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Mã phân bổ đã tồn tại.");
        var allocation = new FundingAllocation(normalizedId, project.Id, source.Id, amount, allocatedAt);
        // Báo cáo cộng chung tất cả đề tài, nên kiểm tra tổng trước khi thay đổi hồ sơ.
        try
        {
            _ = projects.Projects.Sum(item => item.TotalFunding) + allocation.Amount;
        }
        catch (OverflowException ex)
        {
            throw new InvalidOperationException("Khoản cấp làm tổng kinh phí vượt phạm vi decimal.", ex);
        }

        project.AddAllocation(allocation);
        projects.Save(project);
        return allocation;
    }

    public decimal TotalForProject(string projectId) =>
        (projects.FindProject(projectId) ?? throw new InvalidOperationException("Đề tài không tồn tại.")).TotalFunding;
}
