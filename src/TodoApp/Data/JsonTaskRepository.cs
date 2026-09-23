using System.Text.Json;
using System.Text.Json.Serialization;
using TodoApp.Models;

namespace TodoApp.Data;

/// <summary>
/// Lưu công việc ra file JSON bằng <see cref="System.Text.Json"/>.
/// Bọc mọi lỗi I/O bằng try/catch để app không sập khi file hỏng.
/// </summary>
public sealed class JsonTaskRepository : ITaskRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,                                  // JSON dễ đọc khi nộp bài
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },        // Ưu tiên lưu dạng "High"
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _path;

    public JsonTaskRepository(string? path = null)
    {
        // Đường dẫn ổn định: nằm cùng thư mục với file .exe
        _path = path ?? Path.Combine(AppContext.BaseDirectory, "todo.data.json");
    }

    /// <summary>Đường dẫn file dữ liệu (dùng cho log/màn hình chính).</summary>
    public string FilePath => _path;

    public async Task<IReadOnlyList<TaskItem>> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(_path)) return Array.Empty<TaskItem>();

            await using var stream = File.OpenRead(_path);
            var items = await JsonSerializer.DeserializeAsync<List<TaskItem>>(stream, Options, ct);
            return items ?? new List<TaskItem>();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // File hỏng / không đọc được → khởi động với danh sách rỗng thay vì crash
            Console.WriteLine($"  ⚠ Không đọc được file dữ liệu ({ex.GetType().Name}). Bắt đầu với danh sách rỗng.");
            return Array.Empty<TaskItem>();
        }
    }

    public async Task SaveAsync(IReadOnlyList<TaskItem> items, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        // Ghi ra file tạm rồi Move → tránh mất dữ liệu nếu mất điện giữa chừng
        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, items, Options, ct);
        }

        File.Move(temp, _path, overwrite: true);
    }
}
