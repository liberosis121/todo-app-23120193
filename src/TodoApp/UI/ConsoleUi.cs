using Spectre.Console;
using TodoApp.Models;
using TodoApp.Services;
using TodoApp.Utils;

namespace TodoApp.UI;

/// <summary>
/// Lớp bọc quanh Spectre.Console: toàn bộ hiển thị tập trung ở đây,
/// các lớp khác không biết gì về màu sắc/bảng biểu.
/// </summary>
public static class ConsoleUi
{
    /// <summary>Chuỗi escape màu theo priority → hiển thị trong cột bảng.</summary>
    private static string PriorityMarkup(TaskPriority priority) => priority switch
    {
        TaskPriority.High => "[red]Cao[/]",
        TaskPriority.Medium => "[yellow]Trung bình[/]",
        _ => "[blue]Thấp[/]"
    };

    /// <summary>Hiển thị banner đầu ứng dụng.</summary>
    public static void Banner()
    {
        AnsiConsole.Write(new FigletText("TODO").Color(Color.Cyan1));
        AnsiConsole.MarkupLine("[grey]Quản lý ghi chú công việc trên console — MSSV 23120193[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>In bảng danh sách công việc (đẹp, có màu, có viền).</summary>
    public static void RenderTasks(IReadOnlyList<TaskItem> items, string title)
    {
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]Không có công việc nào.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold cyan]{title}[/]")
            .AddColumn(new TableColumn("[bold]#[/]").Centered())
            .AddColumn(new TableColumn("[bold]Trạng thái[/]").Centered())
            .AddColumn(new TableColumn("[bold]Ưu tiên[/]").Centered())
            .AddColumn(new TableColumn("[bold]Tiêu đề[/]"))
            .AddColumn(new TableColumn("[bold]Hạn[/]").Centered())
            .AddColumn(new TableColumn("[bold]Mô tả[/]"));

        for (var i = 0; i < items.Count; i++)
        {
            var t = items[i];
            var status = t.IsDone ? "[green]✓ Xong[/]" : "[white]✗ Chưa[/]";
            var due = t.DueDate is null
                ? "[grey]—[/]"
                : t.IsOverdue()
                    ? $"[red]{t.DueDate:dd/MM/yyyy} ⚠[/]"
                    : $"[white]{t.DueDate:dd/MM/yyyy}[/]";
            var titleCell = t.IsDone ? $"[strike grey]{Markup.Escape(t.Title)}[/]" : Markup.Escape(t.Title);

            table.AddRow(
                (i + 1).ToString(),
                status,
                PriorityMarkup(t.Priority),
                titleCell,
                due,
                Markup.Escape(t.Description.Length0() == 0 ? "—" : t.Description));
        }

        AnsiConsole.Write(table);
    }

    /// <summary>Thống kê nhanh: tổng / xong / chưa xong / quá hạn.</summary>
    public static void Stats(IReadOnlyList<TaskItem> items)
    {
        var done = items.Count(t => t.IsDone);
        var overdue = items.Count(t => t.IsOverdue());

        var grid = new Grid().AddColumn().AddColumn().AddColumn().AddColumn();
        grid.AddRow(
            $"[bold]Tổng:[/] {items.Count}",
            $"[green]Đã xong:[/] {done}",
            $"[yellow]Còn lại:[/] {items.Count - done}",
            $"[red]Quá hạn:[/] {overdue}");

        AnsiConsole.Write(new Panel(grid).BorderColor(Color.Grey));
    }

    /// <summary>Thông báo thành công / cảnh báo / lỗi theo màu.</summary>
    public static void Info(string message) => AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(message)}");
    public static void Warn(string message) => AnsiConsole.MarkupLine($"[yellow]![/] {Markup.Escape(message)}");
    public static void Error(string message) => AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(message)}");

    /// <summary>Dòng phân cách trước mỗi lần refresh menu.</summary>
    public static void Divider() => AnsiConsole.Write(new Rule("[grey]─[/]").RuleStyle(Color.Grey));

    /// <summary>
    /// Đọc lựa chọn menu. Trả về -1 nếu input sai (đã báo lỗi),
    /// và đánh dấu kết thúc khi hết nhập liệu.
    /// </summary>
    public static int ReadMenuChoice(int max)
    {
        AnsiConsole.Markup("[bold]Chọn chức năng[/] [grey](nhập số, Enter)[/]: ");
        var line = ConsoleInput.ReadLine();
        if (line is null) return 0;                             // EOF → chọn "Thoát"

        var raw = line.OneLine();
        return int.TryParse(raw, out var choice) && choice >= 0 && choice <= max ? choice : -1;
    }

    /// <summary>Chọn 1 mục trong danh sách đang hiển thị theo số thứ tự.</summary>
    public static int PickIndex(int count)
    {
        var value = InputValidator.PromptInt("Nhập số thứ tự", 1, count);
        return value - 1;
    }

    /// <summary>Xác nhận có/không (y/n).</summary>
    public static bool Confirm(string question)
    {
        AnsiConsole.Markup($"[bold yellow]{Markup.Escape(question)}[/] [grey](y/n):[/] ");
        var line = ConsoleInput.ReadLine();
        if (line is null) return false;                         // EOF → không xác nhận xóa

        return line.OneLine().ToLowerInvariant() is "y" or "yes";
    }

    /// <summary>Nhập mức ưu tiên qua số (1-3).</summary>
    public static TaskPriority PromptPriority(TaskPriority current = TaskPriority.Medium)
    {
        var currentName = current switch
        {
            TaskPriority.High => "Cao",
            TaskPriority.Low => "Thấp",
            _ => "Trung bình"
        };
        AnsiConsole.MarkupLine("[grey]1. Cao   2. Trung bình   3. Thấp[/]");
        AnsiConsole.MarkupLine($"[grey]Giá trị hiện tại: {currentName}[/]");
        var choice = InputValidator.PromptInt("Ưu tiên", 1, 3);
        return choice switch
        {
            1 => TaskPriority.High,
            3 => TaskPriority.Low,
            _ => TaskPriority.Medium
        };
    }
}
