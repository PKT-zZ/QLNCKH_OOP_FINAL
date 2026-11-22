using QLNCKH_OOP_FINAL.Business.Interfaces;

namespace QLNCKH_OOP_FINAL.Business.Services;

public sealed class FundingByProjectStrategy(IProjectRepository projects) : IReportStrategy
{
    public string Title => "Tổng kinh phí theo đề tài";

    public IReadOnlyList<ReportRow> BuildRows() => projects.Projects
        .OrderBy(project => project.Id, StringComparer.OrdinalIgnoreCase)
        .Select(project => new ReportRow(project.Id, project.Title, project.TotalFunding, "đ"))
        .ToList();
}
