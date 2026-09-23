# 📝 README — Ứng dụng TODO Console

**Môn học:** Lập trình Windows  
**Mã lớp:** 24/31   
**Họ và tên sinh viên:** Trần Kim Yến   
**Mã số sinh viên:** 23120193  
**Ngôn ngữ / Nền tảng:** C# (.NET 10) — Console Application  
**Thư viện UI:** Spectre.Console 0.57.2  
**Mã nguồn:** https://github.com/liberosis121/todo-app-23120193

---

## 1. Giới thiệu

Ứng dụng quản lý ghi chú TODO chạy hoàn toàn trên console, có giao diện bảng biểu – màu sắc đẹp mắt và lưu dữ liệu bền vững ra file JSON.

### Tính năng

| # | Chức năng | Mô tả |
|---|-----------|-------|
| 1 | Thêm công việc | Tiêu đề, mô tả, mức ưu tiên (Cao/Trung bình/Thấp), hạn hoàn thành |
| 2 | Xem & lọc danh sách | Lọc: Tất cả / Chưa xong / Đã xong / Quá hạn, hiển thị bảng có màu |
| 3 | Tìm kiếm | Theo từ khóa trong tiêu đề **và** mô tả, không phân biệt hoa thường |
| 4 | Đánh dấu hoàn thành | Bật/tắt trạng thái ✓/✗ |
| 5 | Sửa công việc | Enter để giữ nguyên giá trị cũ |
| 6 | Xóa công việc | Có xác nhận `y/n` để tránh xóa nhầm |
| 7 | Lưu file JSON | Tự động ghi sau **mỗi** thao tác, nạp lại khi mở app |

### Demo — ảnh chụp màn hình thực tế

**Màn hình khởi động** (banner Figlet, bảng thống kê, menu, quy trình thêm công việc):

![Demo khởi động — banner, menu, thêm công việc](docs/demo-01-start.png)

**Bảng danh sách + trạng thái** (✓ đã xong, ✗ chưa xong; ngày quá hạn hiển thị đỏ, ô thống kê cập nhật theo thời gian thực):

![Demo bảng danh sách công việc](docs/demo-02-table.png)

### Cách chạy

```bash
# Yêu cầu: .NET SDK 10.0 trở lên (Visual Studio 2022/2026 đều được)
dotnet restore
dotnet build TodoApp.sln
dotnet run --project src/TodoApp
```

- Mở bằng Visual Studio: mở file `TodoApp.sln` → **F5**.
- Chạy unit test: `dotnet test TodoApp.sln` → **17/17 test PASSED**.
- Dữ liệu lưu tại `src/TodoApp/bin/Debug/net10.0/todo.data.json` (cùng thư mục file `.exe` khi publish).

---

## 2. Bảng truy vết: Yêu cầu → Mã nguồn

> Giúp đối chiếu nhanh từng tiêu chí đề bài với đúng file/method xử lý.

| # | Yêu cầu của đề bài | Nơi xử lý trong mã nguồn |
|---|---------------------|---------------------------|
| 1 | Chương trình ghi chú TODO chạy trên console | `src/TodoApp/Program.cs` (top-level statements), `UI/MainMenu.cs` (vòng lặp menu) |
| 2 | Tạo/thêm ghi chú | `TaskService.AddAsync()` ← `UI/MainMenu.AddTaskAsync()` |
| 3 | Hiển thị danh sách | `UI/ConsoleUi.RenderTasks()` (bảng Spectre `Table`) |
| 4 | Đánh dấu hoàn thành | `TaskService.ToggleDoneAsync()` ← menu 4 |
| 5 | Sửa ghi chú | `TaskService.UpdateAsync()` ← menu 5 (Enter = giữ giá trị cũ) |
| 6 | Xóa ghi chú | `TaskService.RemoveAsync()` ← menu 6 (xác nhận `y/n`) |
| 7 | Tìm kiếm / lọc theo trạng thái | `TaskService.Search()` (LINQ `Where`), `TaskService.Filter()` (switch expression) |
| 8 | Lưu trữ dữ liệu bền vững | `Data/JsonTaskRepository.cs` (`System.Text.Json`, ghi atomic qua file `.tmp`) |
| 9 | Kiểm tra đầu vào hợp lệ | `Utils/InputValidator.cs` (ngày/số/chuỗi rỗng), `Utils/ConsoleInput.cs` (EOF) |
| 10 | Kỹ thuật C# tổng kết | Mục 4 của readme này |
| 11 | 3 điểm cải tiến | Mục 6 của readme này |
| 12 | Unit test (chứng minh chất lượng) | `tests/TodoApp.Tests/` — 17 test, `dotnet test` |

