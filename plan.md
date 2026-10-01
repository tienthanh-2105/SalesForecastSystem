# Kế hoạch chuyển trạng thái sang enum

Ngày lập kế hoạch: 01/10/2026.

## Mục tiêu

Chuyển các trường trạng thái đang dùng `string` trong C# sang enum để thống nhất giá trị, tránh lỗi gõ chuỗi và kiểm tra kiểu dữ liệu khi biên dịch. Giữ dữ liệu SQL Server và JSON API ở dạng tên trạng thái để tương thích với dữ liệu, stored procedure, trigger và frontend hiện tại.

## Phạm vi

| Thành phần | Enum dự kiến | Giá trị |
| --- | --- | --- |
| Đơn bán hàng | `SalesOrderStatus` | `Draft`, `Pending`, `Delivering`, `Completed`, `Cancelled` |
| Đơn nhập hàng | `PurchaseOrderStatus` | `Draft`, `Posted`, `Cancelled` |
| Người dùng | `UserStatus` | `Active`, `Locked` |

- Tạo các enum dùng chung trong `SalesForecastSystem.Core/Enums` và gán giá trị số cố định cho từng thành viên.
- Giữ các trường `IsActive` ở kiểu `bool` vì chúng biểu diễn trạng thái bật hoặc tắt.
- `ProductStockStatus` đã là enum; rà soát tính nhất quán nhưng không mở rộng phạm vi nghiệp vụ.
- Trạng thái dự báo và chất lượng dữ liệu dự báo được xem xét ở đợt riêng khi có đầy đủ luồng sử dụng.
- Enum không thay thế quy tắc chuyển trạng thái. Frontend hiển thị đủ năm trạng thái: `Draft` là Nháp, `Pending` là Chờ xử lý, `Delivering` là Đang giao, `Completed` là Đã giao và `Cancelled` là Đã hủy.

## Thiết kế lưu trữ và hợp đồng API

### Entity và EF Core

Đổi `SalesOrder.Status`, `PurchaseOrder.Status` và `User.Status` sang enum tương ứng. Giá trị mặc định lần lượt là `Draft`, `Draft` và `Active`.

Cấu hình EF Core bằng `HasConversion<string>()`, giữ cột trạng thái dạng chuỗi tối đa 20 ký tự và cấu hình giá trị mặc định bằng enum:

```csharp
builder.Property(x => x.Status)
    .HasConversion<string>()
    .HasMaxLength(20)
    .IsUnicode(false)
    .HasDefaultValue(SalesOrderStatus.Draft);
```

Database tiếp tục lưu `Draft`, `Pending`, `Completed` và các tên trạng thái hiện có. Không cần migration đổi kiểu cột nếu kiểm tra schema xác nhận cấu hình vẫn tương thích. Tên thành viên enum trở thành giá trị lưu trữ; việc đổi tên sau này phải có kế hoạch chuyển dữ liệu.

### DTO và JSON

- Chuyển trường trạng thái trong request, response và query DTO sang enum tương ứng; bộ lọc tùy chọn dùng enum nullable.
- Cấu hình `JsonStringEnumConverter` với `allowIntegerValues: false` để JSON tiếp tục truyền tên trạng thái và từ chối giá trị số.
- Thay validation bằng regex hoặc danh sách chuỗi bằng kiểm tra enum hợp lệ.
- Dùng `Enum.IsDefined` hoặc validation tương đương cho query DTO vì model binding có thể nhận giá trị số không thuộc enum.
- Với request cập nhật trạng thái bắt buộc, dùng enum nullable kết hợp `[Required]` để phân biệt trường bị thiếu với giá trị mặc định.
- Kiểm tra Swagger, tên giá trị JSON và hành vi chữ hoa/chữ thường để duy trì tương thích API.

## Các bước thực hiện

### 1. Rà soát dữ liệu và điểm sử dụng

- Kiểm tra các giá trị trạng thái đang có trong `Users`, `PurchaseOrders` và `SalesOrders` bằng truy vấn chỉ đọc.
- Xác định dữ liệu không thuộc tập enum trước khi đổi entity; nếu có, lập phương án xử lý cụ thể.
- Rà soát entity, DTO, service, seed dữ liệu, xác thực JWT, báo cáo, dữ liệu dự báo và integration test.
- Rà soát constraint, default, index, trigger và stored procedure phụ thuộc vào tên trạng thái.

### 2. Chuyển đơn bán hàng

