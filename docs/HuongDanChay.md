# HƯỚNG DẪN CHẠY SALES FORECAST SYSTEM

## 1. Yêu cầu trên máy

- .NET SDK 8 hoặc mới hơn.
- SQL Server Express đang chạy.
- `sqlcmd` hoặc SQL Server Management Studio.
- Server đang cấu hình: `LAPTOP-E5S8NVIT\SQLEXPRESS`.

## 2. Mở thư mục dự án

Mở PowerShell:

```powershell
cd "E:\Thuc_Tap_BE_Cty_Khang_Nghi\DuAnThucTap\SalesForecastSystem"
```

## 3. Tạo hoặc cập nhật cơ sở dữ liệu

```powershell
.\database\Deploy.ps1 -Verify
```

Kết quả đúng sẽ có các dòng `PASS`. Script có thể chạy lại an toàn và không xóa dữ liệu hiện có.

Nếu dùng SSMS, kết nối bằng Windows Authentication đến `LAPTOP-E5S8NVIT\SQLEXPRESS`, sau đó Refresh mục Databases và kiểm tra `SalesForecastingDB`.

## 4. Cấu hình JWT

JWT phải được lưu bằng User Secrets, không ghi khóa vào `appsettings.json` hoặc đẩy lên GitHub.

Khởi tạo một khóa ngẫu nhiên và lưu cấu hình:

```powershell
$jwtBytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Fill($jwtBytes)
$jwtKey = [Convert]::ToBase64String($jwtBytes)

dotnet user-secrets set "Jwt:Key" $jwtKey --project SalesForecastSystem.API
dotnet user-secrets set "Jwt:Issuer" "SalesForecastSystem.API" --project SalesForecastSystem.API
dotnet user-secrets set "Jwt:Audience" "SalesForecastSystem.Client" --project SalesForecastSystem.API
```

## 5. Tạo tài khoản Admin lần đầu

Thay email và mật khẩu bên dưới bằng thông tin riêng. Mật khẩu phải có ít nhất 12 ký tự và không quá 72 byte UTF-8. Không dùng mật khẩu mẫu khi triển khai thật.

```powershell
dotnet user-secrets set "SeedAdmin:Email" "email-cua-ban@example.com" --project SalesForecastSystem.API
dotnet user-secrets set "SeedAdmin:Password" "thay-bang-mat-khau-rieng" --project SalesForecastSystem.API
```

Seeder chỉ tạo Admin khi email chưa tồn tại và không tự đổi mật khẩu của tài khoản đã có.

## 6. Biên dịch và chạy API

```powershell
dotnet build
dotnet run --project SalesForecastSystem.API
```

Khi màn hình hiển thị địa chỉ đang lắng nghe, mở Swagger theo URL trong terminal, thường là:

- `http://localhost:5112/swagger`
- hoặc `https://localhost:7191/swagger`

Giữ cửa sổ PowerShell đang chạy API. Nhấn `Ctrl+C` để dừng.

## 7. Đăng nhập trên Swagger

1. Mở nhóm **Auth**.
2. Chọn `POST /api/auth/login` → **Try it out**.
3. Nhập email và mật khẩu Admin đã cấu hình.
4. Chọn **Execute**.
5. Sao chép giá trị `accessToken` trong response.
6. Chọn nút **Authorize** ở đầu Swagger.
7. Dán access token trực tiếp, không thêm chữ `Bearer`.
8. Chọn **Authorize** và đóng hộp thoại.

Ví dụ request đăng nhập:

```json
{
  "email": "email-cua-ban@example.com",
  "password": "mat-khau-rieng-cua-ban"
}
```

## 8. Demo API danh mục

Sau khi Authorize bằng tài khoản Admin:

### Thêm danh mục

`POST /api/danh-muc`

```json
{
  "tenDanhMuc": "Điện thoại",
  "moTa": "Các sản phẩm điện thoại",
  "trangThai": true
}
```

Kết quả mong đợi: `201 Created`.

### Xem danh sách

Gọi `GET /api/danh-muc`. Kết quả mong đợi: `200 OK`.

### Xem chi tiết

Gọi `GET /api/danh-muc/{id}` với mã vừa tạo. Kết quả mong đợi: `200 OK`.

### Sửa danh mục

`PUT /api/danh-muc/{id}`

```json
{
  "tenDanhMuc": "Điện thoại thông minh",
  "moTa": "Danh mục đã cập nhật",
  "trangThai": true
}
```

Kết quả mong đợi: `200 OK`.

### Xóa danh mục

Gọi `DELETE /api/danh-muc/{id}`. Nếu danh mục chưa có sản phẩm, kết quả mong đợi là `200 OK`.

## 9. Demo các trường hợp lỗi

- Gọi API danh mục khi chưa Authorize: `401 Unauthorized`.
- Đăng nhập `QuanLyKho` hoặc `NhanVienBanHang` và thử thêm/sửa/xóa: `403 Forbidden`.
- Gửi tên danh mục rỗng: `400 Bad Request`.
- Thêm lại cùng tên: `409 Conflict`.
- Xem mã không tồn tại, ví dụ `GET /api/danh-muc/0`: `404 Not Found`.
- Xóa danh mục đang được sản phẩm sử dụng: `409 Conflict`.

