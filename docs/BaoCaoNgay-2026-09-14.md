# BÁO CÁO CÔNG VIỆC HẰNG NGÀY

**Ngày thực hiện:** Thứ Hai, ngày 14/09/2026

**Dự án:** Sales Forecast System

**Người thực hiện:** ........................................................

**Người hướng dẫn:** ........................................................

## 1. Mục tiêu trong ngày

- Phân tích cấu trúc dự án backend hiện tại.
- Thiết kế và triển khai cơ sở dữ liệu cho hệ thống bán hàng, kho và dự báo doanh số.
- Hoàn thiện chức năng đăng nhập bằng JWT, đăng xuất và vô hiệu hóa phiên đăng nhập.
- Áp dụng phân quyền cho ba vai trò `Admin`, `QuanLyKho` và `NhanVienBanHang`.
- Xây dựng API quản lý danh mục sản phẩm.
- Kiểm thử xác thực, phân quyền và API danh mục trên Swagger.
- Đồng bộ mã nguồn lên GitHub.

## 2. Công việc đã thực hiện

### 2.1. Phân tích dự án

Dự án được xây dựng bằng ASP.NET Core 8, tổ chức thành ba lớp chính:

- `SalesForecastSystem.API`: cung cấp REST API, Swagger, JWT Authentication và Authorization.
- `SalesForecastSystem.Core`: chứa DTO, interface và các hằng số dùng chung.
- `SalesForecastSystem.Infrastructure`: làm việc với SQL Server thông qua Entity Framework Core, chứa entity, cấu hình ánh xạ và service.

Phần đăng nhập ban đầu đã có cấu trúc cơ bản nhưng chưa quản lý phiên đăng nhập, chưa hỗ trợ đăng xuất thu hồi token và chưa có đầy đủ API nghiệp vụ danh mục.

### 2.2. Thiết kế và triển khai cơ sở dữ liệu

Đã tạo database `SalesForecastingDB` trên SQL Server Express tại `LAPTOP-E5S8NVIT\SQLEXPRESS`.

Cơ sở dữ liệu gồm các nhóm chính:

- Người dùng và phân quyền: `VaiTro`, `NguoiDung`, `PhienDangNhap`.
- Danh mục và sản phẩm: `DanhMuc`, `SanPham`.
- Kho và đối tác: `Kho`, `KhachHang`, `NhaCungCap`.
- Nhập hàng: `PhieuNhap`, `ChiTietPhieuNhap`.
- Bán hàng: `DonHang`, `ChiTietDonHang`.
- Theo dõi tồn kho: `GiaoDichKho`.
- Dự báo: `MoHinhDuBao`, `LanChayDuBao`, `KetQuaDuBao`.
- Quản lý phiên bản database: `SchemaVersion`.

Đã bổ sung khóa chính, khóa ngoại, chỉ mục, ràng buộc dữ liệu, view tổng hợp tồn kho/doanh số và các thủ tục ghi nhận nhập hàng, hoàn tất đơn hàng. Phiên bản cơ sở dữ liệu hiện tại là version 2.

### 2.3. Hoàn thiện đăng nhập JWT

- Nhận email và mật khẩu từ người dùng.
- Chuẩn hóa email trước khi tìm kiếm.
- Dùng BCrypt để so sánh mật khẩu với chuỗi băm trong database.
- Kiểm tra trạng thái tài khoản và trạng thái vai trò.
- Trả access token JWT có thời hạn 15 phút khi đăng nhập thành công.
- JWT chứa mã người dùng, email, họ tên, vai trò và mã phiên đăng nhập.
- Kiểm tra chữ ký, Issuer, Audience và thời hạn token.

### 2.4. Xây dựng đăng xuất

Mỗi access token được liên kết với một bản ghi trong bảng `PhienDangNhap`. API `POST /api/auth/logout` cập nhật thời điểm thu hồi của phiên hiện tại.

Sau khi đăng xuất:

- Token cũ không thể truy cập API và nhận `401 Unauthorized`.
- Trạng thái thu hồi vẫn có hiệu lực sau khi API khởi động lại.
- Phiên khác của cùng người dùng không bị đăng xuất theo.
- Token cũng bị từ chối khi tài khoản bị khóa hoặc vai trò đã thay đổi.

### 2.5. Hoàn thiện phân quyền

Đã chuẩn hóa ba tên quyền dùng trong JWT:

| Vai trò trong database | Quyền trong JWT |
|---|---|
| Admin | `Admin` |
| Quản lý kho | `QuanLyKho` |
| Nhân viên bán hàng | `NhanVienBanHang` |

Các API kiểm tra riêng từng vai trò trả đúng kết quả:

- Chưa đăng nhập: `401 Unauthorized`.
- Đăng nhập nhưng không có quyền: `403 Forbidden`.
- Đăng nhập đúng quyền: `200 OK`.

