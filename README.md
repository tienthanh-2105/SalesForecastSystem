# SalesForecastSystem

Backend ASP.NET Core 8 cho hệ thống bán hàng và dự báo doanh số, sử dụng SQL Server và Entity Framework Core 8.

- [Hướng dẫn chạy và demo](docs/HuongDanChay.md)
- [Báo cáo công việc ngày 14/09/2026](docs/BaoCaoNgay-2026-09-14.md)

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

## Xác thực, phân quyền và danh mục

Các API đã hoàn thiện:

| API | Quyền |
|---|---|
| `POST /api/auth/login` | Công khai; đăng nhập bằng email và mật khẩu BCrypt |
| `POST /api/auth/logout` | Mọi tài khoản đã đăng nhập; thu hồi phiên hiện tại |
| `GET /api/auth/me` | Mọi tài khoản đã đăng nhập |
| `GET /api/danh-muc` | `Admin`, `QuanLyKho`, `NhanVienBanHang` |
| `GET /api/danh-muc/{id}` | `Admin`, `QuanLyKho`, `NhanVienBanHang` |
| `POST /api/danh-muc` | `Admin` |
| `PUT /api/danh-muc/{id}` | `Admin` |
| `DELETE /api/danh-muc/{id}` | `Admin` |

Mỗi access token tương ứng một dòng trong `PhienDangNhap`. Sau khi logout, token đó trả `401`, kể cả sau khi API khởi động lại. Các phiên đăng nhập khác của cùng tài khoản vẫn còn hiệu lực. Token cũng bị từ chối nếu tài khoản/vai trò bị khóa hoặc vai trò của tài khoản đã thay đổi. Access token hết hạn sau 15 phút.

Tên vai trò lưu trong CSDL được chuẩn hóa thành claim: `Admin`, `QuanLyKho`, `NhanVienBanHang`. Chính sách mặc định yêu cầu đăng nhập cho mọi endpoint không đánh dấu công khai; endpoint kiểm tra CSDL chỉ dành cho Admin.

API danh mục trả:

- `400` cho dữ liệu không hợp lệ;
- `404` khi mã danh mục không tồn tại;
- `409` khi tên bị trùng hoặc danh mục đang được sản phẩm sử dụng;
- `401` khi chưa đăng nhập, token sai/hết hạn/đã thu hồi;
- `403` khi đã đăng nhập nhưng vai trò thiếu quyền.

Swagger hiển thị nút **Authorize**. Đăng nhập, sao chép `accessToken`, bấm **Authorize** và dán token trực tiếp (không thêm chữ `Bearer`).

Chạy toàn bộ kiểm thử tích hợp:

```powershell
dotnet run --project tests/SalesForecastSystem.IntegrationChecks
```

Bộ kiểm thử tạo tài khoản và dữ liệu tạm với tên ngẫu nhiên, kiểm tra 69 trường hợp HTTP rồi xóa chúng. Có thể thêm `-- --swagger` để giữ API chạy phục vụ kiểm tra giao diện; tạo tệp `tests/swagger.stop` để dừng và dọn dữ liệu tạm.
