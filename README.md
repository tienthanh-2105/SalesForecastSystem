# SalesForecastSystem

Backend ASP.NET Core 8 cho hệ thống bán hàng và dự báo doanh số, sử dụng SQL Server và Entity Framework Core 8.

## Phân tích hiện trạng

- `SalesForecastSystem.API`: HTTP API, đăng nhập JWT, Swagger, kiểm tra kết nối CSDL.
- `SalesForecastSystem.Core`: DTO đăng nhập và interface dịch vụ.
- `SalesForecastSystem.Infrastructure`: EF Core, ánh xạ `VaiTro`/`NguoiDung`, kiểm tra mật khẩu BCrypt, tạo Admin theo cấu hình.
- `main.py`, `requirements.txt` ban đầu rỗng; chưa có thuật toán huấn luyện/dự báo.
- Chưa có API nghiệp vụ sản phẩm, đơn hàng, nhập kho hoặc dự báo. CSDL mới chuẩn bị nền tảng cho các chức năng này; chưa đồng nghĩa đã triển khai các API đó.

## Cơ sở dữ liệu trên máy

- Server: `LAPTOP-E5S8NVIT\SQLEXPRESS` (có thể dùng `.\SQLEXPRESS` trên máy này).
- Database: `SalesForecastingDB`.
- Xác thực: Windows Authentication.
- Chuỗi kết nối trong `SalesForecastSystem.API/appsettings.json` đã trỏ đúng server/database nên không cần đổi.

Trong SQL Server Management Studio (SSMS), kết nối server trên, chọn **Databases → Refresh → SalesForecastingDB → Tables**.

Tài liệu thiết kế, quan hệ và cách dùng: [database/README.md](database/README.md).

## Triển khai và kiểm tra lại

Chạy PowerShell tại thư mục dự án:

```powershell
.\database\Deploy.ps1 -Verify
dotnet build --no-restore
```

Cần `sqlcmd` và tài khoản Windows có quyền tạo CSDL. Có thể mở lần lượt `database/001_schema.sql`, `database/002_operations.sql` trong SSMS và Execute. Tệp `003_verify.sql` kiểm thử bằng giao dịch rollback; không giữ lại dữ liệu nghiệp vụ giả.

`001_schema.sql` lưu phiên bản trong `SchemaVersion`, bỏ qua việc tạo bảng khi phiên bản 1 đã tồn tại. `002_operations.sql` cập nhật view/procedure/trigger bằng `CREATE OR ALTER`. Không xóa hay tạo lại database khi chạy lại. Nếu có database từ nguồn khác với bảng trùng tên nhưng chưa có phiên bản, script sẽ dừng và rollback phần tạo bảng để yêu cầu đối chiếu cấu trúc.

Hiện quản lý lược đồ bằng SQL, chưa có EF migrations. Không gọi `EnsureCreated` hoặc tự áp dụng initial migration lên database này; cần lập baseline trước khi chuyển sang EF migrations.

## Chạy API và tạo Admin

Ứng dụng yêu cầu `Jwt:Key` (khóa ngẫu nhiên ít nhất 32 byte), `Jwt:Issuer`, `Jwt:Audience`. Cấu hình qua .NET User Secrets hoặc biến môi trường; không ghi khóa thật vào mã nguồn. Để tạo Admin, thêm `SeedAdmin:Email` và `SeedAdmin:Password` trong môi trường Development rồi chạy API. Mật khẩu ít nhất 12 ký tự, tối đa 72 byte UTF-8; seeder lưu BCrypt và không đổi mật khẩu tài khoản đã tồn tại.

Ví dụ tên biến môi trường: `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `SeedAdmin__Email`, `SeedAdmin__Password`. Không có tài khoản hoặc mật khẩu mặc định trong script SQL.

```powershell
dotnet run --project SalesForecastSystem.API
```

Kiểm tra `GET /api/Database/check`; đăng nhập qua `POST /api/auth/login`. Các endpoint và seeder này chỉ sử dụng hai bảng xác thực đã được ánh xạ trong EF. Các bảng nghiệp vụ mới cần bổ sung entity/configuration, DTO, service và controller khi phát triển chức năng tương ứng.
