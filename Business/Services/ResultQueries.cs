using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

//truy van dung chung cho bang chung hoan thanh, Strategy dau ra va tra cuu
public static class ResultQueries
{
    //bai bao lien ket voi de tai + san pham cua de tai, sap theo ma
    public static IReadOnlyList<ResearchOutput> OutputsOf(this IResultRepository results, string projectId)
    {
        var papers = results.PaperLinks
            .Where(l => l.ProjectId.Equals(projectId, StringComparison.OrdinalIgnoreCase))
            .Select(l => results.FindPaper(l.PaperId))
            .OfType<ResearchOutput>();
        var deliverables = results.Deliverables
            .Where(d => d.ProjectId.Equals(projectId, StringComparison.OrdinalIgnoreCase));
        return papers.Concat(deliverables).OrderBy(o => o.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

//adapter B -> A: cho ProjectService.Complete() biet de tai da co diem va dau ra chua
public sealed class ResultCompletionEvidence(IResultRepository results) : ICompletionEvidence
{
    public bool HasScoredCouncil(string projectId) => results.FindCouncilByProject(projectId)?.Score is not null;
    public bool HasResearchOutput(string projectId) => results.OutputsOf(projectId).Count > 0;
}
