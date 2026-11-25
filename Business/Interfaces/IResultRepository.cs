using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Interfaces;

//truy xuat hoi dong nghiem thu, bai bao, san pham ban giao (phan he B)
public interface IResultRepository
{
    IReadOnlyCollection<EvaluationCouncil> Councils { get; }
    IReadOnlyCollection<Paper> Papers { get; }
    IReadOnlyCollection<ProjectPaperLink> PaperLinks { get; }
    IReadOnlyCollection<ProjectDeliverable> Deliverables { get; }
    IReadOnlyCollection<DeliverableType> DeliverableTypes { get; }
    EvaluationCouncil? FindCouncil(string id);
    EvaluationCouncil? FindCouncilByProject(string projectId);
    Paper? FindPaper(string id);
    ProjectDeliverable? FindDeliverable(string id);
    DeliverableType? FindDeliverableType(string id);
    void Save(EvaluationCouncil council);
    void Save(Paper paper);
    // Ghi bài báo mới và liên kết đề tài trong cùng một thao tác.
    void SavePaperWithLink(Paper paper, string projectId);
    void Save(ProjectDeliverable deliverable);
    void Save(ProjectPaperLink link);
}
