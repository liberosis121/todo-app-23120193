using Spectre.Console;
using TodoApp.Models;
using TodoApp.Services;
using TodoApp.Utils;

namespace TodoApp.UI;

/// <summary>
/// Presentation layer dùng Spectre.Console để tạo giao diện TUI (Terminal UI).
/// Mọi màu sắc, panel, bảng và prompt đều được tập trung ở đây để không làm
/// tầng nghiệp vụ phụ thuộc vào console.
/// </summary>
public static class ConsoleUi
{
    private const string Accent = "deepskyblue1";

    private static string PriorityMarkup(TaskPriority priority) => priority switch
    {
        TaskPriority.High => "[bold red]● Cao[/]",
        TaskPriority.Medium => "[bold yellow]● Trung bình[/]",
        _ => "[bold cornflowerblue]● Thấp[/]"
    };

    /// <summary>Xóa màn hình và vẽ thanh tiêu đề nhất quán cho mỗi view.</summary>
    public static void BeginView(string title, string? subtitle = null)
    {
        // Khi chạy tương tác: refresh như ứng dụng desktop. Khi input/output bị
        // redirect (E2E test, chụp demo): giữ lịch sử để công cụ kiểm tra được output.
        if (!Console.IsOutputRedirected && !Console.IsInputRedirected) AnsiConsole.Clear();

        var heading = new Grid().AddColumn().AddColumn(new GridColumn().RightAligned());
        heading.AddRow(
            new Markup($"[bold {Accent}]✓ TODO DESK[/]  [grey]•[/]  [bold white]{Markup.Escape(title)}[/]"),
            new Markup("[grey]MSSV 23120193[/]"));

        AnsiConsole.Write(new Panel(heading)
            .Border(BoxBorder.Heavy)
            .BorderColor(Color.DeepSkyBlue1)
            .Padding(1, 0));

        if (!string.IsNullOrWhiteSpace(subtitle))
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(subtitle)}[/]\n");
    }

    /// <summary>Vẽ dashboard gồm thống kê, tiến độ và các công việc gần nhất.</summary>
    public static void Dashboard(IReadOnlyList<TaskItem> items, string? notification = null)
    {
        BeginView("BẢNG ĐIỀU KHIỂN", "Dùng phím ↑/↓ để di chuyển • Enter để chọn");

        var done = items.Count(t => t.IsDone);
        var active = items.Count - done;
        var overdue = items.Count(t => t.IsOverdue());
        var percent = items.Count == 0 ? 0 : (int)Math.Round(done * 100d / items.Count);

        var cards = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap());
        cards.AddRow(
            StatCard("TỔNG CỘNG", items.Count.ToString(), Color.DeepSkyBlue1),
            StatCard("CHƯA XONG", active.ToString(), Color.Yellow),
            StatCard("HOÀN THÀNH", done.ToString(), Color.Green),
            StatCard("QUÁ HẠN", overdue.ToString(), Color.Red));
        AnsiConsole.Write(cards);

        var progress = new BreakdownChart().Width(60);
        if (items.Count == 0)
            progress.AddItem("Chưa có dữ liệu", 1, Color.Grey);
        else
        {
            if (done > 0) progress.AddItem("Đã xong", done, Color.Green);
            if (active - overdue > 0) progress.AddItem("Đang làm", active - overdue, Color.DeepSkyBlue1);
            if (overdue > 0) progress.AddItem("Quá hạn", overdue, Color.Red);
        }

        AnsiConsole.Write(new Panel(progress)
            .Header($"[bold] TIẾN ĐỘ {percent}% [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey));

        if (items.Count > 0)
        {
            var recent = items
                .OrderBy(t => t.IsDone)
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate ?? DateOnly.MaxValue)
                .Take(5)
                .ToList();
            RenderTasks(recent, "CÔNG VIỆC CẦN CHÚ Ý", showDescription: false);
        }
        else
        {
            AnsiConsole.Write(new Panel(new Markup(
                    "[grey]Chưa có công việc nào.[/]\n[deepskyblue1]Hãy chọn “Thêm công việc” để bắt đầu.[/]"))
                .Header("[bold] DANH SÁCH TRỐNG [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey));
        }

        if (!string.IsNullOrWhiteSpace(notification))
            AnsiConsole.Write(new Panel(new Markup($"[green]✓[/] {Markup.Escape(notification)}"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green));
    }

    private static Panel StatCard(string label, string value, Color color) =>
        new(new Align(new Markup($"[bold {color}]{value}[/]\n[grey]{label}[/]"), HorizontalAlignment.Center))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(color),
            Padding = new Padding(2, 0)
        };

    /// <summary>In bảng task có màu, trạng thái, deadline và mô tả.</summary>
    public static void RenderTasks(IReadOnlyList<TaskItem> items, string title, bool showDescription = true)
    {
        if (items.Count == 0)
        {
            AnsiConsole.Write(new Panel("[grey]Không có công việc phù hợp.[/]")
                .Header($"[bold {Accent}] {Markup.Escape(title)} [/]")
                .BorderColor(Color.Grey));
            return;
        }

        var table = new Table()
            .Expand()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title($"[bold {Accent}] {Markup.Escape(title)} [/]")
            .AddColumn(new TableColumn("[bold grey]#[/]").Centered().Width(3))
            .AddColumn(new TableColumn("[bold]Trạng thái[/]").Centered())
            .AddColumn(new TableColumn("[bold]Ưu tiên[/]").Centered())
            .AddColumn(new TableColumn("[bold]Tiêu đề[/]"))
            .AddColumn(new TableColumn("[bold]Hạn chót[/]").Centered());

        if (showDescription) table.AddColumn(new TableColumn("[bold]Mô tả[/]"));

        for (var i = 0; i < items.Count; i++)
        {
            var t = items[i];
            var status = t.IsDone ? "[bold green]✓ Xong[/]" : "[grey]○ Chưa xong[/]";
            var due = t.DueDate is null
                ? "[grey]Không hạn[/]"
                : t.IsOverdue()
                    ? $"[bold red]{t.DueDate:dd/MM/yyyy} ⚠[/]"
                    : $"[white]{t.DueDate:dd/MM/yyyy}[/]";
            var taskTitle = t.IsDone
                ? $"[strike grey]{Markup.Escape(t.Title)}[/]"
                : $"[bold white]{Markup.Escape(t.Title)}[/]";

            var cells = new List<string>
            {
                (i + 1).ToString(), status, PriorityMarkup(t.Priority), taskTitle, due
            };
            if (showDescription)
                cells.Add(Markup.Escape(t.Description.Length0() == 0 ? "—" : t.Description));

            table.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(table);
    }

    /// <summary>Menu TUI dùng phím mũi tên; tự fallback sang nhập số khi test bằng pipe.</summary>
    public static int MainMenu(string[] choices)
    {
        if (Console.IsInputRedirected) return ReadNumericChoice(choices.Length);

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold {Accent}]Bạn muốn làm gì tiếp theo?[/]")
                .PageSize(choices.Length)
                .HighlightStyle(new Style(Color.Black, Color.DeepSkyBlue1, Decoration.Bold))
                .AddChoices(choices));

        return Array.IndexOf(choices, selected) + 1;
    }

    public static TaskFilter PromptFilter()
    {
        if (Console.IsInputRedirected)
            return (TaskFilter)(InputValidator.PromptInt("Lọc", 1, 4) - 1);

        var options = new Dictionary<string, TaskFilter>
        {
            ["▣  Tất cả công việc"] = TaskFilter.All,
            ["○  Chưa hoàn thành"] = TaskFilter.Active,
            ["✓  Đã hoàn thành"] = TaskFilter.Done,
            ["⚠  Đã quá hạn"] = TaskFilter.Overdue
        };
        var selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Chọn bộ lọc[/]")
            .HighlightStyle(new Style(Color.Black, Color.DeepSkyBlue1))
            .AddChoices(options.Keys));
        return options[selected];
    }

    public static TaskItem PickTask(IReadOnlyList<TaskItem> items, string title)
    {
        if (Console.IsInputRedirected)
            return items[InputValidator.PromptInt("Nhập số thứ tự", 1, items.Count) - 1];

        return AnsiConsole.Prompt(new SelectionPrompt<TaskItem>()
            .Title($"[bold]{Markup.Escape(title)}[/]")
            .PageSize(Math.Min(10, items.Count))
            .HighlightStyle(new Style(Color.Black, Color.DeepSkyBlue1))
            .UseConverter(t => $"{(t.IsDone ? "✓" : "○")}  {Markup.Escape(t.Title)}  [grey]• {PriorityText(t.Priority)}[/]")
            .AddChoices(items));
    }

    public static TaskPriority PromptPriority(TaskPriority current = TaskPriority.Medium)
    {
        if (Console.IsInputRedirected)
        {
            AnsiConsole.MarkupLine("[grey]1. Cao   2. Trung bình   3. Thấp[/]");
            return InputValidator.PromptInt("Ưu tiên", 1, 3) switch
            {
                1 => TaskPriority.High,
                3 => TaskPriority.Low,
                _ => TaskPriority.Medium
            };
        }

        var options = new Dictionary<string, TaskPriority>
        {
            ["[red]●[/]  Cao — cần ưu tiên xử lý"] = TaskPriority.High,
            ["[yellow]●[/]  Trung bình"] = TaskPriority.Medium,
            ["[cornflowerblue]●[/]  Thấp"] = TaskPriority.Low
        };
        var currentLabel = options.First(x => x.Value == current).Key;
        var selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Mức độ ưu tiên[/]")
            .HighlightStyle(new Style(Color.Black, Color.DeepSkyBlue1))
            .AddChoices(new[] { currentLabel }.Concat(options.Keys.Where(x => x != currentLabel))));
        return options[selected];
    }

    public static bool Confirm(string question) => Console.IsInputRedirected
        ? ReadRedirectedConfirmation(question)
        : AnsiConsole.Confirm($"[bold yellow]{Markup.Escape(question)}[/]", defaultValue: false);

    public static void FormHeader(string icon, string title, string hint) =>
        AnsiConsole.Write(new Panel(new Markup($"[grey]{Markup.Escape(hint)}[/]"))
            .Header($"[bold {Accent}] {icon}  {Markup.Escape(title)} [/]")
            .Border(BoxBorder.Double)
            .BorderColor(Color.DeepSkyBlue1));

    public static void Success(string message) =>
        AnsiConsole.Write(new Panel(new Markup($"[bold green]✓ {Markup.Escape(message)}[/]"))
            .Border(BoxBorder.Rounded).BorderColor(Color.Green));

    public static void Warn(string message) =>
        AnsiConsole.Write(new Panel(new Markup($"[yellow]⚠ {Markup.Escape(message)}[/]"))
            .Border(BoxBorder.Rounded).BorderColor(Color.Yellow));

    public static void Error(string message) =>
        AnsiConsole.MarkupLine($"[bold red]✗ {Markup.Escape(message)}[/]");

    public static void Pause()
    {
        if (Console.IsInputRedirected) return;
        AnsiConsole.Markup("\n[grey]Nhấn [white]Enter[/] để quay lại bảng điều khiển...[/]");
        Console.ReadLine();
    }

    private static int ReadNumericChoice(int max)
    {
        AnsiConsole.Markup($"[bold]Chọn chức năng (1-{max})[/]: ");
        var line = ConsoleInput.ReadLine();
        if (line is null) return max;
        return int.TryParse(line.OneLine(), out var value) && value >= 1 && value <= max ? value : -1;
    }

    private static bool ReadRedirectedConfirmation(string question)
    {
        AnsiConsole.Markup($"[bold yellow]{Markup.Escape(question)}[/] [grey](y/n):[/] ");
        return ConsoleInput.ReadLine().OneLine().ToLowerInvariant() is "y" or "yes";
    }

    private static string PriorityText(TaskPriority priority) => priority switch
    {
        TaskPriority.High => "Cao",
        TaskPriority.Low => "Thấp",
        _ => "Trung bình"
    };
}
