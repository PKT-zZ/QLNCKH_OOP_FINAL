using QLNCKH_OOP_FINAL.Business.Models;

namespace QLNCKH_OOP_FINAL.Business.Services;

//kiem tra vai tro nguoi thao tac o tang service
public static class Access
{
    public static void Require(CurrentUserContext user, UserRole role)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.Role != role)
            throw new UnauthorizedAccessException("Vai trò hiện tại không được thực hiện thao tác này.");
    }
}