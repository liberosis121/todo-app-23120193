using System.Text;
using System.Text.RegularExpressions;

namespace TodoApp.UI;

/// <summary>
/// Decorator cho stdout: sau mỗi ANSI reset do thư viện UI phát ra, writer
/// khôi phục foreground/background của light theme. Việc này cần thiết trên
/// Windows Console Host cũ vì SGR 0 luôn quay về nền đen của terminal.
/// </summary>
internal sealed partial class LightThemeTextWriter : TextWriter
{
    private const string Foreground = "\u001b[38;2;32;38;46m";
    private const string Background = "\u001b[48;2;244;241;234m";
    private const string DefaultStyle = Foreground + Background;

    private readonly TextWriter _inner;

    public LightThemeTextWriter(TextWriter inner)
    {
        _inner = inner;
        _inner.Write(DefaultStyle);
    }

    public override Encoding Encoding => _inner.Encoding;

    public override void Write(string? value)
    {
        if (value is not null) _inner.Write(ApplyThemeAfterReset(value));
    }

    public override void Write(char value) => _inner.Write(value);

    public override void Write(char[] buffer, int index, int count) =>
        Write(new string(buffer, index, count));

    public override void Write(ReadOnlySpan<char> buffer) => Write(buffer.ToString());

    public override void WriteLine(string? value)
    {
        Write(value);
        _inner.WriteLine();
    }

    public override void Flush() => _inner.Flush();

    private static string ApplyThemeAfterReset(string value) =>
        SgrPattern().Replace(value, match =>
        {
            var raw = match.Groups[1].Value;
            var codes = raw.Length == 0
                ? new[] { 0 }
                : raw.Split(';').Select(code => int.TryParse(code, out var number) ? number : -1).ToArray();

            var restore = string.Empty;

            // Spectre có thể gộp reset và màu mới trong cùng một SGR, ví dụ
            // "0;38;2;0;175;165m". Chỉ khôi phục kênh không được sequence đó
            // thiết lập lại, nếu không ta sẽ ghi đè màu teal/gold vừa chọn.
            var resetsAll = codes.Contains(0);
            var setsForeground = codes.Contains(38);
            var setsBackground = codes.Contains(48);

            if ((resetsAll || codes.Contains(39)) && !setsForeground)
                restore += Foreground;
            if ((resetsAll || codes.Contains(49)) && !setsBackground)
                restore += Background;

            return match.Value + restore;
        });

    [GeneratedRegex("\\u001B\\[([0-9;]*)m", RegexOptions.Compiled)]
    private static partial Regex SgrPattern();
}
