using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Tests.Fakes;

/// <summary>
/// Repository giả lập trong bộ nhớ: cho phép test toàn bộ nghiệp vụ
/// mà không đụng tới file system (Dependency Inversion trong action).
/// </summary>
public sealed class InMemoryTaskRepository : ITaskRepository
{
    public List<TaskItem> Store { get; } = new();

    /// <summary>Số lần được ghi — dùng để xác minh service có lưu sau mỗi thao tác.</summary>
    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<TaskItem>> LoadAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TaskItem>>(Store.Select(Clone).ToList());

    public Task SaveAsync(IReadOnlyList<TaskItem> items, CancellationToken ct = default)
    {
        SaveCount++;
        Store.Clear();
        Store.AddRange(items.Select(Clone));
        return Task.CompletedTask;
    }

    private static TaskItem Clone(TaskItem t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Priority = t.Priority,
        DueDate = t.DueDate,
        IsDone = t.IsDone,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