### 2.6. Xây dựng API danh mục sản phẩm

| Phương thức | Đường dẫn | Chức năng | Quyền |
|---|---|---|---|
| GET | `/api/danh-muc` | Xem danh sách | Cả ba vai trò |
| GET | `/api/danh-muc/{id}` | Xem chi tiết | Cả ba vai trò |
| POST | `/api/danh-muc` | Thêm danh mục | Chỉ Admin |
| PUT | `/api/danh-muc/{id}` | Sửa danh mục | Chỉ Admin |
| DELETE | `/api/danh-muc/{id}` | Xóa danh mục | Chỉ Admin |

API đã xử lý các trường hợp:

- Tên danh mục rỗng, null hoặc vượt quá độ dài: `400 Bad Request`.
- Tên danh mục trùng: `409 Conflict`.
- Không tìm thấy danh mục: `404 Not Found`.
- Danh mục đang được sản phẩm sử dụng: `409 Conflict`.
- Thêm thành công: `201 Created`.
- Xem hoặc sửa thành công: `200 OK`.

### 2.7. Kiểm thử

Đã xây dựng bộ kiểm thử tích hợp tự động và kiểm tra trực tiếp qua Swagger.

Kết quả:

- Dự án build thành công: 0 lỗi, 0 cảnh báo.
- 69/69 trường hợp HTTP đạt yêu cầu.
- Đã kiểm tra ma trận quyền của ba vai trò.
- Đã kiểm tra token sai chữ ký, sai Issuer, hết hạn và đã đăng xuất.
- Đã kiểm tra dữ liệu danh mục sai, trùng tên, không tồn tại và đang được sử dụng.
- Dữ liệu kiểm thử được tự động xóa sau khi hoàn thành.
- Database không còn tài khoản, phiên hoặc danh mục kiểm thử tạm.

### 2.8. Quản lý mã nguồn

Mã nguồn đã được commit và đẩy lên nhánh `master` của GitHub:

- Repository: <https://github.com/tienthanh-2105/SalesForecastSystem>
- Commit hoàn thiện phân quyền và kiểm thử: `a04530a`

## 3. Kết quả đạt được

Hệ thống hiện có nền tảng cơ sở dữ liệu cho bán hàng, tồn kho và dự báo doanh số. Phần xác thực đã hỗ trợ đăng nhập, đăng xuất có thu hồi phiên và kiểm tra trạng thái tài khoản theo mỗi request. Phân quyền ba vai trò hoạt động đúng. API danh mục sản phẩm đã có đầy đủ thao tác xem, thêm, sửa và xóa, đồng thời xử lý các lỗi nghiệp vụ chính.

## 4. Khó khăn và cách giải quyết

- SQL Server yêu cầu thiết lập phù hợp khi tạo chỉ mục trên cột tính toán và filtered index. Đã bổ sung các tùy chọn phiên SQL cần thiết vào script triển khai.
- JWT vốn không thể bị thu hồi ngay trước khi hết hạn. Đã giải quyết bằng cách lưu mã phiên trong database và kiểm tra trạng thái phiên khi xác thực mỗi request.
- Tên vai trò trong database có dấu và khoảng trắng, trong khi code cần tên ổn định. Đã xây dựng bước chuẩn hóa tên vai trò trước khi đưa vào JWT.
- Việc cập nhật tồn kho có nguy cơ ghi nhận trùng. Đã sử dụng transaction, khóa theo kho và ràng buộc duy nhất để bảo vệ dữ liệu.

## 5. Hạn chế hiện tại

- Chưa xây dựng API sản phẩm, kho, khách hàng, nhà cung cấp, nhập hàng và đơn bán hàng.
- Chưa xây dựng chức năng quản lý tài khoản để Admin tạo người dùng cho hai vai trò còn lại.
- Chưa có refresh token; người dùng cần đăng nhập lại sau 15 phút.
- Chưa triển khai thuật toán huấn luyện và dự báo bằng Python.
- Chưa có giao diện frontend.

## 6. Kế hoạch tiếp theo

- Xây dựng API quản lý sản phẩm và áp dụng quyền theo vai trò.
- Xây dựng API kho, nhập hàng và kiểm tra tồn kho.
- Xây dựng API đơn hàng và thống kê doanh số theo ngày/tháng.
- Xây dựng API quản lý người dùng dành cho Admin.
- Chuẩn bị dữ liệu lịch sử và triển khai mô hình dự báo doanh số.
- Kết nối backend với giao diện người dùng.

## 7. Tự đánh giá

Công việc trong ngày đã hoàn thành đúng phạm vi đề ra. Chức năng được kiểm tra cả tự động lẫn trực tiếp trên Swagger. Cấu trúc hiện tại có thể tiếp tục mở rộng sang các nghiệp vụ sản phẩm, kho, bán hàng và dự báo mà không phải thay đổi lại phần xác thực cốt lõi.
