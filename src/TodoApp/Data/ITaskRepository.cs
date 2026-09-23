using TodoApp.Models;

namespace TodoApp.Data;

/// <summary>
/// Hợp đồng lưu trữ: mọi phụ thuộc chỉ nhìn thấy interface này,
/// nên có thể thay JSON bằng SQLite/HTTP mà không sửa Service (OCP).
/// </summary>
public interface ITaskRepository
{
    /// <summary>Đọc toàn bộ công việc; trả về danh sách rỗng nếu chưa có dữ liệu.</summary>
    Task<IReadOnlyList<TaskItem>> LoadAsync(CancellationToken ct = default);

    /// <summary>Ghi đè toàn bộ công việc ra kho.</summary>
    Task SaveAsync(IReadOnlyList<TaskItem> items, CancellationToken ct = default);
}
