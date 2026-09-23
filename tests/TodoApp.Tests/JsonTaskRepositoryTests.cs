using System.Text.Json;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Tests;

/// <summary>
/// Test đọc/ghi file JSON thật (dùng file tạm trong thư mục test),
/// bao phủ cả nhánh lỗi: file không tồn tại và file bị hỏng.
/// </summary>
public class JsonTaskRepositoryTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"todo-test-{Guid.NewGuid():N}.json");

    private JsonTaskRepository Create() => new(_path);

    [Fact]
    public async Task Load_WhenFileMissing_ReturnsEmptyList()
    {
        var repo = Create();

        var items = await repo.LoadAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsAllFields()
    {
        var repo = Create();
        var original = new TaskItem
        {
            Title = "Bài tập Windows",
            Description = "Nộp readme + zip",
            Priority = TaskPriority.High,
            DueDate = new DateOnly(2026, 12, 31),
            IsDone = true
        };

        await repo.SaveAsync(new[] { original });
        var loaded = (await repo.LoadAsync()).Single();

        Assert.Equal(original.Id, loaded.Id);
        Assert.Equal(original.Title, loaded.Title);
        Assert.Equal(original.Description, loaded.Description);
        Assert.Equal(TaskPriority.High, loaded.Priority);   // enum round-trip an toàn
        Assert.Equal(original.DueDate, loaded.DueDate);
        Assert.True(loaded.IsDone);
    }

    [Fact]
    public async Task Save_WritesReadableJson_WithEnumAsString()
    {
        var repo = Create();
        await repo.SaveAsync(new[] { new TaskItem { Title = "X", Priority = TaskPriority.Medium } });

        var json = await File.ReadAllTextAsync(_path);

        using var doc = JsonDocument.Parse(json);           // file phải hợp lệ JSON
        var priority = doc.RootElement[0].GetProperty("priority").GetString();
        Assert.Equal("Medium", priority);                   // lưu "Medium", không phải 1
    }

    [Fact]
    public async Task Load_WhenFileCorrupted_ReturnsEmptyInsteadOfCrashing()
    {
        await File.WriteAllTextAsync(_path, "{ this is not valid json !!!");
        var repo = Create();

        var items = await repo.LoadAsync();                 // không được ném exception

        Assert.Empty(items);
    }

    [Fact]
    public async Task Save_OverwritesPreviousContent()
    {
        var repo = Create();
        await repo.SaveAsync(new[] { new TaskItem { Title = "A" }, new TaskItem { Title = "B" } });
        await repo.SaveAsync(new[] { new TaskItem { Title = "C" } });

        var items = await repo.LoadAsync();

        Assert.Single(items);
        Assert.Equal("C", items[0].Title);
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
        GC.SuppressFinalize(this);
    }
}
