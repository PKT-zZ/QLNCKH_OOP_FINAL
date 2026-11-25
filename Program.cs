using QLNCKH_OOP_FINAL.Presentation;

try { new ConsoleMenu().Run(); }
catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException
    or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Không thể mở dữ liệu ứng dụng: {ex.Message}");
    Console.Error.WriteLine("Giữ lại tệp lỗi và khôi phục từ bản sao lưu. Để chạy lại dữ liệu mẫu, dùng thư mục ứng dụng mới có Data/seed.json.");
    Environment.ExitCode = 1;
}