## 10. Đăng xuất

Gọi `POST /api/auth/logout` khi đang Authorize. Kết quả mong đợi: `200 OK`.

Sau đó gọi lại `GET /api/auth/me` bằng token cũ. Kết quả mong đợi: `401 Unauthorized`.

## 11. Chạy kiểm thử tự động

```powershell
dotnet run --project tests/SalesForecastSystem.IntegrationChecks
```

Kết quả đúng:

```text
ALL 133 HTTP CHECKS PASSED; Swagger security schema verified.
Temporary test data cleaned up.
```

Bộ kiểm thử tự tạo tài khoản/danh mục/sản phẩm tạm và tự xóa sau khi chạy xong.

## 12. Lỗi thường gặp

### Chưa cấu hình JWT

Nếu ứng dụng báo `Chưa cấu hình Jwt:Key`, thực hiện lại bước 4.

### Không kết nối được SQL Server

- Kiểm tra dịch vụ `SQL Server (SQLEXPRESS)` đang chạy.
- Kiểm tra tên server trong `SalesForecastSystem.API/appsettings.json`.
- Thử kết nối bằng Windows Authentication trong SSMS.

### Không đăng nhập được

- Kiểm tra đúng email đã cấu hình trong `SeedAdmin:Email`.
- Seeder không đổi mật khẩu nếu tài khoản đã tồn tại.
- Kiểm tra tài khoản có trạng thái `Hoạt động` và vai trò có trạng thái bật.

### Swagger trả 401

- Token có thể đã hết hạn sau 15 phút.
- Token có thể đã bị thu hồi khi đăng xuất.
- Đăng nhập lại và cập nhật token trong nút **Authorize**.

## 13. API sản phẩm

API dùng bảng `dbo.SanPham` hiện có, không cần bổ sung cột hoặc chạy migration.
Mọi vai trò đã đăng nhập được xem; chỉ Admin được thêm, sửa, xóa, giống API danh mục.

| Phương thức | Đường dẫn | Chức năng |
| --- | --- | --- |
| GET | `/api/san-pham` | Danh sách sản phẩm |
| GET | `/api/san-pham/{id}` | Chi tiết sản phẩm |
| POST | `/api/san-pham` | Thêm sản phẩm, trả 201 |
| PUT | `/api/san-pham/{id}` | Sửa sản phẩm, trả 200 |
| DELETE | `/api/san-pham/{id}` | Xóa sản phẩm chưa được sử dụng, trả 200 |
| GET | `/api/san-pham/{id}/ton-kho` | Tổng số lượng tồn trên tất cả kho |

Ví dụ nội dung POST/PUT (thay `maDanhMuc` bằng mã danh mục đang tồn tại):

```json
{
  "maDanhMuc": 1,
  "sku": "SP-001",
  "tenSanPham": "Bàn phím",
  "donViTinh": "Cái",
  "giaBan": 250000,
  "trangThai": true
}
```

Quy tắc kiểm tra:

- `MaSanPham` là khóa tự tăng; mã người dùng nhập và cần chống trùng là `sku`.
- SKU bắt buộc, tối đa 50 ký tự ASCII in được; tên tối đa 200 ký tự, đơn vị tính tối đa 30 ký tự. Các trường chuỗi bắt buộc không được chỉ chứa khoảng trắng.
- Bỏ khoảng trắng đầu/cuối trước khi lưu. SKU trùng trả `409`, kể cả khi sửa sang SKU của sản phẩm khác. So sánh chữ hoa/thường theo collation của SQL Server (cơ sở dữ liệu hiện tại không phân biệt hoa/thường).
- Danh mục phải tồn tại, nếu sai trả `400`.
- Giá bán bắt buộc, từ `0` đến `9999999999999999.99`, tối đa 2 chữ số thập phân; thiếu, âm hoặc vượt giới hạn trả `400`. Giá bằng 0 được chấp nhận theo ràng buộc cơ sở dữ liệu hiện có.
- Tồn kho được tính từ giao dịch phiếu nhập/bán. Không gửi `soLuong` hoặc `soLuongTon` trong POST/PUT: số âm, số lẻ, null hoặc sai kiểu trả `400`; số nguyên không âm cũng trả `400` kèm thông báo cần điều chỉnh qua phiếu nhập/bán. API sản phẩm không ghi đè tồn kho.
- Sản phẩm chưa có giao dịch có tồn kho bằng 0. Dữ liệu trả về từ API tồn kho có dạng `{"maSanPham": 1, "soLuongTon": 0}`.
- Mã sản phẩm không tồn tại trả `404`. Xóa sản phẩm đã được sử dụng hoặc gặp thay đổi đồng thời trả `409`.
- Lỗi dữ liệu trả `ValidationProblemDetails` với trường `errors`; lỗi trùng/xung đột trả `ProblemDetails` với trường `title`.
