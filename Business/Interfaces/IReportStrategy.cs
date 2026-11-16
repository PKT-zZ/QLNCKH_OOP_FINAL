namespace QLNCKH_OOP_FINAL.Business.Interfaces;
public sealed record ReportRow(string Key, string Label, decimal Value, string Unit, string? Detail = null);

//interface cho Strategy Pattern
public interface IReportStrategy
{
    string Title { get; }
    IReadOnlyList<ReportRow> BuildRows();
}