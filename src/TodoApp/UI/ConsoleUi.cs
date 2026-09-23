using Spectre.Console;
using Spectre.Console.Rendering;
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
    // Light retro palette: lấy cảm hứng từ giao diện game ASCII/pixel art.
    // Dùng mã HEX để màu nhất quán giữa Windows Terminal và terminal ANSI.
    private const string Ink = "#20262E";
    private const string Muted = "#66727D";
    private const string Teal = "#00AFA5";
    private const string Green = "#18A558";
    private const string Gold = "#C89B00";
    private const string Coral = "#E5484D";

    private static readonly Color TealColor = new(0, 175, 165);
    private static readonly Color GreenColor = new(24, 165, 88);
    private static readonly Color GoldColor = new(200, 155, 0);
    private static readonly Color CoralColor = new(229, 72, 77);
    private static readonly Color InkColor = new(32, 38, 46);
    private static readonly Color MutedColor = new(102, 114, 125);
    private static readonly Color SurfaceColor = new(244, 241, 234);

    /// <summary>Chiều rộng nội dung tối đa; chừa lề hai bên để bố cục cân giữa.</summary>
    private static int ContentWidth => Math.Min(124, Math.Max(64, AnsiConsole.Profile.Width - 8));

    private static void WriteCentered(IRenderable renderable)
    {
        // Khóa vùng căn giữa theo cùng ContentWidth. Nếu chỉ Align trực tiếp
        // một Markup ngắn, một số terminal có thể đo sai viewport và cắt đầu dòng.
        var content = new Align(renderable, HorizontalAlignment.Center)
            .Width(ContentWidth);
        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center));
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Áp dụng nền sáng cho toàn bộ cửa sổ console. Spectre.Console đảm nhiệm
    /// foreground chi tiết; ConsoleColor cung cấp màu nền mặc định.
    /// </summary>
    public static void ApplyLightTheme()
    {
        try
        {
            if (!Console.IsOutputRedirected && Console.Out is not LightThemeTextWriter)
                Console.SetOut(new LightThemeTextWriter(Console.Out));

            // OSC 10/11 đổi màu foreground/background mặc định của terminal.
            // Nhờ đó mã SGR reset do Spectre.Console phát ra vẫn quay về light
            // theme thay vì nền đen mặc định. Windows Terminal hỗ trợ đầy đủ.
            if (!Console.IsOutputRedirected)
                Console.Write("\u001b]10;#20262E\u0007\u001b]11;#F4F1EA\u0007");

            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;

            if (!Console.IsOutputRedirected) Console.Clear();

            // Quan trọng: đặt default style ngay trong Spectre.Console. Nếu chỉ
            // đổi ConsoleColor, mỗi lần Spectre reset ANSI sẽ quay về nền đen.
            AnsiConsole.Background = SurfaceColor;
            AnsiConsole.Foreground = InkColor;

            // Console.SetOut dùng decorator có thể khiến thư viện không tự nhận
            // đúng kích thước host cũ. Khai báo rõ width để tránh buffer ngang.
            if (!Console.IsOutputRedirected)
                AnsiConsole.Profile.Width = Console.WindowWidth;
        }
        catch (IOException)
        {
            // Một số host/CI không cho thay đổi màu console; app vẫn chạy được.
        }
    }

    /// <summary>Trả màu terminal về profile ban đầu sau khi ứng dụng kết thúc.</summary>
    public static void RestoreTerminalTheme()
    {
        try
        {
            if (!Console.IsOutputRedirected)
            {
                // OSC 110/111: reset dynamic foreground/background color.
                Console.Out.Write("\u001b[0m\u001b]110\u0007\u001b]111\u0007");
                Console.Out.Flush();
            }
            Console.ResetColor();
        }
        catch (IOException)
        {
            // Terminal đã đóng thì không cần khôi phục thêm.
        }
    }

    private static string PriorityMarkup(TaskPriority priority) => priority switch
    {
        TaskPriority.High => $"[bold {Coral}]● Cao[/]",
        TaskPriority.Medium => $"[bold {Gold}]● Trung bình[/]",
        _ => $"[bold {Teal}]● Thấp[/]"
    };

    /// <summary>Xóa màn hình và vẽ thanh tiêu đề nhất quán cho mỗi view.</summary>
    public static void BeginView(string title, string? subtitle = null)
    {
        // Khi chạy tương tác: refresh như ứng dụng desktop. Khi input/output bị
        // redirect (E2E test, chụp demo): giữ lịch sử để công cụ kiểm tra được output.
        if (!Console.IsOutputRedirected && !Console.IsInputRedirected) AnsiConsole.Clear();

        var heading = new Align(
            new Markup($"[bold {Teal}]✓ TODO DESK[/]  [{Muted}]•[/]  [bold {Ink}]{Markup.Escape(title)}[/]"),
            HorizontalAlignment.Center);
        var headingPanel = new Panel(heading)
            .Border(BoxBorder.Rounded)
            .BorderColor(TealColor)
            .Padding(1, 0);
        headingPanel.Width = ContentWidth;
        WriteCentered(headingPanel);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            WriteCentered(new Markup($"[{Muted}]{Markup.Escape(subtitle)}[/]"));
        }
    }

    /// <summary>Vẽ dashboard gồm thống kê, tiến độ và các công việc gần nhất.</summary>
    public static void Dashboard(IReadOnlyList<TaskItem> items, string? notification = null)
    {
        BeginView("BẢNG ĐIỀU KHIỂN", "Dùng phím ↑/↓ để di chuyển • Enter để chọn");

        RenderHero();

        var done = items.Count(t => t.IsDone);
        var active = items.Count - done;
        var overdue = items.Count(t => t.IsOverdue());
        var percent = items.Count == 0 ? 0 : (int)Math.Round(done * 100d / items.Count);

        var cards = new Grid()
            .Width(ContentWidth)
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap());
        cards.AddRow(
            StatCard("TỔNG CỘNG", items.Count.ToString(), Teal, TealColor),
            StatCard("CHƯA XONG", active.ToString(), Gold, GoldColor),
            StatCard("HOÀN THÀNH", done.ToString(), Green, GreenColor),
            StatCard("QUÁ HẠN", overdue.ToString(), Coral, CoralColor));
        WriteCentered(cards);

        // BreakdownChart cần thêm không gian cho label/legend bên dưới. Nếu bar
        // rộng sát panel, Spectre có thể mở rộng buffer ngang và làm lệch view.
        var progress = new BreakdownChart().Width(Math.Max(40, ContentWidth - 32));
        if (items.Count == 0)
            progress.AddItem("Chưa có dữ liệu", 1, MutedColor);
        else
        {
            if (done > 0) progress.AddItem("Đã xong", done, GreenColor);
            if (active - overdue > 0) progress.AddItem("Đang làm", active - overdue, TealColor);
            if (overdue > 0) progress.AddItem("Quá hạn", overdue, CoralColor);
        }

        var progressPanel = new Panel(progress)
            .Header($"[bold] TIẾN ĐỘ {percent}% [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(GoldColor);
        progressPanel.Width = ContentWidth;
        WriteCentered(progressPanel);

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
            var emptyPanel = new Panel(new Align(new Markup(
                    $"[{Muted}]Chưa có công việc nào.[/]\n[bold {Teal}]Hãy chọn “Thêm công việc” để bắt đầu.[/]"),
                    HorizontalAlignment.Center))
                .Header("[bold] DANH SÁCH TRỐNG [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(TealColor);
            emptyPanel.Width = Math.Min(64, ContentWidth);
            WriteCentered(emptyPanel);
        }

        if (!string.IsNullOrWhiteSpace(notification))
        {
            var noticePanel = new Panel(new Align(
                    new Markup($"[green]✓[/] {Markup.Escape(notification)}"),
                    HorizontalAlignment.Center))
                .Border(BoxBorder.Rounded)
                .BorderColor(GreenColor);
            noticePanel.Width = Math.Min(80, ContentWidth);
            WriteCentered(noticePanel);
        }

        RenderFooter();
    }

    /// <summary>
    /// Biểu tượng Task-Bot và wordmark TODO dùng ký tự block theo phong cách
    /// retro game. Bố cục xếp dọc và đối xứng quanh trục giữa màn hình.
    /// </summary>
    private static void RenderHero()
    {
        const string mascot = """
                ▄████████████▄
                █  ■      ■  █
                █     ▄▄     █
                ▀███  TASK ███▀
                    ▀▀▀▀
            """;

        const string logo = """
             ████████╗ ██████╗ ██████╗  ██████╗
             ╚══██╔══╝██╔═══██╗██╔══██╗██╔═══██╗
                ██║   ██║   ██║██║  ██║██║   ██║
                ██║   ╚██████╔╝██████╔╝╚██████╔╝
                ╚═╝    ╚═════╝ ╚═════╝  ╚═════╝
            """;

        const string leftDecoration = """
            ◆ ───────── ◆
              ▪  ▫  ▪
            ╱╲╱╲╱╲╱╲╱╲
              ▫  ▪  ▫
            ◆ ───────── ◆
            """;

        const string rightDecoration = """
            ◆ ───────── ◆
              ▪  ▫  ▪
            ╲╱╲╱╲╱╲╱╲╱
              ▫  ▪  ▫
            ◆ ───────── ◆
            """;

        var center = new Grid().AddColumn(new GridColumn().Centered());
        center.AddRow(new Text(mascot, new Style(GoldColor, decoration: Decoration.Bold)));
        center.AddRow(new Text(logo, new Style(TealColor, decoration: Decoration.Bold)));

        if (ContentWidth >= 100)
        {
            var hero = new Grid()
                .Width(Math.Min(112, ContentWidth))
                .AddColumn(new GridColumn().Width(24).Centered())
                .AddColumn(new GridColumn().Width(64).Centered())
                .AddColumn(new GridColumn().Width(24).Centered());
            hero.AddRow(
                new Text(leftDecoration, new Style(MutedColor)),
                center,
                new Text(rightDecoration, new Style(MutedColor)));
            WriteCentered(hero);
        }
        else
        {
            // Cửa sổ hẹp: ưu tiên logo, bỏ họa tiết hai bên để không bị wrap.
            WriteCentered(center);
        }

        WriteCentered(new Markup(
            $"[bold {Green}]MAKE A PLAN.[/]  [bold {Teal}]DO THE WORK.[/]  [bold {Gold}]ENJOY THE WIN.[/]"));
    }

    private static void RenderFooter()
    {
        var footer = new Panel(new Align(
                new Markup($"[{Muted}]DỰ ÁN ĐƯỢC THỰC HIỆN BỞI[/]  [bold {Ink}]TRẦN KIM YẾN[/]  [{Muted}]• 23120193[/]"),
                HorizontalAlignment.Center))
            .Border(BoxBorder.Rounded)
            .BorderColor(MutedColor)
            .Padding(1, 0);
        footer.Width = ContentWidth;
        WriteCentered(footer);
    }

    private static Panel StatCard(string label, string value, string markupColor, Color borderColor) =>
        new(new Align(new Markup($"[bold {markupColor}]{value}[/]\n[{Muted}]{label}[/]"), HorizontalAlignment.Center))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(borderColor),
            Padding = new Padding(2, 0)
        };

    /// <summary>In bảng task có màu, trạng thái, deadline và mô tả.</summary>
    public static void RenderTasks(IReadOnlyList<TaskItem> items, string title, bool showDescription = true)
    {
        if (items.Count == 0)
        {
            var emptyPanel = new Panel($"[{Muted}]Không có công việc phù hợp.[/]")
                .Header($"[bold {Teal}] {Markup.Escape(title)} [/]")
                .BorderColor(TealColor);
            emptyPanel.Width = Math.Min(64, ContentWidth);
            WriteCentered(emptyPanel);
            return;
        }

        var table = new Table()
            .Width(ContentWidth)
            .Border(TableBorder.Rounded)
            .BorderColor(TealColor)
            .Title($"[bold {Teal}] {Markup.Escape(title)} [/]")
            .AddColumn(new TableColumn($"[bold {Muted}]#[/]").Centered().Width(3))
            .AddColumn(new TableColumn("[bold]Trạng thái[/]").Centered())
            .AddColumn(new TableColumn("[bold]Ưu tiên[/]").Centered())
            .AddColumn(new TableColumn("[bold]Tiêu đề[/]"))
            .AddColumn(new TableColumn("[bold]Hạn chót[/]").Centered());

        if (showDescription) table.AddColumn(new TableColumn("[bold]Mô tả[/]"));

        for (var i = 0; i < items.Count; i++)
        {
            var t = items[i];
            var status = t.IsDone ? $"[bold {Green}]✓ Xong[/]" : $"[{Muted}]○ Chưa xong[/]";
            var due = t.DueDate is null
                ? $"[{Muted}]Không hạn[/]"
                : t.IsOverdue()
                    ? $"[bold {Coral}]{t.DueDate:dd/MM/yyyy} ⚠[/]"
                    : $"[{Ink}]{t.DueDate:dd/MM/yyyy}[/]";
            var taskTitle = t.IsDone
                ? $"[strike {Muted}]{Markup.Escape(t.Title)}[/]"
                : $"[bold {Ink}]{Markup.Escape(t.Title)}[/]";

            var cells = new List<string>
            {
                (i + 1).ToString(), status, PriorityMarkup(t.Priority), taskTitle, due
            };
            if (showDescription)
                cells.Add(Markup.Escape(t.Description.Length0() == 0 ? "—" : t.Description));

            table.AddRow(cells.ToArray());
        }

        WriteCentered(table);
    }

    /// <summary>Menu TUI dùng phím mũi tên; tự fallback sang nhập số khi test bằng pipe.</summary>
    public static int MainMenu(string[] choices)
    {
        if (Console.IsInputRedirected) return ReadNumericChoice(choices.Length);

        var selectedIndex = 0;
        var top = Console.CursorTop;

        try
        {
            Console.CursorVisible = false;

            while (true)
            {
                RenderMainMenu(choices, selectedIndex, top);

                switch (Console.ReadKey(intercept: true).Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + choices.Length) % choices.Length;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % choices.Length;
                        break;
                    case ConsoleKey.Enter:
                        return selectedIndex + 1;
                }
            }
        }
        finally
        {
            try { Console.CursorVisible = true; }
            catch (IOException) { /* Terminal đã đóng. */ }
        }
    }

    private static void RenderMainMenu(string[] choices, int selectedIndex, int top)
    {
        var menuWidth = Math.Clamp(choices.Max(choice => choice.Length) + 8, 38, 52);
        var left = Math.Max(0, (Console.WindowWidth - menuWidth) / 2);
        var title = "Bạn muốn làm gì tiếp theo?";
        var titleLeft = Math.Max(0, (Console.WindowWidth - title.Length) / 2);

        WriteMenuLine(top, $"{new string(' ', titleLeft)}[bold {Teal}]{title}[/]");

        for (var i = 0; i < choices.Length; i++)
        {
            var label = $"{(i == selectedIndex ? "›" : " ")}  {choices[i]}".PadRight(menuWidth);
            var line = i == selectedIndex
                ? $"{new string(' ', left)}[bold white on {Teal}]{Markup.Escape(label)}[/]"
                : $"{new string(' ', left)}[{Ink}]{Markup.Escape(label)}[/]";
            WriteMenuLine(top + i + 1, line);
        }

        Console.SetCursorPosition(0, Math.Min(Console.BufferHeight - 1, top + choices.Length + 1));
    }

    private static void WriteMenuLine(int row, string markup)
    {
        Console.SetCursorPosition(0, row);
        Console.Write(new string(' ', Math.Max(1, Console.WindowWidth - 1)));
        Console.SetCursorPosition(0, row);
        AnsiConsole.Markup(markup);
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
            .HighlightStyle(new Style(Color.White, TealColor, Decoration.Bold))
            .AddChoices(options.Keys));
        return options[selected];
    }

    public static TaskItem PickTask(IReadOnlyList<TaskItem> items, string title)
    {
        if (Console.IsInputRedirected)
            return items[InputValidator.PromptInt("Nhập số thứ tự", 1, items.Count) - 1];

        return AnsiConsole.Prompt(new SelectionPrompt<TaskItem>()
            .Title($"[bold]{Markup.Escape(title)}[/]")
            // Spectre.Console yêu cầu PageSize tối thiểu là 3, kể cả khi
            // danh sách chỉ có 1-2 lựa chọn.
            .PageSize(GetTaskPromptPageSize(items.Count))
            .HighlightStyle(new Style(Color.White, TealColor, Decoration.Bold))
            .UseConverter(t => $"{(t.IsDone ? "✓" : "○")}  {Markup.Escape(t.Title)}  [{Muted}]• {PriorityText(t.Priority)}[/]")
            .AddChoices(items));
    }

    /// <summary>
    /// Spectre.Console yêu cầu page size từ 3 trở lên. Giới hạn trên 10 giúp
    /// prompt không chiếm toàn bộ màn hình khi có nhiều công việc.
    /// </summary>
    internal static int GetTaskPromptPageSize(int itemCount)
    {
        if (itemCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(itemCount), "Danh sách phải có ít nhất một công việc.");

        return Math.Clamp(itemCount, 3, 10);
    }

    public static TaskPriority PromptPriority(TaskPriority current = TaskPriority.Medium)
    {
        if (Console.IsInputRedirected)
        {
            AnsiConsole.MarkupLine($"[{Muted}]1. Cao   2. Trung bình   3. Thấp[/]");
            return InputValidator.PromptInt("Ưu tiên", 1, 3) switch
            {
                1 => TaskPriority.High,
                3 => TaskPriority.Low,
                _ => TaskPriority.Medium
            };
        }

        var options = new Dictionary<string, TaskPriority>
        {
            [$"[{Coral}]●[/]  Cao — cần ưu tiên xử lý"] = TaskPriority.High,
            [$"[{Gold}]●[/]  Trung bình"] = TaskPriority.Medium,
            [$"[{Teal}]●[/]  Thấp"] = TaskPriority.Low
        };
        var currentLabel = options.First(x => x.Value == current).Key;
        var selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Mức độ ưu tiên[/]")
            .HighlightStyle(new Style(Color.White, TealColor, Decoration.Bold))
            .AddChoices(new[] { currentLabel }.Concat(options.Keys.Where(x => x != currentLabel))));
        return options[selected];
    }

    public static bool Confirm(string question) => Console.IsInputRedirected
        ? ReadRedirectedConfirmation(question)
        : AnsiConsole.Confirm($"[bold yellow]{Markup.Escape(question)}[/]", defaultValue: false);

    public static void FormHeader(string icon, string title, string hint) =>
        AnsiConsole.Write(new Panel(new Markup($"[{Muted}]{Markup.Escape(hint)}[/]"))
            .Header($"[bold {Teal}] {icon}  {Markup.Escape(title)} [/]")
            .Border(BoxBorder.Double)
            .BorderColor(TealColor));

    public static void Success(string message) =>
        AnsiConsole.Write(new Panel(new Markup($"[bold {Green}]✓ {Markup.Escape(message)}[/]"))
            .Border(BoxBorder.Rounded).BorderColor(GreenColor));

    public static void Warn(string message) =>
        AnsiConsole.Write(new Panel(new Markup($"[{Gold}]⚠ {Markup.Escape(message)}[/]"))
            .Border(BoxBorder.Rounded).BorderColor(GoldColor));

    public static void Error(string message) =>
        AnsiConsole.MarkupLine($"[bold {Coral}]✗ {Markup.Escape(message)}[/]");

    public static void Pause()
    {
        if (Console.IsInputRedirected) return;
        AnsiConsole.Markup($"\n[{Muted}]Nhấn [bold {Ink}]Enter[/] để quay lại bảng điều khiển...[/]");
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
        AnsiConsole.Markup($"[bold {Gold}]{Markup.Escape(question)}[/] [{Muted}](y/n):[/] ");
        return ConsoleInput.ReadLine().OneLine().ToLowerInvariant() is "y" or "yes";
    }

    private static string PriorityText(TaskPriority priority) => priority switch
    {
        TaskPriority.High => "Cao",
        TaskPriority.Low => "Thấp",
        _ => "Trung bình"
    };
}
