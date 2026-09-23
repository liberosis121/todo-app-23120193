# TODO Desk

## Ứng dụng quản lý công việc trên Console bằng C#

| Thông tin | Nội dung |
|---|---|
| **Môn học** | Lập trình Windows |
| **Mã lớp** | 24/31 |
| **Sinh viên** | Trần Kim Yến |
| **Mã số sinh viên** | 23120193 |
| **Ngôn ngữ** | C# 14, .NET 10 |
| **Loại ứng dụng** | Console Application — Terminal User Interface |
| **Thư viện giao diện** | Spectre.Console 0.57.2 |

---

## Mục lục

1. [Giới thiệu](#1-giới-thiệu)
2. [Chức năng](#2-chức-năng)
3. [Giao diện](#3-giao-diện)
4. [Cài đặt và chạy chương trình](#4-cài-đặt-và-chạy-chương-trình)
5. [Cấu trúc và kiến trúc mã nguồn](#5-cấu-trúc-và-kiến-trúc-mã-nguồn)
6. [Luồng hoạt động](#6-luồng-hoạt-động)
7. [Tổng kết các kỹ thuật C# đã học](#7-tổng-kết-các-kỹ-thuật-c-đã-học)
8. [Ba điểm cải tiến mã nguồn](#8-ba-điểm-cải-tiến-mã-nguồn)
9. [Kết luận](#9-kết-luận)

---

## 1. Giới thiệu

**TODO Desk** là chương trình quản lý ghi chú công việc chạy trên console. Ứng dụng cho phép người dùng tạo, theo dõi, tìm kiếm, chỉnh sửa và hoàn thành các công việc hằng ngày. Dữ liệu được lưu tự động dưới dạng JSON và được nạp lại trong lần chạy tiếp theo.

Thay cho menu console chỉ gồm văn bản và số thứ tự, ứng dụng sử dụng **Terminal User Interface (TUI)** để tạo trải nghiệm trực quan hơn nhưng vẫn đáp ứng đúng yêu cầu chương trình console:

- điều khiển bằng phím mũi tên và phím Enter;
- dashboard thống kê tình trạng công việc;
- biểu đồ tiến độ;
- bảng dữ liệu có màu sắc và căn chỉnh;
- logo Figlet và hình minh họa ASCII;
- giao diện nền sáng theo phong cách retro terminal;
- màn hình riêng cho từng chức năng.

### Mục tiêu của chương trình

- vận dụng kiến thức C# vào một bài toán quản lý dữ liệu thực tế;
- xây dựng chương trình console có cấu trúc thay vì đặt toàn bộ mã nguồn trong `Program.cs`;
- phân tách giao diện, nghiệp vụ, mô hình và lưu trữ;
- xử lý dữ liệu bất đồng bộ và lỗi nhập liệu;
- lưu dữ liệu bền vững giữa các phiên làm việc;
- tạo giao diện console dễ sử dụng và có tính thẩm mỹ.

---

## 2. Chức năng

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Thêm công việc | Nhập tiêu đề, mô tả, mức ưu tiên và hạn hoàn thành |
| 2 | Xem danh sách | Hiển thị các công việc dưới dạng bảng |
| 3 | Lọc công việc | Lọc tất cả, chưa hoàn thành, đã hoàn thành hoặc quá hạn |
| 4 | Tìm kiếm | Tìm trong tiêu đề và mô tả, không phân biệt chữ hoa và chữ thường |
| 5 | Đổi trạng thái | Chuyển công việc giữa trạng thái chưa xong và đã xong |
| 6 | Chỉnh sửa | Cập nhật nội dung, mô tả, mức ưu tiên và hạn hoàn thành |
| 7 | Xóa công việc | Chọn công việc và xác nhận trước khi xóa |
| 8 | Theo dõi tiến độ | Hiển thị tổng số, số chưa xong, số hoàn thành, số quá hạn và phần trăm tiến độ |
| 9 | Tự động lưu | Ghi dữ liệu sau mỗi thao tác thay đổi |
| 10 | Nạp dữ liệu | Đọc lại file JSON khi chương trình khởi động |
| 11 | Thoát an toàn | Xử lý Ctrl+C và cố gắng lưu dữ liệu trước khi kết thúc |

### Quy tắc trạng thái

- Công việc mới mặc định ở trạng thái **chưa hoàn thành**.
- Công việc được xem là quá hạn khi ngày hết hạn nhỏ hơn ngày hiện tại và công việc chưa hoàn thành.
- Công việc đã hoàn thành không còn được tính là quá hạn.
- Tiêu đề không được để trống.
- Hạn hoàn thành có thể bỏ trống.
- Mức ưu tiên gồm **Cao**, **Trung bình** và **Thấp**.

---

## 3. Giao diện

### Dashboard và menu chính

![Dashboard TODO Desk](docs/demo-01-start.png)

Dashboard gồm:

- logo `TODO` được tạo bằng `FigletText`;
- hình ASCII Task-Bot;
- bốn thẻ thống kê;
- biểu đồ tiến độ;
- danh sách công việc cần chú ý;
- menu điều khiển bằng phím `↑`, `↓` và `Enter`.

### Danh sách công việc

![Danh sách công việc](docs/demo-02-table.png)

Quy ước màu sắc:

| Màu | Ý nghĩa |
|---|---|
| Teal | Thành phần điều hướng, tiêu đề và công việc ưu tiên thấp |
| Xanh lá | Công việc đã hoàn thành |
| Vàng gold | Công việc đang thực hiện và ưu tiên trung bình |
| Đỏ coral | Công việc quá hạn hoặc ưu tiên cao |
| Xám đậm | Nội dung phụ và trạng thái chưa hoàn thành |

Giao diện sử dụng nền ngà sáng. `LightThemeTextWriter` bảo đảm màu nền không bị mã ANSI reset về màu đen trên Windows Console Host cũ.

---

## 4. Cài đặt và chạy chương trình

### 4.1. Yêu cầu môi trường

- Windows 10 hoặc Windows 11;
- .NET SDK 10.0 trở lên;
- Visual Studio hoặc một terminal hỗ trợ UTF-8;
- kết nối Internet trong lần đầu khôi phục NuGet package.

Kiểm tra .NET SDK:

```powershell
dotnet --version
```

### 4.2. Chạy bằng Visual Studio

1. Giải nén mã nguồn.
2. Mở file `TodoApp.sln` bằng Visual Studio.
3. Trong Solution Explorer, đặt project `TodoApp` làm Startup Project.
4. Chờ Visual Studio hoàn tất khôi phục NuGet package.
5. Nhấn `Ctrl+F5` để chạy không debug hoặc `F5` để chạy với debugger.

### 4.3. Chạy bằng dòng lệnh

Mở PowerShell tại thư mục chứa `TodoApp.sln`, sau đó chạy:

```powershell
dotnet restore TodoApp.sln
dotnet build TodoApp.sln
dotnet run --project src/TodoApp/TodoApp.csproj
```

Sau khi đã build, có thể chạy nhanh bằng:

```powershell
dotnet run --project src/TodoApp --no-build
```

### 4.4. Cách sử dụng

| Thao tác | Phím |
|---|---|
| Di chuyển trong menu | `↑` / `↓` |
| Xác nhận lựa chọn | `Enter` |
| Bỏ qua trường tùy chọn | `Enter` mà không nhập nội dung |
| Giữ giá trị cũ khi chỉnh sửa | `Enter` |
| Dừng chương trình | `Ctrl+C` |

### 4.5. File dữ liệu

Ở chế độ Debug, file dữ liệu nằm tại:

```text
src/TodoApp/bin/Debug/net10.0/todo.data.json
```

File được tạo tự động. Để đưa ứng dụng về trạng thái chưa có dữ liệu, đóng chương trình và xóa file `todo.data.json`.

---

## 5. Cấu trúc và kiến trúc mã nguồn

### 5.1. Cấu trúc thư mục

```text
TODO App/
├── TodoApp.sln
├── readme.md
├── docs/
│   ├── demo-01-start.png
│   └── demo-02-table.png
├── src/TodoApp/
│   ├── Program.cs
│   ├── TodoApp.csproj
│   ├── Models/
│   │   └── TaskItem.cs
│   ├── Data/
│   │   ├── ITaskRepository.cs
│   │   └── JsonTaskRepository.cs
│   ├── Services/
│   │   ├── ITaskService.cs
│   │   └── TaskService.cs
│   ├── UI/
│   │   ├── ConsoleUi.cs
│   │   ├── LightThemeTextWriter.cs
│   │   └── MainMenu.cs
│   └── Utils/
│       ├── ConsoleInput.cs
│       └── InputValidator.cs
```

### 5.2. Các tầng chính

```text
┌─────────────────────────────────────────────────────┐
│ UI                                                  │
│ MainMenu, ConsoleUi, LightThemeTextWriter           │
│ Điều hướng, nhập liệu và hiển thị TUI               │
├─────────────────────────────────────────────────────┤
│ Service                                             │
│ ITaskService, TaskService                           │
│ Quy tắc thêm, sửa, xóa, tìm kiếm và lọc             │
├─────────────────────────────────────────────────────┤
│ Data                                                │
│ ITaskRepository, JsonTaskRepository                 │
│ Đọc và ghi dữ liệu JSON                             │
├─────────────────────────────────────────────────────┤
│ Model                                               │
│ TaskItem, TaskPriority, TaskFilter                  │
│ Biểu diễn dữ liệu và trạng thái                     │
├─────────────────────────────────────────────────────┤
│ Utility                                             │
│ InputValidator, ConsoleInput, StringExtensions      │
│ Chuẩn hóa và kiểm tra dữ liệu nhập                  │
└─────────────────────────────────────────────────────┘
```

### 5.3. Trách nhiệm của từng thành phần

| Thành phần | Trách nhiệm |
|---|---|
| `Program.cs` | Khởi tạo dependency, cấu hình console và quản lý vòng đời ứng dụng |
| `TaskItem` | Biểu diễn một công việc và kiểm tra quá hạn |
| `ITaskService` | Khai báo hợp đồng nghiệp vụ |
| `TaskService` | Thực hiện các thao tác quản lý công việc |
| `ITaskRepository` | Khai báo hợp đồng lưu trữ |
| `JsonTaskRepository` | Serialize, deserialize và ghi file an toàn |
| `MainMenu` | Điều phối thao tác giữa người dùng và service |
| `ConsoleUi` | Vẽ dashboard, bảng, form, menu và thông báo |
| `LightThemeTextWriter` | Duy trì light theme sau các mã ANSI reset |
| `InputValidator` | Kiểm tra tiêu đề, số và ngày |
| `ConsoleInput` | Phát hiện EOF khi luồng nhập kết thúc |

Kiến trúc này giúp mã nguồn không phụ thuộc chặt vào một giao diện hoặc một phương thức lưu trữ cụ thể. `TaskService` không sử dụng `Console` và cũng không biết dữ liệu được lưu bằng JSON.

---

## 6. Luồng hoạt động

### 6.1. Khởi động

```text
Program.cs
   │
   ├── cấu hình UTF-8 và light theme
   ├── tạo JsonTaskRepository
   ├── tạo TaskService
   ├── đọc todo.data.json
   └── chạy MainMenu
```

### 6.2. Thực hiện một thao tác thay đổi dữ liệu

```text
Người dùng chọn chức năng
        ↓
MainMenu nhận dữ liệu
        ↓
ITaskService / TaskService xử lý nghiệp vụ
        ↓
ITaskRepository / JsonTaskRepository lưu JSON
        ↓
Dashboard được cập nhật
```

### 6.3. Thoát chương trình

Khi người dùng chọn thoát hoặc nhấn Ctrl+C:

1. chương trình phát tín hiệu cancellation nếu cần;
2. khối `finally` cố gắng lưu dữ liệu;
3. light theme được trả về cấu hình terminal ban đầu;
4. tiến trình kết thúc an toàn.

---

## 7. Tổng kết các kỹ thuật C# đã học

Đây là các kỹ thuật được sử dụng trực tiếp trong mã nguồn của dự án.

### 7.1. Class, object và mô hình hóa dữ liệu

`TaskItem` là lớp đại diện cho một công việc:

```csharp
public sealed class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsDone { get; set; }
}
```

Qua lớp này có thể nhận thấy:

- class dùng để đóng gói dữ liệu liên quan;
- property mô tả trạng thái của object;
- giá trị mặc định giúp object có trạng thái khởi tạo hợp lệ;
- `sealed` thể hiện lớp không được thiết kế để kế thừa;
- hành vi liên quan trực tiếp đến công việc, như `IsOverdue()`, được đặt cùng model.

### 7.2. Enum để biểu diễn tập giá trị hữu hạn

```csharp
public enum TaskPriority
{
    Low,
    Medium,
    High
}
```

`enum` phù hợp với mức ưu tiên vì số lựa chọn đã được xác định. So với string, enum tránh lỗi chính tả, hỗ trợ IntelliSense và bảo đảm an toàn kiểu.

`TaskFilter` cũng sử dụng enum cho các trạng thái lọc `All`, `Active`, `Done` và `Overdue`.

### 7.3. Kiểu dữ liệu Guid, DateOnly, DateTime và nullable

- `Guid` dùng làm mã định danh ổn định cho mỗi task.
- `DateOnly` biểu diễn hạn hoàn thành vì không cần giờ và múi giờ.
- `DateTime` lưu thời điểm tạo và cập nhật.
- `DateOnly?` biểu diễn trường hợp công việc không có hạn.
- `TaskItem?` biểu diễn kết quả có thể không tìm thấy.

Việc chọn đúng kiểu dữ liệu làm cho mã nguồn tự mô tả và giảm xử lý chuyển đổi string thủ công.

### 7.4. Interface và abstraction

```csharp
public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IReadOnlyList<TaskItem> items, CancellationToken ct = default);
}
```

Interface định nghĩa khả năng của repository nhưng không quy định dữ liệu phải lưu bằng JSON, SQLite hay dịch vụ web. `TaskService` chỉ phụ thuộc vào `ITaskRepository`.

Kỹ thuật này giúp:

- giảm phụ thuộc giữa các lớp;
- thay đổi phương thức lưu trữ dễ hơn;
- phân tách hợp đồng và implementation;
- áp dụng Dependency Inversion Principle trong SOLID.

### 7.5. Constructor Injection

```csharp
public TaskService(ITaskRepository repository)
{
    _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
}
```

Repository được truyền từ bên ngoài thay vì được tạo trực tiếp trong `TaskService`. Đây là constructor injection.

`Program.cs` đóng vai trò Composition Root:

```csharp
ITaskRepository repository = new JsonTaskRepository();
ITaskService service = new TaskService(repository);
var menu = new MainMenu(service);
```

Nhờ đó dependency graph tập trung tại một vị trí và các lớp nghiệp vụ không cần biết implementation cụ thể.

### 7.6. Separation of Concerns và Single Responsibility

Mỗi nhóm lớp có một trách nhiệm chính:

- Model quản lý dữ liệu;
- Service quản lý nghiệp vụ;
- Repository quản lý lưu trữ;
- UI quản lý hiển thị và tương tác;
- Utility quản lý kiểm tra input.

Nếu thay giao diện console bằng WinForms, phần nghiệp vụ có thể được tái sử dụng. Nếu thay JSON bằng SQLite, phần UI không cần thay đổi.

### 7.7. Generic collection và IReadOnlyList

```csharp
private readonly List<TaskItem> _items = new();
public IReadOnlyList<TaskItem> Items => _items;
```

`List<TaskItem>` lưu collection có kiểu rõ ràng. Service chỉ trả `IReadOnlyList<TaskItem>` để UI được phép đọc nhưng không thể tự ý thêm hoặc xóa dữ liệu.

Mọi thao tác thay đổi phải đi qua service, nhờ đó quy tắc nghiệp vụ và thao tác lưu file không bị bỏ qua.

### 7.8. LINQ

Tìm kiếm được xây dựng bằng chuỗi toán tử LINQ:

```csharp
return _items
    .Where(t => t.Title.ToLowerInvariant().Contains(key)
             || t.Description.ToLowerInvariant().Contains(key))
    .OrderBy(t => t.IsDone)
    .ThenByDescending(t => t.Priority)
    .ToList();
```

Các toán tử được sử dụng gồm:

- `Where` để lọc;
- `OrderBy` và `ThenByDescending` để sắp xếp nhiều cấp;
- `FirstOrDefault` để tìm một phần tử;
- `Count` để thống kê;
- `Take` để lấy một số task cần chú ý;
- `Select` để biến đổi collection.

LINQ giúp biểu diễn truy vấn rõ ràng hơn so với nhiều vòng lặp lồng nhau.

### 7.9. Switch expression và pattern matching

```csharp
public IReadOnlyList<TaskItem> Filter(TaskFilter filter) => filter switch
{
    TaskFilter.Active => _items.Where(t => !t.IsDone).ToList(),
    TaskFilter.Done => _items.Where(t => t.IsDone).ToList(),
    TaskFilter.Overdue => _items.Where(t => t.IsOverdue()).ToList(),
    _ => _items.ToList()
};
```

Switch expression giúp mã nguồn ngắn gọn và yêu cầu các nhánh trả về cùng kiểu dữ liệu.

Pattern matching được sử dụng khi kiểm tra câu trả lời:

```csharp
return answer is "y" or "yes";
```

### 7.10. Async và await

Các thao tác file được thực hiện bất đồng bộ:

```csharp
await using var stream = File.OpenRead(_path);
var items = await JsonSerializer.DeserializeAsync<List<TaskItem>>(
    stream, Options, ct);
```

Service chờ lưu hoàn tất trước khi trả kết quả:

```csharp
_items.Add(item);
await PersistAsync();
return item;
```

Các kiến thức rút ra:

- `Task` đại diện cho thao tác bất đồng bộ;
- `Task<T>` trả về một kết quả trong tương lai;
- `await` giúp code bất đồng bộ vẫn có cấu trúc tuần tự dễ đọc;
- không nên dùng `.Wait()` hoặc `.Result` trong call chain bất đồng bộ;
- thao tác lưu quan trọng không nên chạy theo kiểu fire-and-forget.

### 7.11. CancellationToken và event

```csharp
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};
```

`Console.CancelKeyPress` là event được phát khi người dùng nhấn Ctrl+C. Chương trình hủy theo cơ chế hợp tác thay vì bị kết thúc đột ngột.

`CancellationToken` được truyền vào quá trình đọc và ghi dữ liệu để thao tác I/O có thể dừng tại điểm an toàn.

### 7.12. Exception handling và exception filter

```csharp
catch (Exception ex) when (
    ex is JsonException or IOException or UnauthorizedAccessException)
{
    return Array.Empty<TaskItem>();
}
```

Exception filter giúp chỉ bắt những lỗi mà repository biết cách xử lý. Lỗi JSON, lỗi I/O và lỗi quyền truy cập được xử lý thay vì làm chương trình dừng với stack trace.

`finally` được dùng để thực hiện thao tác lưu và khôi phục màu terminal dù chương trình kết thúc bình thường hay có lỗi.

### 7.13. System.Text.Json

```csharp
private static readonly JsonSerializerOptions Options = new()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

Các tùy chọn có ý nghĩa:

- `WriteIndented`: JSON dễ đọc;
- `camelCase`: phù hợp quy ước JSON;
- `JsonStringEnumConverter`: lưu `High` thay vì số nguyên;
- `WhenWritingNull`: bỏ qua trường không có giá trị;
- `[JsonIgnore]`: không lưu property chỉ phục vụ hiển thị.

### 7.14. Ghi file atomic

```csharp
var temp = _path + ".tmp";
await using (var stream = File.Create(temp))
{
    await JsonSerializer.SerializeAsync(stream, items, Options, ct);
}
File.Move(temp, _path, overwrite: true);
```

Dữ liệu được ghi vào file tạm trước. Chỉ khi serialize thành công, file tạm mới thay thế file chính. Cách này giảm nguy cơ làm hỏng file dữ liệu nếu chương trình bị dừng giữa lúc ghi.

### 7.15. Nullable Reference Types

Project bật nullable reference type:

```xml
<Nullable>enable</Nullable>
```

Compiler có thể cảnh báo sớm những vị trí có nguy cơ truy cập null. Các trường tùy chọn được khai báo rõ bằng dấu `?`.

### 7.16. Extension method

```csharp
public static string OneLine(this string? text)
    => (text ?? string.Empty).Trim();
```

Extension method cho phép gọi `value.OneLine()` như method của string. Logic xử lý null và khoảng trắng chỉ cần viết một lần và được sử dụng nhất quán.

### 7.17. Top-level statements và quản lý tài nguyên

`Program.cs` sử dụng top-level statements nên không cần khai báo lớp `Program` và method `Main` thủ công.

`using var` và `await using` bảo đảm các tài nguyên như stream và `CancellationTokenSource` được giải phóng đúng lúc.

### 7.18. Terminal User Interface với Spectre.Console

Các thành phần Spectre.Console được sử dụng:

- `Panel` cho header, card và form;
- `Table` cho danh sách công việc;
- `SelectionPrompt<T>` cho menu tương tác;
- `BreakdownChart` cho biểu đồ tiến độ;
- `FigletText` cho logo;
- `Markup` và `Style` cho màu sắc;
- `AnsiConsole.Clear()` để làm mới màn hình;
- `Markup.Escape()` để dữ liệu người dùng không phá cú pháp hiển thị.

`SelectionPrompt.PageSize()` yêu cầu giá trị tối thiểu là 3. Vì vậy page size khi chọn task được giới hạn an toàn:

```csharp
.PageSize(Math.Clamp(items.Count, 3, 10))
```

Điều này bảo đảm menu hoạt động cả khi chỉ có một hoặc hai công việc.

### 7.19. Decorator pattern cho light theme

Spectre.Console sử dụng mã ANSI reset. Trên Console Host cũ, mã reset có thể đưa nền về màu đen. `LightThemeTextWriter` bọc `stdout`, nhận diện các chuỗi SGR và khôi phục màu mặc định của light theme.

```csharp
Console.SetOut(new LightThemeTextWriter(Console.Out));
```

Writer chỉ khôi phục kênh foreground hoặc background đã bị reset, không ghi đè các màu component vừa thiết lập. Đây là một ứng dụng thực tế của **Decorator pattern**.

### 7.20. Defensive programming và kiểm tra input

Chương trình không giả định dữ liệu người dùng luôn hợp lệ:

- tiêu đề rỗng được yêu cầu nhập lại;
- số ngoài phạm vi bị từ chối;
- ngày sai định dạng được thông báo;
- thao tác sửa, xóa hoặc toggle trên danh sách rỗng được chặn;
- xóa yêu cầu xác nhận;
- EOF được phát hiện để tránh vòng lặp vô hạn;
- nội dung người dùng được escape trước khi render markup.

---

## 8. Ba điểm cải tiến mã nguồn

### 8.1. Sử dụng Generic Host và Dependency Injection container

#### Hiện trạng

Các dependency đang được khởi tạo thủ công trong `Program.cs`. Cách này rõ ràng với quy mô nhỏ, nhưng sẽ khó quản lý khi ứng dụng có thêm nhiều repository, service, cấu hình và logger.

#### Đề xuất

Sử dụng `Microsoft.Extensions.Hosting` và `Microsoft.Extensions.DependencyInjection`:

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ITaskRepository, JsonTaskRepository>();
builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<MainMenu>();

using var host = builder.Build();
await host.Services.GetRequiredService<MainMenu>().RunAsync();
```

Đồng thời, đưa đường dẫn file dữ liệu và cấu hình giao diện vào `appsettings.json`, sử dụng `IOptions<T>` và `ILogger<T>`.

#### Lợi ích

- dependency graph được quản lý tập trung;
- vòng đời object được quản lý tự động;
- dễ bổ sung implementation mới;
- cấu hình được tách khỏi mã nguồn;
- logging có level và category;
- thuận lợi mở rộng thành ứng dụng lớn hơn.

#### Đánh đổi

- thêm package và khái niệm framework;
- tăng độ phức tạp ban đầu đối với project nhỏ.

### 8.2. Thay JSON bằng SQLite và bổ sung kiểm soát đồng thời

#### Hiện trạng

Mỗi thao tác thay đổi hiện serialize lại toàn bộ danh sách. Phương pháp này phù hợp dữ liệu nhỏ nhưng không tối ưu khi số lượng task tăng. Hai tiến trình cùng mở file cũng có thể ghi đè dữ liệu của nhau.

#### Đề xuất

Tạo `SqliteTaskRepository` triển khai `ITaskRepository`, lưu mỗi task thành một row:

```sql
CREATE TABLE Tasks (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Description TEXT NOT NULL,
    Priority INTEGER NOT NULL,
    DueDate TEXT NULL,
    IsDone INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    Version INTEGER NOT NULL DEFAULT 1
);
```

Bổ sung:

- transaction cho nhóm thao tác;
- index cho `IsDone`, `Priority` và `DueDate`;
- cột `Version` để optimistic concurrency;
- migration khi schema thay đổi;
- JSON được giữ lại cho chức năng import/export.

#### Lợi ích

- chỉ cập nhật row thay đổi;
- hiệu quả hơn với dữ liệu lớn;
- truy vấn và lọc trực tiếp tại database;
- transaction bảo đảm tính nhất quán;
- phát hiện xung đột khi nhiều tiến trình sửa cùng dữ liệu.

#### Đánh đổi

- cần quản lý schema và migration;
- quá trình triển khai phức tạp hơn file JSON;
- cần xử lý thêm lỗi kết nối và transaction.

### 8.3. Chuẩn hóa validation, error model và cancellation end-to-end

#### Hiện trạng

Validation hiện tập trung nhiều ở UI. Các method thay đổi của service chưa nhận `CancellationToken` đầy đủ. Lỗi dự kiến như không tìm thấy task và lỗi lưu trữ chưa có một kiểu kết quả thống nhất.

#### Đề xuất

Tạo request model và Result pattern:

```csharp
public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate);

public sealed record Error(string Code, string Message);

public sealed record Result<T>(
    bool IsSuccess,
    T? Value,
    Error? Error);
```

Chữ ký service có thể được đổi thành:

```csharp
Task<Result<TaskItem>> AddAsync(
    CreateTaskRequest request,
    CancellationToken ct = default);
```

Các mã lỗi có thể gồm:

- `VALIDATION_ERROR`;
- `TASK_NOT_FOUND`;
- `STORAGE_UNAVAILABLE`;
- `CONCURRENCY_CONFLICT`.

Cancellation token cần được truyền xuyên suốt:

```text
MainMenu → TaskService → Repository → File/Database API
```

#### Lợi ích

- quy tắc hợp lệ không phụ thuộc giao diện;
- lỗi nghiệp vụ và lỗi kỹ thuật được phân biệt rõ;
- UI nhận được thông báo có cấu trúc;
- thao tác có thể dừng nhanh khi người dùng hủy;
- chữ ký method mô tả rõ các khả năng thành công và thất bại;
- dễ bảo trì khi bổ sung giao diện khác.

#### Đánh đổi

- số lượng request/result type tăng;
- chữ ký method dài hơn;
- cần quy ước mã lỗi thống nhất trong toàn bộ ứng dụng.

---

## 9. Kết luận

TODO Desk đáp ứng yêu cầu xây dựng ứng dụng ghi chú TODO trên console, đồng thời mở rộng bài toán với tìm kiếm, lọc, mức ưu tiên, hạn hoàn thành, thống kê tiến độ và lưu dữ liệu JSON.

Qua mã nguồn của dự án, các kiến thức C# được vận dụng từ mức cơ bản đến tổ chức phần mềm: class, enum, collection, LINQ, async/await, serialization, exception handling, nullable, interface, dependency injection, kiến trúc phân tầng và xây dựng TUI.

Ba hướng cải tiến được đề xuất tập trung vào khả năng mở rộng và độ tin cậy: chuẩn hóa dependency bằng Generic Host, nâng cấp lưu trữ sang SQLite và xây dựng error model/cancellation xuyên suốt. Đây là nền tảng để ứng dụng có thể tiếp tục phát triển mà không phải thay đổi toàn bộ cấu trúc hiện tại.

---

**Trần Kim Yến — MSSV 23120193 — Môn Lập trình Windows**
