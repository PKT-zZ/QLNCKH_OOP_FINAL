using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Data;

//kho data trong bộ nhớ, dùng để kiểm thử và làm trạng thái nền cho kho JSON
public class InMemoryResearchRepository : IPeopleRepository, IProjectRepository
{
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

    //thêm danh mục và lịch sử lãnh đạo từ dữ liệu mẫu khi khởi tạo
    public void AddSeed(ProjectCategory value) => _categories.Add(value.Id, value);
    public void AddSeed(DepartmentHeadTerm value) => _headTerms.Add(value);
}