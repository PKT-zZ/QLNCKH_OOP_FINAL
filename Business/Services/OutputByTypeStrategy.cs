using QLNCKH_OOP_FINAL.Business.Interfaces;

namespace QLNCKH_OOP_FINAL.Business.Services;

//Strategy mo rong (B): gom dau ra theo loai (bai bao, tung loai san pham). Loai chua co du lieu khong hien.
public sealed class OutputByTypeStrategy(IResultRepository results) : IReportStrategy
{
    public string Title => "Kết quả đầu ra theo loại";

    public IReadOnlyList<ReportRow> BuildRows()
    {
        var rows = new List<ReportRow>();
        if (results.Papers.Count > 0)
            rows.Add(new ReportRow("PAPER", "Bài báo khoa học", results.Papers.Count, "bài"));

        rows.AddRange(results.Deliverables
            .GroupBy(d => d.TypeId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var type = results.FindDeliverableType(g.Key)
                    ?? throw new InvalidDataException($"Loại sản phẩm {g.Key} không tồn tại.");
                return new ReportRow(type.Id, type.Name, g.Sum(d => d.Quantity), "sản phẩm", $"{g.Count()} lần ghi nhận");
            }));
        return rows;
    }
}
