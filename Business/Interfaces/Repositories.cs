using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Interfaces;

//truy xuat don vi, researcher, nhiem ki lanh dao dvi
public interface IPeopleRepository
{
    Department? FindDepartment(string id);
    Researcher? FindResearcher(string id);
    IReadOnlyCollection<Researcher> Researchers { get; }
    IReadOnlyCollection<DepartmentHeadTerm> HeadTerms { get; }
    void Save(Department department);
    void Save(Researcher researcher);
}

//truy xuat de tai, loai de tai, nguon kinh phi
public interface IProjectRepository
{
    ProjectCategory? FindCategory(string id);
    ResearchProject? FindProject(string id);
    FundingSource? FindSource(string id);
    IReadOnlyCollection<ResearchProject> Projects { get; }
    void Save(ResearchProject project);
    void Save(FundingSource source);
}

//cung cap bang chung can thiet truoc khi hoan thanh de tai
public interface ICompletionEvidence
{
    bool HasScoredCouncil(string projectId);
    bool HasResearchOutput(string projectId);
}