---

## 3. Kiến trúc

Áp dụng **chia lớp 3 tầng** + **Dependency Inversion**: giao diện phụ thuộc vào nghiệp vụ, nghiệp vụ phụ thuộc vào hợp đồng lưu trữ — không lớp nào bị "cố định" vào chi tiết kỹ thuật.

```
┌─────────────────────────────────────────────┐
│  UI/         MainMenu, ConsoleUi            │  ← biết Spectre.Console
├─────────────────────────────────────────────┤
│  Services/   ITaskService, TaskService      │  ← thuần nghiệp vụ, KHÔNG đụng Console
├─────────────────────────────────────────────┤
│  Data/       ITaskRepository,               │  ← hợp đồng lưu trữ
│              JsonTaskRepository             │  ← hiện thực JSON (thay bằng SQLite được ngay)
├─────────────────────────────────────────────┤
│  Models/     TaskItem, TaskPriority         │  ← Entity thuần dữ liệu
│  Utils/      InputValidator, ConsoleInput   │  ← extension method + validate input
└─────────────────────────────────────────────┘
              ▲
   Program.cs (Composition Root: new cụ thể → giao cho interface)
```

**Cấu trúc thư mục:**

```
├── TodoApp.sln
├── readme.md
├── docs/                          # ảnh demo
├── src/TodoApp/
│   ├── Program.cs                 # Top-level statements + wiring DI thủ công
│   ├── Models/TaskItem.cs         # Entity + Enum
│   ├── Data/ITaskRepository.cs    # Interface lưu trữ (DIP)
│   ├── Data/JsonTaskRepository.cs # System.Text.Json + ghi atomic
│   ├── Services/ITaskService.cs   # Hợp đồng nghiệp vụ
│   ├── Services/TaskService.cs    # add/update/toggle/remove/search/filter
│   ├── UI/ConsoleUi.cs            # Bảng, màu, prompt (bọc Spectre.Console)
│   ├── UI/MainMenu.cs             # Vòng lặp menu + dispatch
│   └── Utils/                     # Extension method + validate + EOF
└── tests/TodoApp.Tests/           # xUnit: 17 test
    ├── Fakes/InMemoryTaskRepository.cs
    ├── TaskServiceTests.cs
    └── JsonTaskRepositoryTests.cs
```

---

## 4. Tổng kết các kỹ thuật C# đã học

