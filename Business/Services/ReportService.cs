using QLNCKH_OOP_FINAL.Business.Interfaces;
using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

// UC08: điểm vào duy nhất cho việc sinh báo cáo từ Strategy.
// Kiểm quyền tại tầng nghiệp vụ để không phụ thuộc vào việc menu có kiểm tra hay không.
public sealed class ReportService
{
    public IReadOnlyList<ReportRow> Generate(CurrentUserContext user, IReportStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        if (user.Role != UserRole.Officer)
            throw new UnauthorizedAccessException("Chỉ Chuyên viên mới được xem báo cáo.");

        return strategy.BuildRows().ToList();
    }
}