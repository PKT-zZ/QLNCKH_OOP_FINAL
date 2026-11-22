using QLNCKH_OOP_FINAL.Business.Interfaces;

namespace QLNCKH_OOP_FINAL.Business.Services;

public sealed class FundingBySourceStrategy(IProjectRepository projects) : IReportStrategy
{
    public string Title => "Tổng kinh phí theo nguồn";

    // Nguồn chưa cấp kinh phí không xuất hiện trong báo cáo.
    public IReadOnlyList<ReportRow> BuildRows() => projects.Projects
        .SelectMany(project => project.Allocations)
        .GroupBy(allocation => allocation.SourceId, StringComparer.OrdinalIgnoreCase)
        .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
        .Select(group =>
        {
            var source = projects.FindSource(group.Key)
                ?? throw new InvalidDataException($"Nguồn kinh phí {group.Key} không tồn tại.");
            return new ReportRow(source.Id, source.Name, group.Sum(item => item.Amount), "đ",
                $"{group.Count()} đợt cấp");
        })
        .ToList();
}