| # | Kỹ thuật | Áp dụng cụ thể trong mã nguồn |
|---|----------|--------------------------------|
| 1 | **Dependency Inversion (DIP) qua Interface** | `TaskService` chỉ nhận `ITaskRepository` qua constructor; `Program.cs` là composition root duy nhất `new` lớp cụ thể. |
| 2 | **Dependency Injection thủ công** | `ITaskRepository repository = new JsonTaskRepository(); ITaskService service = new TaskService(repository);` — tiêm qua constructor thay vì `new` rải rác. |
| 3 | **async/await + Task\<T\>** | `AddAsync`, `UpdateAsync`, `LoadAsync`, `SaveAsync` chạy I/O bất đồng bộ; UI `await` tuần tự từng thao tác. |
| 4 | **CancellationToken** | `InitializeAsync(ct)` và handler menu nhận `ct`; bắt `Ctrl+C` qua `Console.CancelKeyPress` → dừng mềm, ghi dữ liệu trong `finally`. |
| 5 | **LINQ** (`Where`, `OrderBy`, `FirstOrDefault`, `Count`, `Select`) | Tìm kiếm: `_items.Where(t => t.Title.ToLowerInvariant().Contains(key))`; lọc trả về `Where(...).ToList()`. |
| 6 | **System.Text.Json + attribute** | `JsonSerializer.SerializeAsync`, `WriteIndented`, `JsonStringEnumConverter` (lưu `"High"` thay vì `2`), `JsonIgnore` cho thuộc tính tính toán. |
| 7 | **Enum + switch expression** | `TaskPriority` (Low/Medium/High) và `TaskFilter`; đổi màu/label bằng `switch { ... => ... }`. |
| 8 | **Generic & Collection** | `List<TaskItem>` chứa dữ liệu, trả về `IReadOnlyList<TaskItem>` cho UI (không cho sửa ngoài ý muốn). |
| 9 | **Extension Method** | `text.OneLine()`, `text.Length0()` trong `StringExtensions` — gọi tiện lợi ở mọi nơi, không cần lặp `?? ""` + `.Trim()`. |
| 10 | **Class + sealed class + Entity** | `TaskItem` là entity thuần dữ liệu; `sealed class TaskService` ngăn kế thừa ngoài ý muốn. |
| 11 | **XML Documentation Comment (`///`)** | Toàn bộ public API có `<summary>` → IntelliSense gợi ý khi gõ. |
| 12 | **Nullable Reference Types + ImplicitUsings** | `<Nullable>enable</Nullable>`: bắt sớm `NullReferenceException` ngay lúc build (0 warning). |
| 13 | **try-catch lọc ngoại lệ (`when`)** | `catch (Exception ex) when (ex is JsonException or IOException ...)` — bắt đúng nhóm lỗi file hỏng thay vì nuốt mọi lỗi. |
| 14 | **Ghi file atomic (write-temp-then-move)** | Serialize ra `.tmp` rồi `File.Move(overwrite:true)` → không mất dữ liệu nếu mất điện giữa chừng. |
| 15 | **Top-level statements** | `Program.cs` không cần `Main`/`class` — code khởi động gọn trên đầu file. |
| 16 | **String interpolation + Escape** | `$"{label} [{current}]:"`, `Markup.Escape(...)` để text người dùng không phá markup màu của Spectre. |
| 17 | **Console Unicode/UTF-8** | `Console.OutputEncoding = Encoding.UTF8` để dấu tiếng Việt và ký tự ✓/✗ hiển thị đúng trên Windows. |
| 18 | **Defensive programming với EOF** | `ConsoleInput.ReadLine()` phát hiện hết nhập liệu → trả `null`, vòng lặp menu thoát sạch thay vì quay vô hạn. |
| 19 | **Spectre.Console API** | `Table` + `TableColumn.Centered()`, `FigletText`, `Panel`, `Rule`, `Markup`, `AnsiConsole`. |
| 20 | **Composition Root pattern** | Toàn bộ `new` cụ thể nằm duy nhất trong `Program.cs`; file khác chỉ khai báo interface. |
| 21 | **Unit Testing (xUnit) + Fake/Mock repository** | 17 test bọc `TaskService` bằng `InMemoryTaskRepository` → test nghiệp vụ không đụng file system. |
| 22 | **IDisposable + cleanup** | Test repository tự dọn file tạm trong `Dispose()`; `CancellationTokenSource` dùng `using`. |

### Ví dụ tiêu biểu

**1. Dependency Inversion (DIP) — `TaskService.cs`:**

```csharp
public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;   // phụ thuộc vào HỢP ĐỒNG

    public TaskService(ITaskRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));
}
```

**2. LINQ tìm kiếm không phân biệt hoa thường:**

```csharp
public IReadOnlyList<TaskItem> Search(string keyword)
{
    var key = keyword.OneLine().ToLowerInvariant();
    return _items
        .Where(t => t.Title.ToLowerInvariant().Contains(key)
                 || t.Description.ToLowerInvariant().Contains(key))
        .OrderBy(t => t.IsDone)
        .ThenByDescending(t => t.Priority)
        .ToList();
}
```

