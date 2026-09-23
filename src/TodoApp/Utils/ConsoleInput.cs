namespace TodoApp.Utils;

/// <summary>
/// Giúp phát hiện kết thúc stream nhập liệu (Ctrl+Z / pipe đóng / console đóng).
/// Tránh vòng lặp menu quay vô hạn khi không còn dữ liệu để đọc.
/// </summary>
public static class ConsoleInput
{
    /// <summary>Đã hết dữ liệu nhập liệu hay chưa.</summary>
    public static bool Eof { get; private set; }

    /// <summary>
    /// Đọc một dòng; trả về <c>null</c> khi hết nhập liệu và đánh dấu <see cref="Eof"/>.
    /// </summary>
    public static string? ReadLine()
    {
        var line = Console.ReadLine();
        if (line is null) Eof = true;
        return line;
    }

    /// <summary>Reset cờ — dùng cho test.</summary>
    public static void Reset() => Eof = false;
}
