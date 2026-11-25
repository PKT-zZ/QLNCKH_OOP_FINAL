using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

//Strategy (B): so dau ra theo tung de tai
public sealed class OutputByProjectStrategy(IProjectRepository projects, IResultRepository results) : IReportStrategy
{
    public string Title => "Kết quả đầu ra theo đề tài";

    public IReadOnlyList<ReportRow> BuildRows() => projects.Projects
        .OrderBy(project => project.Id, StringComparer.OrdinalIgnoreCase)
        .Select(project =>
        {
            var outputs = results.OutputsOf(project.Id);
            var detail = outputs.Count == 0
                ? "chưa có đầu ra"
                : $"{outputs.OfType<Paper>().Count()} bài báo, {outputs.OfType<ProjectDeliverable>().Count()} sản phẩm: " +
                  string.Join("; ", outputs.Select(o => o.Title));
            return new ReportRow(project.Id, project.Title, outputs.Count, "đầu ra", detail);
        })
        .ToList();
}
