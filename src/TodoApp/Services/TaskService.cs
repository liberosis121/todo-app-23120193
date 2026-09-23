using TodoApp.Data;
using TodoApp.Models;
using TodoApp.Utils;

namespace TodoApp.Services;

/// <summary>
/// Nghiệp vụ TODO: phụ thuộc vào <see cref="ITaskRepository"/> (Dependency Inversion),
/// hoàn toàn không đụng tới Console → dễ unit test.
/// </summary>
public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly List<TaskItem> _items = new();

    public TaskService(ITaskRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>Danh sách hiện tại (chỉ đọc từ phía UI).</summary>
    public IReadOnlyList<TaskItem> Items => _items;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var loaded = await _repository.LoadAsync(ct);
        _items.Clear();
        _items.AddRange(loaded);
    }

    public async Task<TaskItem> AddAsync(string title, string? description, TaskPriority priority, DateOnly? dueDate)
    {
        var item = new TaskItem
        {
            Title = title.OneLine(),
            Description = description.OneLine(),
            Priority = priority,
            DueDate = dueDate
        };

        _items.Add(item);
        await PersistAsync();
        return item;
    }

    public async Task<bool> UpdateAsync(Guid id, string title, string? description, TaskPriority priority, DateOnly? dueDate)
    {
        var item = FindById(id);
        if (item is null) return false;

        item.Title = title.OneLine();
        item.Description = description.OneLine();
        item.Priority = priority;
        item.DueDate = dueDate;
        item.UpdatedAt = DateTime.Now;

        await PersistAsync();
        return true;
    }

    public async Task<bool> ToggleDoneAsync(Guid id)
    {
        var item = FindById(id);
        if (item is null) return false;

        item.IsDone = !item.IsDone;
        item.UpdatedAt = DateTime.Now;
        await PersistAsync();
        return true;
    }

    public async Task<bool> RemoveAsync(Guid id)
    {
        var item = FindById(id);
        if (item is null) return false;

        _items.Remove(item);
        await PersistAsync();
        return true;
    }

    public TaskItem? FindById(Guid id) => _items.FirstOrDefault(t => t.Id == id);

    /// <summary>Tìm kiếm không phân biệt hoa thường bằng LINQ.</summary>
    public IReadOnlyList<TaskItem> Search(string keyword)
    {
        var key = keyword.OneLine().ToLowerInvariant();
        if (key.Length0() == 0) return _items;

        return _items
            .Where(t => t.Title.ToLowerInvariant().Contains(key)
                     || t.Description.ToLowerInvariant().Contains(key))
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Priority)
            .ToList();
    }

    public IReadOnlyList<TaskItem> Filter(TaskFilter filter) => filter switch
    {
        TaskFilter.Active => _items.Where(t => !t.IsDone).ToList(),
        TaskFilter.Done => _items.Where(t => t.IsDone).ToList(),
        TaskFilter.Overdue => _items.Where(t => t.IsOverdue()).ToList(),
        _ => _items.ToList()
    };

    /// <summary>Trả về Task ghi dữ liệu để caller await đến khi lưu hoàn tất.</summary>
    private Task PersistAsync() => _repository.SaveAsync(_items.ToList());
}
