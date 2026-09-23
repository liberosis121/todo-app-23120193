using TodoApp.Models;

namespace TodoApp.Services;

/// <summary>Trạng thái hiển thị khi lọc danh sách.</summary>
public enum TaskFilter
{
    All,
    Active,
    Done,
    Overdue
}

/// <summary>
/// Toàn bộ nghiệp vụ của ứng dụng TODO.
/// Interface giúp dễ mock khi viết unit test.
/// </summary>
public interface ITaskService
{
    IReadOnlyList<TaskItem> Items { get; }

    Task InitializeAsync(CancellationToken ct = default);

    Task<TaskItem> AddAsync(string title, string? description, TaskPriority priority, DateOnly? dueDate);

    Task<bool> UpdateAsync(Guid id, string title, string? description, TaskPriority priority, DateOnly? dueDate);

    Task<bool> ToggleDoneAsync(Guid id);

    Task<bool> RemoveAsync(Guid id);

    TaskItem? FindById(Guid id);

    /// <summary>Tìm theo từ khóa trong tiêu đề và mô tả (LINQ).</summary>
    IReadOnlyList<TaskItem> Search(string keyword);

    /// <summary>Lọc theo trạng thái.</summary>
    IReadOnlyList<TaskItem> Filter(TaskFilter filter);
}
