namespace QLNCKH_OOP_FINAL.Business.Models;

//lop cha truu tuong cua moi ket qua dau ra (bai bao, san pham ban giao)
//khong dat ProjectId o day: 1 bai bao co the gan nhieu de tai, con san pham chi gan 1 de tai
public abstract class ResearchOutput
{
    public string Id { get; }
    public string Title { get; }

    protected ResearchOutput(string id, string title)
    {
        Id = Guard.Required(id, nameof(id));
        Title = Guard.Required(title, nameof(title));
    }

    //mo ta ngan de hien thi, moi lop con tu viet theo dac thu cua minh
    public abstract string GetDisplaySummary();

    //kiem tra bat bien (R9, R10); lop con ghi de, ném ngoai le neu sai
    public abstract void Validate();
}

//loai san pham ban giao (danh muc lay tu seed)
public sealed class DeliverableType
{
    public string Id { get; }
    public string Name { get; }

    public DeliverableType(string id, string name)
    {
        Id = Guard.Required(id, nameof(id));
        Name = Guard.Required(name, nameof(name));
    }
}

//san pham ban giao gan voi dung 1 de tai
public sealed class ProjectDeliverable : ResearchOutput
{
    public string ProjectId { get; }
    public string TypeId { get; }
    public int Quantity { get; }

    public ProjectDeliverable(string id, string projectId, string typeId, string title, int quantity)
        : base(id, title)
    {
        ProjectId = Guard.Required(projectId, nameof(projectId));
        TypeId = Guard.Required(typeId, nameof(typeId));
        Quantity = quantity;
    }

    //R10: so luong san pham la so nguyen duong
    public override void Validate()
    {
        if (Quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(Quantity), "Số lượng sản phẩm phải là số nguyên dương.");
    }

    public override string GetDisplaySummary() => $"Sản phẩm [{Id}] {Title} (loại {TypeId}, số lượng {Quantity})";
}

//1 tac gia cua bai bao: thu tu + co phai tac gia lien he khong
public sealed class PaperAuthor
{
    public string PaperId { get; }
    public string ResearcherId { get; }
    public int AuthorOrder { get; }
    public bool IsCorresponding { get; }

    public PaperAuthor(string paperId, string researcherId, int authorOrder, bool isCorresponding)
    {
        if (authorOrder <= 0) throw new ArgumentOutOfRangeException(nameof(authorOrder), "Thứ tự tác giả phải lớn hơn 0.");
        PaperId = Guard.Required(paperId, nameof(paperId));
        ResearcherId = Guard.Required(researcherId, nameof(researcherId));
        AuthorOrder = authorOrder;
        IsCorresponding = isCorresponding;
    }
}

//bai bao khoa hoc; danh sach tac gia chi sua qua AddAuthor
public sealed class Paper : ResearchOutput
{
    private readonly List<PaperAuthor> _authors = [];

    public string? Doi { get; }
    public string Publisher { get; }
    public int PublishedYear { get; }
    public IReadOnlyList<PaperAuthor> Authors => _authors.AsReadOnly();

    public Paper(string id, string title, string? doi, string publisher, int publishedYear) : base(id, title)
    {
        if (publishedYear is < 1900 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(publishedYear), "Năm xuất bản không hợp lệ.");
        Doi = string.IsNullOrWhiteSpace(doi) ? null : doi.Trim();
        Publisher = Guard.Required(publisher, nameof(publisher));
        PublishedYear = publishedYear;
    }

    //R9: thu tu tac gia khong trung, nguoi khong lap, chi 1 tac gia lien he
    public void AddAuthor(string researcherId, int order, bool isCorresponding)
    {
        var author = new PaperAuthor(Id, researcherId, order, isCorresponding);
        if (_authors.Any(a => a.ResearcherId.Equals(author.ResearcherId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Nhà nghiên cứu đã có trong danh sách tác giả.");
        if (_authors.Any(a => a.AuthorOrder == order))
            throw new InvalidOperationException("Thứ tự tác giả bị trùng.");
        if (isCorresponding && _authors.Any(a => a.IsCorresponding))
            throw new InvalidOperationException("Bài báo đã có tác giả liên hệ.");
        _authors.Add(author);
    }

    //R9: phai co it nhat 1 tac gia va dung 1 tac gia lien he
    public override void Validate()
    {
        if (_authors.Count == 0)
            throw new InvalidOperationException("Bài báo cần ít nhất một tác giả.");
        if (_authors.Count(a => a.IsCorresponding) != 1)
            throw new InvalidOperationException("Bài báo cần đúng một tác giả liên hệ.");
    }

    public override string GetDisplaySummary() =>
        $"Bài báo [{Id}] {Title} ({Publisher}, {PublishedYear}), {_authors.Count} tác giả" +
        (Doi is null ? "" : $", DOI {Doi}");

    //khoi phuc tu json: dung lai AddAuthor de kiem tra lai bat bien
    internal static Paper Restore(string id, string title, string? doi, string publisher, int year,
        IEnumerable<PaperAuthor> authors)
    {
        var paper = new Paper(id, title, doi, publisher, year);
        foreach (var author in authors)
        {
            if (!author.PaperId.Equals(paper.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Tác giả tham chiếu sai bài báo.");
            paper.AddAuthor(author.ResearcherId, author.AuthorOrder, author.IsCorresponding);
        }
        paper.Validate();
        return paper;
    }
}

//lien ket nhieu-nhieu giua de tai va bai bao
public sealed class ProjectPaperLink
{
    public string ProjectId { get; }
    public string PaperId { get; }

    public ProjectPaperLink(string projectId, string paperId)
    {
        ProjectId = Guard.Required(projectId, nameof(projectId));
        PaperId = Guard.Required(paperId, nameof(paperId));
    }
}