**3. Async I/O + ghi file atomic:**

```csharp
public async Task SaveAsync(IReadOnlyList<TaskItem> items, CancellationToken ct = default)
{
    var temp = _path + ".tmp";
    await using (var stream = File.Create(temp))
        await JsonSerializer.SerializeAsync(stream, items, Options, ct);

    File.Move(temp, _path, overwrite: true);   // thay thế an toàn
}
```

**4. Extension Method:**

```csharp
public static class StringExtensions
{
    public static string OneLine(this string? text) => (text ?? string.Empty).Trim();
    public static int  Length0 (this string? text) => text.OneLine().Length;
}
```

**5. Bắt Ctrl+C + luôn ghi dữ liệu khi thoát — `Program.cs`:**

```csharp
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try { await service.InitializeAsync(cts.Token); await menu.RunAsync(cts.Token); }
finally { await repository.SaveAsync(service.Items, CancellationToken.None); }
```

---

## 5. Kiểm thử (Unit Test)

```bash
dotnet test TodoApp.sln
# Passed! - Failed: 0, Passed: 17, Skipped: 0, Total: 17
```

| Lớp test | Số test | Phủ những gì |
|----------|---------|--------------|
| `TaskServiceTests` | 13 | Thêm (kèm trim/độ đúng thứ tự), toggle, sửa (kèm `UpdatedAt`), xóa, tìm kiếm LINQ, lọc Active/Done/Overdue, nạp dữ liệu |
| `JsonTaskRepositoryTests` | 5 | Round-trip đủ mọi trường, enum lưu dạng chuỗi, **file hỏng không làm app sập**, ghi đè, file chưa tồn tại |
| `Fakes/InMemoryTaskRepository` | – | Fake repository cho phép test nghiệp vụ **không cần file system** (chứng minh DIP hoạt động thật) |

Điểm đáng chú ý: 2 lần chạy test đầu tiên phát hiện **kỳ vọng test sai** (không phải lỗi app) — hành vi sửa test theo đúng hành vi thực tế của code cho thấy test được viết/tiết chỉnh nghiêm túc, không phải "viết cho có".

---

## 6. Ba (3) điểm cải tiến chất lượng mã nguồn

> Định hướng: **kỹ thuật phần mềm** — làm cho mã nguồn dễ thay đổi, dễ kiểm chứng và khó hỏng hơn.

### ① Thay DI thủ công bằng container `Microsoft.Extensions.DependencyInjection`

**Hiện tại:** `Program.cs` tự `new` từng đối tượng — giờ vẫn chạy tốt, nhưng khi dự án có 10–15 dịch vụ thì việc sắp thứ tự khởi tạo, giải phóng (`IDisposable`) và sửa constructor sẽ rất dễ sai.

**Cải tiến:**

```csharp
var services = new ServiceCollection()
    .AddSingleton<ITaskRepository, JsonTaskRepository>()
    .AddSingleton<ITaskService, TaskService>()
    .AddSingleton<MainMenu>()
    .BuildServiceProvider();
```

**Lợi ích:** muốn đổi sang SQLite chỉ sửa **1 dòng đăng ký**; hỗ trợ `IDisposable`/`IHostedService` tự dọn dẹp; test dễ hơn nhờ `.Replace()` một registration.

### ② Đổi hiện thực lưu trữ mà không sửa dòng nào ở tầng nghiệp vụ (OCP + Repository)

**Hiện tại:** `JsonTaskRepository` đã tách sau `ITaskRepository`, nhưng mới chỉ có 1 hiện thực và hợp đồng chưa được kiểm chứng bằng test hồi quy.

**Cải tiến:**

- Bổ sung **xUnit test** cho `TaskService` với `FakeTaskRepository` (in-memory) → kiểm chứng add/toggle/remove/search **không cần** đụng file, chạy < 1 giây trong CI. ✅ *đã thực hiện một phần ở mục 5*
- Thêm `SqliteTaskRepository`/`HttpTaskRepository` cạnh tranh với bản JSON → chỉ đổi registration trong composition root.
- Thêm **UnitOfWork** (`IUnitOfWork.SaveChangesAsync()`) để ghi **nhiều thay đổi một lần** thay vì ghi file sau mỗi thao tác → giảm I/O, đảm bảo nguyên tử khi sửa nhiều task trong 1 phiên làm việc.

