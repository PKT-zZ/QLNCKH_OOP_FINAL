using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Data;

//kho data trong bộ nhớ, dùng để kiểm thử và làm trạng thái nền cho kho JSON
public class InMemoryResearchRepository : IPeopleRepository, IProjectRepository, IResultRepository
{
    //phan he B: hoi dong nghiem thu, bai bao, san pham ban giao
    private readonly Dictionary<string, EvaluationCouncil> _councils = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Paper> _papers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProjectDeliverable> _deliverables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DeliverableType> _deliverableTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ProjectPaperLink> _paperLinks = [];

    private readonly Dictionary<string, Department> _departments = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Researcher> _researchers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProjectCategory> _categories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ResearchProject> _projects = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FundingSource> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DepartmentHeadTerm> _headTerms = [];

    //trả danh sách dạng chỉ đọc để service không sửa trực tiếp các Dictionary
    public IReadOnlyCollection<Researcher> Researchers => _researchers.Values.ToList();
    public IReadOnlyCollection<Department> Departments => _departments.Values.ToList();
    public IReadOnlyCollection<DepartmentHeadTerm> HeadTerms => _headTerms.AsReadOnly();
    public IReadOnlyCollection<ProjectCategory> Categories => _categories.Values.ToList();
    public IReadOnlyCollection<ResearchProject> Projects => _projects.Values.ToList();
    public IReadOnlyCollection<FundingSource> Sources => _sources.Values.ToList();
    public Department? FindDepartment(string id) => _departments.GetValueOrDefault(id);
    public Researcher? FindResearcher(string id) => _researchers.GetValueOrDefault(id);
    public ProjectCategory? FindCategory(string id) => _categories.GetValueOrDefault(id);
    public ResearchProject? FindProject(string id) => _projects.GetValueOrDefault(id);
    public FundingSource? FindSource(string id) => _sources.GetValueOrDefault(id);
    public virtual void Save(Department value) => _departments[value.Id] = value;
    public virtual void Save(Researcher value) => _researchers[value.Id] = value;
    public virtual void Save(ResearchProject value) => _projects[value.Id] = value;
    public virtual void Save(FundingSource value) => _sources[value.Id] = value;

    //IResultRepository (phan he B)
    public IReadOnlyCollection<EvaluationCouncil> Councils => _councils.Values.ToList();
    public IReadOnlyCollection<Paper> Papers => _papers.Values.ToList();
    public IReadOnlyCollection<ProjectPaperLink> PaperLinks => _paperLinks.AsReadOnly();
    public IReadOnlyCollection<ProjectDeliverable> Deliverables => _deliverables.Values.ToList();
    public IReadOnlyCollection<DeliverableType> DeliverableTypes => _deliverableTypes.Values.ToList();
    public EvaluationCouncil? FindCouncil(string id) => _councils.GetValueOrDefault(id);
    public EvaluationCouncil? FindCouncilByProject(string projectId) =>
        _councils.Values.FirstOrDefault(c => c.ProjectId.Equals(projectId, StringComparison.OrdinalIgnoreCase));
    public Paper? FindPaper(string id) => _papers.GetValueOrDefault(id);
    public ProjectDeliverable? FindDeliverable(string id) => _deliverables.GetValueOrDefault(id);
    public DeliverableType? FindDeliverableType(string id) => _deliverableTypes.GetValueOrDefault(id);
    public virtual void Save(EvaluationCouncil value) => _councils[value.Id] = value;
    public virtual void Save(Paper value) => _papers[value.Id] = value;
    public virtual void Save(ProjectDeliverable value) => _deliverables[value.Id] = value;
    public virtual void Save(ProjectPaperLink value)
    {
        //moi cap (de tai, bai bao) chi luu 1 lan
        if (!_paperLinks.Any(l => l.ProjectId.Equals(value.ProjectId, StringComparison.OrdinalIgnoreCase) &&
                                  l.PaperId.Equals(value.PaperId, StringComparison.OrdinalIgnoreCase)))
            _paperLinks.Add(value);
    }

    //thêm danh mục và lịch sử lãnh đạo từ dữ liệu mẫu khi khởi tạo
    public void AddSeed(ProjectCategory value) => _categories.Add(value.Id, value);
    public void AddSeed(DepartmentHeadTerm value) => _headTerms.Add(value);
    public void AddSeed(DeliverableType value) => _deliverableTypes.Add(value.Id, value);
}