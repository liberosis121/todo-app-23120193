using System.Text.Json.Serialization;

namespace TodoApp.Models;

/// <summary>Mức độ ưu tiên của một công việc.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}

/// <summary>
/// Một ghi chú TODO. Là "Entity" của hệ thống:
/// thuần dữ liệu, không phụ thuộc Console hay file I/O.
/// </summary>
public sealed class TaskItem
{
    /// <summary>Mã định danh duy nhất của công việc.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Tiêu đề công việc (bắt buộc, không được rỗng).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết (tùy chọn).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Mức ưu tiên.</summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>Ngày hạn hoàn thành; <c>null</c> nghĩa là không có hạn.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>Đã hoàn thành hay chưa.</summary>
    public bool IsDone { get; set; }

    /// <summary>Thời điểm tạo (tự động).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Thời điểm cập nhật gần nhất.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>Dấu hiển thị trạng thái: ✓ đã xong, ✗ chưa xong.</summary>
    [JsonIgnore]
    public string Mark => IsDone ? "✓" : "✗";

    /// <summary>Extension method trạng thái: có quá hạn hay không.</summary>
    public bool IsOverdue(DateTime? today = null)
    {
        if (IsDone || DueDate is null) return false;
        var reference = today ?? DateTime.Today;
        return DueDate.Value.ToDateTime(TimeOnly.MinValue) < reference.Date;
    }

    public override string ToString() => $"{Mark} [{Priority}] {Title}";
}
