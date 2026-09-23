using Spectre.Console;
using TodoApp.Models;
using TodoApp.Services;
using TodoApp.Utils;

namespace TodoApp.UI;

/// <summary>
/// Application controller của giao diện: nhận lựa chọn từ TUI, gọi service tương ứng
/// và điều hướng giữa các view. Lớp này không chứa quy tắc nghiệp vụ.
/// </summary>
public sealed class MainMenu
{
    private static readonly string[] MenuItems =
    {
        "＋  Thêm công việc mới",
        "≡  Xem danh sách và lọc",
        "?  Tìm kiếm công việc",
        "✓  Đổi trạng thái hoàn thành",
        "~  Chỉnh sửa công việc",
        "×  Xóa công việc",
        "→  Thoát ứng dụng"
    };

    private readonly ITaskService _service;
    private bool _running = true;
    private string? _notification;

    public MainMenu(ITaskService service) => _service = service;

    public async Task RunAsync(CancellationToken ct = default)
    {
        while (_running && !ct.IsCancellationRequested)
        {
            ConsoleUi.Dashboard(_service.Items, _notification);
            _notification = null;

            var choice = ConsoleUi.MainMenu(MenuItems);
            switch (choice)
            {
                case 1: await AddTaskAsync(ct); break;
                case 2: ListTasks(); break;
                case 3: SearchTasks(); break;
                case 4: await ToggleTaskAsync(ct); break;
                case 5: await EditTaskAsync(ct); break;
                case 6: await RemoveTaskAsync(ct); break;
                case 7: _running = false; break;
                default: _notification = "Lựa chọn không hợp lệ, vui lòng thử lại."; break;
            }
        }

        ConsoleUi.BeginView("HẸN GẶP LẠI");
        ConsoleUi.Success("Dữ liệu đã được lưu an toàn. Chúc bạn hoàn thành tốt mọi công việc!");
    }

    private async Task AddTaskAsync(CancellationToken ct)
    {
        ConsoleUi.BeginView("THÊM CÔNG VIỆC");
        ConsoleUi.FormHeader("＋", "THÔNG TIN CÔNG VIỆC MỚI",
            "Tiêu đề là bắt buộc • Mô tả và hạn chót có thể bỏ trống");

        var title = InputValidator.PromptNonEmpty("Tiêu đề");
        var description = InputValidator.PromptOptional("Mô tả");
        var priority = ConsoleUi.PromptPriority();
        var due = InputValidator.PromptDate("Hạn hoàn thành");

        await _service.AddAsync(title, description, priority, due);
        ct.ThrowIfCancellationRequested();
        _notification = $"Đã thêm “{title}”.";
    }

    private void ListTasks()
    {
        ConsoleUi.BeginView("DANH SÁCH CÔNG VIỆC", "Chọn một bộ lọc để thu gọn danh sách");
        var filter = ConsoleUi.PromptFilter();
        var items = _service.Filter(filter);
        var label = filter switch
        {
            TaskFilter.Active => "CHƯA HOÀN THÀNH",
            TaskFilter.Done => "ĐÃ HOÀN THÀNH",
            TaskFilter.Overdue => "QUÁ HẠN",
            _ => "TẤT CẢ CÔNG VIỆC"
        };

        ConsoleUi.RenderTasks(items, $"{label} • {items.Count} MỤC");
        ConsoleUi.Pause();
    }

    private void SearchTasks()
    {
        ConsoleUi.BeginView("TÌM KIẾM", "Tìm trong cả tiêu đề và mô tả, không phân biệt chữ hoa/thường");
        ConsoleUi.FormHeader("?", "NHẬP TỪ KHÓA", "Ví dụ: học tập, báo cáo, mua sắm...");
        var keyword = InputValidator.PromptNonEmpty("Từ khóa");
        var results = _service.Search(keyword);

        ConsoleUi.RenderTasks(results, $"KẾT QUẢ “{keyword}” • {results.Count} MỤC");
        ConsoleUi.Pause();
    }

    private async Task ToggleTaskAsync(CancellationToken ct)
    {
        if (!EnsureHasTasks()) return;
        ConsoleUi.BeginView("CẬP NHẬT TRẠNG THÁI", "Chọn task để chuyển giữa Chưa xong ↔ Đã xong");
        ConsoleUi.RenderTasks(_service.Items, "CHỌN CÔNG VIỆC", showDescription: false);
        var item = ConsoleUi.PickTask(_service.Items, "Công việc cần đổi trạng thái");

        await _service.ToggleDoneAsync(item.Id);
        ct.ThrowIfCancellationRequested();
        _notification = item.IsDone
            ? $"“{item.Title}” đã hoàn thành. Làm tốt lắm!"
            : $"Đã chuyển “{item.Title}” về trạng thái chưa xong.";
    }

    private async Task EditTaskAsync(CancellationToken ct)
    {
        if (!EnsureHasTasks()) return;
        ConsoleUi.BeginView("CHỈNH SỬA CÔNG VIỆC");
        var item = ConsoleUi.PickTask(_service.Items, "Chọn công việc cần chỉnh sửa");

        ConsoleUi.FormHeader("~", "THÔNG TIN CHỈNH SỬA",
            "Nhấn Enter để giữ nguyên tiêu đề, mô tả hoặc hạn chót hiện tại");
        var title = InputValidator.PromptNonEmpty("Tiêu đề", item.Title);
        var description = InputValidator.PromptOptional("Mô tả") ?? item.Description;
        var priority = ConsoleUi.PromptPriority(item.Priority);
        var due = InputValidator.PromptDate("Hạn hoàn thành", item.DueDate);

        await _service.UpdateAsync(item.Id, title, description, priority, due);
        ct.ThrowIfCancellationRequested();
        _notification = $"Đã cập nhật “{title}”.";
    }

    private async Task RemoveTaskAsync(CancellationToken ct)
    {
        if (!EnsureHasTasks()) return;
        ConsoleUi.BeginView("XÓA CÔNG VIỆC", "Thao tác xóa không thể hoàn tác");
        var item = ConsoleUi.PickTask(_service.Items, "Chọn công việc cần xóa");

        if (!ConsoleUi.Confirm($"Bạn chắc chắn muốn xóa “{item.Title}”?"))
        {
            _notification = "Đã hủy thao tác xóa.";
            return;
        }

        await _service.RemoveAsync(item.Id);
        ct.ThrowIfCancellationRequested();
        _notification = $"Đã xóa “{item.Title}”.";
    }

    private bool EnsureHasTasks()
    {
        if (_service.Items.Count > 0) return true;
        _notification = "Danh sách đang trống. Hãy thêm công việc trước.";
        return false;
    }
}