- Tạo `SalesOrderStatus` và cập nhật `SalesOrder`, cấu hình EF Core, DTO bán hàng và lịch sử đơn của khách hàng.
- Thay các so sánh chuỗi trong `SalesService` bằng enum.
- Đổi tham số `expected` và `next` của hàm chuyển trạng thái sang `SalesOrderStatus`.
- Giữ điều kiện cập nhật nguyên tử để ngăn chuyển trạng thái đồng thời sai lệch.
- Cập nhật truy vấn báo cáo và dữ liệu dự báo đang lọc theo `Completed`.
- Bộ lọc trả đúng từng trạng thái riêng biệt; danh sách bao gồm cả đơn `Cancelled` để theo dõi lịch sử.

### 3. Chuyển đơn nhập hàng

- Tạo `PurchaseOrderStatus`; cập nhật entity, EF Core, DTO và `PurchaseService`.
- Thay các điều kiện chuỗi bằng enum, giữ quy tắc sửa đơn nháp, ghi nhận nhập kho và hủy đơn.
- Kiểm tra quy trình ghi sổ qua stored procedure vẫn đọc và ghi đúng tên trạng thái.

### 4. Chuyển trạng thái người dùng

- Tạo `UserStatus`; cập nhật entity, EF Core, DTO và `UserService`.
- Hợp nhất các định nghĩa chuỗi trùng nhau `UserStatuses` và `UserStatusValues` vào enum dùng chung.
- Cập nhật `AuthService`, `JwtTokenService`, `SessionJwtEvents`, seed dữ liệu và test.
- Kiểm tra khóa người dùng tiếp tục ngăn đăng nhập và vô hiệu hóa phiên theo cơ chế hiện có.

### 5. Kiểm tra frontend và tài liệu

- Xác nhận frontend vẫn nhận tên trạng thái dạng chữ; badge, bộ lọc và nút thao tác hoạt động đúng.
- Kiểm tra luồng Nháp → Chờ xử lý → Đang giao → Đã giao và thao tác hủy từ các trạng thái chưa hoàn tất. Bước `submit` được thực hiện bằng nút Xác nhận đơn riêng.
- Cập nhật payload mẫu và hướng dẫn API nếu validation hoặc mô tả Swagger thay đổi.
- Build và chạy kiểm thử sau từng nhóm chuyển đổi để xác định lỗi đúng phạm vi.

## Kiểm thử và tiêu chí hoàn thành

- Solution build thành công.
- EF Core đọc và ghi enum đúng với tên trạng thái trong database; dữ liệu hiện có vẫn đọc được.
- JSON response trả tên enum dạng chữ; JSON request từ chối số và tên không hợp lệ.
- Query trạng thái không hợp lệ trả HTTP 400; request thiếu trạng thái bắt buộc trả HTTP 400.
- Bộ lọc, phân trang và lịch sử đơn trả đúng dữ liệu.
- Đơn bán hàng và đơn nhập hàng chỉ chuyển trạng thái theo quy tắc hiện có; thao tác sai trả lỗi phù hợp.
- Các kiểm thử đồng thời, xuất kho, nhập kho và bảo vệ đơn hoàn tất tiếp tục đạt.
- Đăng nhập, khóa tài khoản và kiểm tra phiên JWT hoạt động đúng.
- Kiểm tra truy vấn LINQ và projection DTO được EF Core dịch sang SQL; tránh chuyển enum bằng `ToString()` ở vị trí không được hỗ trợ.
- Chạy bộ integration test trên database kiểm thử riêng, không tạo hoặc chỉnh dữ liệu nghiệp vụ chỉ để kiểm thử.
- Rà soát để loại bỏ các chuỗi trạng thái rải rác trong C# thuộc phạm vi đã chuyển đổi.

## Thứ tự triển khai

1. Rà soát dữ liệu, tạo enum dùng chung và chuẩn hóa JSON/validation.
2. Chuyển `SalesOrderStatus`, kiểm tra luồng bán hàng và báo cáo.
3. Chuyển `PurchaseOrderStatus`, kiểm tra luồng nhập hàng.
4. Chuyển `UserStatus`, kiểm tra xác thực và khóa tài khoản.
5. Chạy kiểm thử tổng thể, kiểm tra frontend và cập nhật tài liệu.

## Phương án khôi phục

Do cấu trúc database và tên trạng thái được giữ tương thích, có thể quay lại phiên bản ứng dụng trước khi refactor nếu kiểm thử phát hiện lỗi. Nếu rà soát phát hiện cần thay đổi schema hoặc dữ liệu, phải bổ sung script chuyển đổi, bản sao lưu và bước khôi phục trước khi triển khai thay đổi đó.
