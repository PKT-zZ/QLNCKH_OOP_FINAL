using QLNCKH_OOP_FINAL.Presentation;

Console.WriteLine("Hệ thống Quản lý NCKH khởi động thành công!");

try { new ConsoleMenu().Run(); }
catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException
    or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Không thể mở dữ liệu ứng dụng: {ex.Message}");
    Environment.ExitCode = 1;
}