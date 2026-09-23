using System.Globalization;

namespace TodoApp.Utils;

/// <summary>
/// Các phương thức mở rộng (Extension Methods) phục vụ kiểm tra input.
/// </summary>
public static class StringExtensions
{
    /// <summary>Cắt chuỗi về một dòng, trả về chuỗi rỗng nếu null.</summary>
    public static string OneLine(this string? text) => (text ?? string.Empty).Trim();

    /// <summary>Độ dài thực tế của chuỗi, an toàn với null.</summary>
    public static int Length0(this string? text) => text.OneLine().Length;
}

/// <summary>
/// Bộ kiểm tra input của người dùng: tập trung mọi ràng buộc
/// vào một chỗ để UI chỉ việc gọi.
/// </summary>
public static class InputValidator
{
    private static readonly CultureInfo Vi = CultureInfo.InvariantCulture;

    /// <summary>
    /// Đọc một dòng không rỗng từ console. Lặp lại cho đến khi hợp lệ.
    /// </summary>
    public static string PromptNonEmpty(string label, string? current = null)
    {
        while (true)
        {
            if (current is not null) Console.Write($"{label} [{current}]: ");
            else Console.Write($"{label}: ");

            var line = ConsoleInput.ReadLine();
            if (line is null) return current ?? string.Empty;   // EOF → giữ giá trị cũ

            var value = line.OneLine();
            if (value.Length0() > 0) return value;
            if (current is not null && value.Length0() == 0) return current;

            Console.WriteLine("  ⚠ Giá trị không được để trống.");
        }
    }

    /// <summary>
    /// Đọc một tùy chọn (có thể bỏ trống). Trả về <c>null</c> nếu người dùng Enter.
    /// </summary>
    public static string? PromptOptional(string label)
    {
        Console.Write($"{label} (Enter để bỏ qua): ");
        var line = ConsoleInput.ReadLine();
        if (line is null) return null;

        var value = line.OneLine();
        return value.Length0() == 0 ? null : value;
    }

    /// <summary>
    /// Đọc ngày theo định dạng dd/MM/yyyy hoặc yyyy-MM-dd; hỗ trợ Enter = không có hạn.
    /// </summary>
    public static DateOnly? PromptDate(string label, DateOnly? current = null)
    {
        while (true)
        {
            Console.Write(current is null
                ? $"{label} (dd/MM/yyyy, Enter = không hạn): "
                : $"{label} [{current:dd/MM/yyyy}] (Enter = giữ nguyên): ");

            var line = ConsoleInput.ReadLine();
            if (line is null) return current;                   // EOF → giữ nguyên

            var raw = line.OneLine();
            if (raw.Length0() == 0) return current;

            if (TryParseDate(raw, out var date)) return date;
            Console.WriteLine("  ⚠ Ngày không hợp lệ. Ví dụ: 25/12/2026");
        }
    }

    /// <summary>Thử parse ngày với hai định dạng phổ biến.</summary>
    public static bool TryParseDate(string raw, out DateOnly date)
    {
        string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
        return DateOnly.TryParseExact(raw, formats, Vi, DateTimeStyles.None, out date);
    }

    /// <summary>
    /// Đọc một số nguyên trong khoảng [min, max], lặp lại khi sai.
    /// </summary>
    public static int PromptInt(string label, int min, int max)
    {
        while (true)
        {
            Console.Write($"{label} ({min}-{max}): ");
            var line = ConsoleInput.ReadLine();
            if (line is null) return min;                       // EOF → thoát vòng lặp

            var raw = line.OneLine();
            if (int.TryParse(raw, NumberStyles.Integer, Vi, out var value)
                && value >= min && value <= max)
            {
                return value;
            }
            Console.WriteLine($"  ⚠ Hãy nhập số nguyên từ {min} đến {max}.");
        }
    }
}
