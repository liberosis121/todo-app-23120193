// Top-level statements: điểm khởi động của ứng dụng.
// Toàn bộ wiring thủ công (Composition Root) nằm gọn tại đây.

using System.Text;
using TodoApp.Data;
using TodoApp.Services;
using TodoApp.UI;

// Console tiếng Việt cần UTF-8, nếu không dấu sẽ thành ??? trên Windows
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;
Console.Title = "TODO App — 23120193";

// Dependency Inversion: chỉ new interface-implementation ở composition root,
// các lớp bên dưới chỉ nhận ITaskRepository / ITaskService.
ITaskRepository repository = new JsonTaskRepository();
ITaskService service = new TaskService(repository);

using var cts = new CancellationTokenSource();

// Bắt phím Ctrl+C để dừng mềm thay vì kill process
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    ConsoleUi.Warn("Đang thoát, dữ liệu được lưu an toàn...");
};

try
{
    await service.InitializeAsync(cts.Token);

    var menu = new MainMenu(service);
    await menu.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    ConsoleUi.Warn("Đã hủy thao tác bởi người dùng.");
}
catch (Exception ex)
{
    // Chống crash: mọi lỗi không lường trước đều được báo đẹp thay vì stack trace đỏ
    ConsoleUi.Error($"Lỗi không mong muốn: {ex.Message}");
}
finally
{
    // Đảm bảo dữ liệu được ghi xuống file khi thoát (dù lỗi hay bình thường)
    try
    {
        await repository.SaveAsync(service.Items, CancellationToken.None);
    }
    catch (IOException ioEx)
    {
        ConsoleUi.Error($"Không lưu được file: {ioEx.Message}");
    }
}
