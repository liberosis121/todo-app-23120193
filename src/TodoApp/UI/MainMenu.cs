using Spectre.Console;
using TodoApp.Models;
using TodoApp.Services;
using TodoApp.Utils;

namespace TodoApp.UI;

/// <summary>
/// Vòng lặp menu chính: đọc lệnh → gọi Service → hiển thị kết quả.
/// Chỉ có lớp này biết về Console; Service hoàn toàn "thuần nghiệp vụ".
/// </summary>
public sealed class MainMenu
{
    private static readonly string[] MenuItems =
    {
        "Thêm công việc",
        "Xem danh sách / Lọc",
        "Tìm kiếm",
        "Đánh dấu hoàn thành",
        "Sửa công việc",
        "Xóa công việc",
        "Thoát"
    };

    private readonly ITaskService _service;
    private bool _running = true;

    public MainMenu(ITaskService service) => _service = service;

    /// <summary>Chạy ứng dụng cho đến khi người dùng chọn Thoát.</summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        ConsoleUi.Banner();

        while (_running)
        {
            ConsoleUi.Divider();
            ConsoleUi.Stats(_service.Items);
            AnsiConsole.WriteLine();

            for (var i = 0; i < MenuItems.Length; i++)
                AnsiConsole.MarkupLine($"  [bold cyan]{i + 1}.[/] {MenuItems[i]}");

            AnsiConsole.WriteLine();
            var choice = ConsoleUi.ReadMenuChoice(MenuItems.Length);
            if (choice == 0 && ConsoleInput.Eof) break;         // hết nhập liệu → thoát sạch
            if (choice < 0)
            {
                ConsoleUi.Warn("Lựa chọn không hợp lệ, thử lại.");
                continue;
            }

            // Gọi handler tương ứng; async/await chạy tuần tự từng thao tác
            switch (choice)
            {
                case 1: await AddTaskAsync(ct); break;
                case 2: ListTasks(); break;
                case 3: SearchTasks(); break;
                case 4: await ToggleTaskAsync(ct); break;
                case 5: await EditTaskAsync(ct); break;
                case 6: await RemoveTaskAsync(ct); break;
                case 7: _running = false; break;
                case 0: ShowDataInfo(); break;   // "bí mật": 0 = info file dữ liệu
            }
        }

        ConsoleUi.Info("Tạm biệt! Dữ liệu đã được lưu.");
    }

    private async Task AddTaskAsync(CancellationToken ct)
    {
        AnsiConsole.MarkupLine("\n[bold cyan]＋ Thêm công việc mới[/]");
        var title = InputValidator.PromptNonEmpty("Tiêu đề");
        var description = InputValidator.PromptOptional("Mô tả");
        var priority = ConsoleUi.PromptPriority();
        var due = InputValidator.PromptDate("Hạn hoàn thành");

        await _service.AddAsync(title, description, priority, due);
        ConsoleUi.Info($"Đã thêm \"{title}\".");
        ct.ThrowIfCancellationRequested();
    }

    private void ListTasks()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]Danh sách công việc[/]");
        AnsiConsole.MarkupLine("[grey]1. Tất cả   2. Chưa xong   3. Đã xong   4. Quá hạn[/]");
        var choice = InputValidator.PromptInt("Lọc", 1, 4);
        var filter = (TaskFilter)(choice - 1);

        var items = _service.Filter(filter);
        ConsoleUi.RenderTasks(items, $"Lọc: {filter switch
        {
            TaskFilter.Active => "Chưa xong",
            TaskFilter.Done => "Đã xong",
            TaskFilter.Overdue => "Quá hạn",
            _ => "Tất cả"
        }}");
    }

    private void SearchTasks()
    {
        AnsiConsole.MarkupLine("\n[bold cyan]Tìm kiếm công việc[/]");
        var keyword = InputValidator.PromptNonEmpty("Từ khóa");
        var results = _service.Search(keyword);
        ConsoleUi.RenderTasks(results, $"Kết quả cho \"{keyword}\" ({results.Count} mục)");
    }

    private async Task ToggleTaskAsync(CancellationToken ct)
    {
        if (!EnsureHasTasks()) return;

        AnsiConsole.MarkupLine("\n[bold cyan]Đánh dấu hoàn thành[/]");
        ConsoleUi.RenderTasks(_service.Items, "Chọn công việc");
        var index = ConsoleUi.PickIndex(_service.Items.Count);
        var item = _service.Items[index];

        await _service.ToggleDoneAsync(item.Id);
        ConsoleUi.Info(item.IsDone
            ? $"Đánh dấu \"{item.Title}\" là ĐÃ XONG."
            : $"Bỏ đánh dấu \"{item.Title}\".");
        ct.ThrowIfCancellationRequested();
    }

    private async Task EditTaskAsync(CancellationToken ct)
    {
        if (!EnsureHasTasks()) return;

        AnsiConsole.MarkupLine("\n[bold cyan]Sửa công việc[/]");
        ConsoleUi.RenderTasks(_service.Items, "Chọn công việc cần sửa");
        var index = ConsoleUi.PickIndex(_service.Items.Count);
        var item = _service.Items[index];

        AnsiConsole.MarkupLine("[grey]Enter để giữ nguyên giá trị hiện tại.[/]");
        var title = InputValidator.PromptNonEmpty("Tiêu đề", item.Title);
        var description = InputValidator.PromptOptional("Mô tả") ?? item.Description;
        var priority = ConsoleUi.PromptPriority(item.Priority);
        var due = InputValidator.PromptDate("Hạn hoàn thành", item.DueDate);

        await _service.UpdateAsync(item.Id, title, description, priority, due);
        ConsoleUi.Info($"Đã cập nhật \"{title}\".");
        ct.ThrowIfCancellationRequested();
    }

    private async Task RemoveTaskAsync(CancellationToken ct)
    {
        if (!EnsureHasTasks()) return;

        AnsiConsole.MarkupLine("\n[bold red]Xóa công việc[/]");
        ConsoleUi.RenderTasks(_service.Items, "Chọn công việc cần xóa");
        var index = ConsoleUi.PickIndex(_service.Items.Count);
        var item = _service.Items[index];

        if (!ConsoleUi.Confirm($"Xóa \"{item.Title}\"? Hành động này không hoàn tác.")) return;

        await _service.RemoveAsync(item.Id);
        ConsoleUi.Info("Đã xóa 1 công việc.");
        ct.ThrowIfCancellationRequested();
    }

    private void ShowDataInfo()
    {
        AnsiConsole.MarkupLine($"\n[grey]Số công việc trong bộ nhớ: {_service.Items.Count}[/]");
        AnsiConsole.MarkupLine("[grey]Nhập 0 bất kỳ lúc nào để xem thông tin này.[/]");
    }

    private bool EnsureHasTasks()
    {
        if (_service.Items.Count > 0) return true;
        ConsoleUi.Warn("Chưa có công việc nào. Hãy chọn chức năng 1 để thêm.");
        return false;
    }
}
