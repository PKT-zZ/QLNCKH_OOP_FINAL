using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

//du lieu nhap cho UC07; moi loai dau ra co 1 draft rieng
public abstract record OutputDraft(string Id, string Title, string ProjectId);
public sealed record AuthorDraft(string ResearcherId, int Order, bool IsCorresponding);
public sealed record PaperDraft(string Id, string Title, string ProjectId, string? Doi, string Publisher,
    int PublishedYear, IReadOnlyList<AuthorDraft> Authors) : OutputDraft(Id, Title, ProjectId);
public sealed record DeliverableDraft(string Id, string Title, string ProjectId, string TypeId, int Quantity)
    : OutputDraft(Id, Title, ProjectId);

//Factory Method: Creator khai bao Create() truu tuong, lop con quyet dinh tao lop nao.
//Build() la phan chung: goi factory method roi bat buoc Validate() truoc khi tra ve.
public abstract class ResearchOutputCreator
{
    protected abstract ResearchOutput Create(OutputDraft draft);

    public ResearchOutput Build(OutputDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var output = Create(draft);
        output.Validate();
        return output;
    }
}

//UC07 AddPaper dung creator nay -> tao Paper kem danh sach tac gia
public sealed class PaperCreator : ResearchOutputCreator
{
    protected override ResearchOutput Create(OutputDraft draft)
    {
        if (draft is not PaperDraft data) throw new ArgumentException("PaperCreator cần PaperDraft.", nameof(draft));
        ArgumentNullException.ThrowIfNull(data.Authors);
        var paper = new Paper(data.Id, data.Title, data.Doi, data.Publisher, data.PublishedYear);
        foreach (var author in data.Authors) paper.AddAuthor(author.ResearcherId, author.Order, author.IsCorresponding);
        return paper;
    }
}

//UC07 AddDeliverable dung creator nay -> tao ProjectDeliverable
public sealed class DeliverableCreator : ResearchOutputCreator
{
    protected override ResearchOutput Create(OutputDraft draft)
    {
        if (draft is not DeliverableDraft data) throw new ArgumentException("DeliverableCreator cần DeliverableDraft.", nameof(draft));
        return new ProjectDeliverable(data.Id, data.ProjectId, data.TypeId, data.Title, data.Quantity);
    }
}