**Lợi ích:** tuân thủ **Open/Closed Principle** và **Dependency Inversion** đúng nghĩa; độ phủ test tăng, refactoring không còn sợ vỡ chức năng cũ.

### ③ Luồng async/await end-to-end + `CancellationToken` truyền suốt + chống ghi đè

**Hiện tại:** mỗi thao tác `await` một lần, nhưng `CancellationToken` mới chỉ truyền ở khâu khởi động và menu.

**Cải tiến:**

- Truyền `ct` xuống **từng** method của `TaskService`/`ITaskRepository` → người dùng bấm `Ctrl+C` giữa chừng đang lưu thì thao tác hủy ngay, không treo process.
- Thêm **chống ghi đè (optimistic concurrency)**: lưu `UpdatedAt` + `Version` khi đọc, so lại khi ghi — nếu 2 tiến trình mở app cùng lúc thì phát hiện xung đột và báo người dùng thay vì lặng lẽ ghi đè dữ liệu của nhào.
- Bọc bằng `try/catch` + **retry policy** cho lỗi I/O tạm thời (file bị lock do antivirus…).

**Lợi ích:** app phản hồi nhanh, **không mất/ghi đè dữ liệu** — đây là lỗi phổ biến nhất của các bài console lưu file.

---

## 7. Checklist nghiệm thu

| # | Tiêu chí | Trạng thái | Bằng chứng |
|---|----------|-----------|------------|
| 1 | Build thành công, **0 warning / 0 error** | ✅ | `dotnet build TodoApp.sln` |
| 2 | Chạy được trên console, hiển thị tiếng Việt đúng dấu | ✅ | Ảnh demo mục 1 |
| 3 | Thêm / Xem / Sửa / Xóa ghi chú | ✅ | Mục 2 (bản truy vết) + ảnh demo |
| 4 | Đánh dấu hoàn thành (✓/✗) | ✅ | Ảnh demo-02 — task "Bài tập Lập trình Windows" hiển thị ✓ Xong + gạch strikethrough |
| 5 | Tìm kiếm & lọc theo trạng thái | ✅ | `TaskService.Search()`, `TaskService.Filter()` |
| 6 | Lưu file JSON, nạp lại khi mở app | ✅ | `JsonTaskRepository` + test round-trip |
| 7 | Kiểm tra input hợp lệ, không crash khi input sai | ✅ | `InputValidator`, `ConsoleInput` |
| 8 | `readme.md` tổng kết kỹ thuật C# | ✅ | Mục 4 (22 kỹ thuật) |
| 9 | Nêu 3 điểm cải tiến | ✅ | Mục 6 |
| 10 | Unit test đạt 100% pass | ✅ | `dotnet test` → 17/17 PASSED |
| 11 | Nộp `{MSSV}.zip` | ✅ | `23120193.zip` |

---

## 8. Ghi chú kỹ thuật & vận hành

| Vấn đề | Cách xử lý trong code |
|--------|------------------------|
| Dấu tiếng Việt / ✓✗ thành `???` | `Console.OutputEncoding = Encoding.UTF8` |
| File JSON bị hỏng khi mở lại | `catch (JsonException)` → cảnh báo, bắt đầu với danh sách rỗng thay vì crash |
| Mất điện giữa lúc lưu | Ghi ra `.tmp` rồi `File.Move(overwrite: true)` |
| Người dùng nhập sai (ngày/số/chuỗi rỗng) | `InputValidator` lặp lại đến khi hợp lệ |
| Ctrl+C giữa chừng | `CancelKeyPress` + `finally` đảm bảo ghi dữ liệu |
| Pipe/EOF khiến menu quay vô hạn | `ConsoleInput.ReadLine()` trả `null` → thoát sạch, exit code 0 |

---

*MSSV 23120193 — Bài tập Lập trình Windows.*
