using TodoApp.Models;
using TodoApp.Services;
using TodoApp.Tests.Fakes;

namespace TodoApp.Tests;

public class TaskServiceTests
{
    private readonly InMemoryTaskRepository _repo = new();
    private readonly TaskService _service;

    public TaskServiceTests() => _service = new TaskService(_repo);

    // ---------- Add ----------

    [Fact]
    public async Task AddAsync_AppendsItem_AndPersists()
    {
        var item = await _service.AddAsync("Học C#", "Bài tập chương 3",
            TaskPriority.High, new DateOnly(2026, 12, 25));

        Assert.Single(_service.Items);
        Assert.Equal("Học C#", item.Title);
        Assert.Equal(TaskPriority.High, item.Priority);
        Assert.Equal(1, _repo.SaveCount);          // mỗi thao tác = 1 lần ghi
        Assert.Single(_repo.Store);                 // dữ liệu đã về repository
    }

    [Fact]
    public async Task AddAsync_TrimsTitle_AndAllowsEmptyDescription()
    {
        var item = await _service.AddAsync("   Gọi điện cho thầy   ", null,
            TaskPriority.Low, null);

        Assert.Equal("Gọi điện cho thầy", item.Title);
        Assert.Equal(string.Empty, item.Description);
        Assert.Null(item.DueDate);
    }

    [Fact]
    public async Task AddAsync_SeveralItems_KeepsInsertOrder()
    {
        await _service.AddAsync("A", null, TaskPriority.Low, null);
        await _service.AddAsync("B", null, TaskPriority.Low, null);
        await _service.AddAsync("C", null, TaskPriority.Low, null);

        Assert.Equal(new[] { "A", "B", "C" }, _service.Items.Select(t => t.Title));
    }

    // ---------- Toggle ----------

    [Fact]
    public async Task ToggleDoneAsync_FlipsStatus_Twice()
    {
        var item = await _service.AddAsync("Xong việc", null, TaskPriority.Medium, null);

        Assert.True(await _service.ToggleDoneAsync(item.Id));
        Assert.True(item.IsDone);

        Assert.True(await _service.ToggleDoneAsync(item.Id));
        Assert.False(item.IsDone);
    }

    [Fact]
    public async Task ToggleDoneAsync_UnknownId_ReturnsFalse()
    {
        Assert.False(await _service.ToggleDoneAsync(Guid.NewGuid()));
        Assert.Empty(_service.Items);
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateAsync_ChangesAllFields_AndBumpsUpdatedAt()
    {
        var item = await _service.AddAsync("Cũ", "mô tả cũ", TaskPriority.Low, null);
        var oldUpdated = item.UpdatedAt;

        var ok = await _service.UpdateAsync(item.Id, "Mới", "mô tả mới",
            TaskPriority.High, new DateOnly(2026, 1, 1));

        Assert.True(ok);
        Assert.Equal("Mới", item.Title);
        Assert.Equal("mô tả mới", item.Description);
        Assert.Equal(TaskPriority.High, item.Priority);
        Assert.Equal(new DateOnly(2026, 1, 1), item.DueDate);
        Assert.True(item.UpdatedAt >= oldUpdated);
    }

    // ---------- Remove ----------

    [Fact]
    public async Task RemoveAsync_DeletesOnlyTarget()
    {
        var keep = await _service.AddAsync("Giữ", null, TaskPriority.Low, null);
        var delete = await _service.AddAsync("Xóa", null, TaskPriority.Low, null);

        Assert.True(await _service.RemoveAsync(delete.Id));
        Assert.Single(_service.Items);                       // còn đúng "Giữ"
        Assert.Equal("Giữ", _service.Items[0].Title);
        Assert.Single(_repo.Store);                          // repo đã đồng bộ
        Assert.Equal(keep.Id, _service.Items[0].Id);
    }

    [Fact]
    public async Task RemoveAsync_UnknownId_ReturnsFalse()
        => Assert.False(await _service.RemoveAsync(Guid.NewGuid()));

    // ---------- Search (LINQ) ----------

    [Fact]
    public async Task Search_MatchesTitleAndDescription_CaseInsensitive()
    {
        await _service.AddAsync("Đọc Sách hay", null, TaskPriority.Low, null);
        await _service.AddAsync("Tập gym", "kế hoạch sức khỏe", TaskPriority.Low, null);
        await _service.AddAsync("Nấu ăn", null, TaskPriority.Low, null);
        await _service.AddAsync("Mua sách vở", null, TaskPriority.Low, null);

        Assert.Equal(2, _service.Search("SÁCH").Count);          // không phân biệt hoa thường
        Assert.Single(_service.Search("sức khỏe"));              // tìm trong mô tả
        Assert.Equal(4, _service.Search("").Count);              // rỗng → trả toàn bộ
        Assert.Empty(_service.Search("không tồn tại"));
    }

    // ---------- Filter ----------

    [Fact]
    public async Task Filter_SplitsActiveDoneOverdue()
    {
        var done = await _service.AddAsync("Đã xong", null, TaskPriority.Low, null);
        await _service.AddAsync("Quá hạn", null, TaskPriority.Low, new DateOnly(2020, 1, 1));
        await _service.AddAsync("Bình thường", null, TaskPriority.Low, null);
        await _service.ToggleDoneAsync(done.Id);

        Assert.Equal(3, _service.Filter(TaskFilter.All).Count);
        Assert.Equal(2, _service.Filter(TaskFilter.Active).Count);
        Assert.Single(_service.Filter(TaskFilter.Done));
        Assert.Single(_service.Filter(TaskFilter.Overdue));      // quá hạn & chưa xong
    }

    [Fact]
    public async Task Filter_DoneItem_IsNeverOverdue()
    {
        var item = await _service.AddAsync("Xong sớm", null, TaskPriority.Low,
            new DateOnly(2020, 1, 1));
        await _service.ToggleDoneAsync(item.Id);

        Assert.Empty(_service.Filter(TaskFilter.Overdue));
        Assert.False(item.IsOverdue());
    }

    // ---------- Load ----------

    [Fact]
    public async Task InitializeAsync_LoadsItemsFromRepository()
    {
        _repo.Store.Add(new TaskItem { Title = "Có sẵn" });

        await _service.InitializeAsync();

        Assert.Single(_service.Items);
        Assert.Equal("Có sẵn", _service.Items[0].Title);
    }
}
