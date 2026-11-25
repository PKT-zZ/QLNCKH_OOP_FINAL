using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

public sealed record ProjectSummary(string Id, string Title, ProjectStatus Status, decimal TotalFunding,
    decimal? CouncilScore, IReadOnlyList<string> Outputs);

//UC08 (tra cuu): tim de tai theo ma hoac trang thai, kem tong kinh phi, diem hoi dong, dau ra
public sealed class ProjectLookupService(IProjectRepository projects, IResultRepository results)
{
    public IReadOnlyList<ProjectSummary> Search(CurrentUserContext user, string? projectId = null, ProjectStatus? status = null)
    {
        Access.Require(user, UserRole.Officer);
        if (status is not null && !Enum.IsDefined(status.Value))
            throw new ArgumentOutOfRangeException(nameof(status), "Trạng thái không hợp lệ.");
        return projects.Projects
            .Where(p => string.IsNullOrWhiteSpace(projectId) || p.Id.Equals(projectId.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(p => status is null || p.Status == status)
            .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(p => new ProjectSummary(p.Id, p.Title, p.Status, p.TotalFunding,
                results.FindCouncilByProject(p.Id)?.Score,
                results.OutputsOf(p.Id).Select(o => o.GetDisplaySummary()).ToList()))
            .ToList();
    }
}
