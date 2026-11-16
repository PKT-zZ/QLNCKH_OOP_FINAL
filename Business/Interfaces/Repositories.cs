using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Interfaces;

// Đặt hợp đồng ở tầng nghiệp vụ để thay cách lưu dữ liệu mà không sửa các service.
/// <summary>Truy xuất đơn vị, nhà nghiên cứu và nhiệm kỳ lãnh đạo.</summary>
public interface IPeopleRepository
{
    Department? FindDepartment(string id);
    Researcher? FindResearcher(string id);
    IReadOnlyCollection<Researcher> Researchers { get; }
    IReadOnlyCollection<DepartmentHeadTerm> HeadTerms { get; }
    void Save(Department department);
    void Save(Researcher researcher);
}

//Truy xuất đề tài, loại đề tài và nguồn kinh phí
public interface IProjectRepository
{
    ProjectCategory? FindCategory(string id);
    ResearchProject? FindProject(string id);
    FundingSource? FindSource(string id);
    IReadOnlyCollection<ResearchProject> Projects { get; }
    void Save(ResearchProject project);
    void Save(FundingSource source);
}

// Phần B sẽ cài hợp đồng này từ dữ liệu nghiệm thu và đầu ra thật.
// Giao diện không tự quyết định bằng hai cờ bool do menu truyền vào.
/// <summary>Cung cấp bằng chứng cần thiết trước khi hoàn thành đề tài.</summary>
public interface ICompletionEvidence
{
    bool HasScoredCouncil(string projectId);
    bool HasResearchOutput(string projectId);
}