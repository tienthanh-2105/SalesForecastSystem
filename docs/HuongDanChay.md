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

`POST /api/categories`

```json
{
  "name": "Điện thoại",
  "description": "Các sản phẩm điện thoại",
  "isActive": true
}
```

Kết quả mong đợi: `201 Created`.

### Xem danh sách

Gọi `GET /api/categories`. Kết quả mong đợi: `200 OK`.

### Xem chi tiết

Gọi `GET /api/categories/{id}` với mã vừa tạo. Kết quả mong đợi: `200 OK`.

### Sửa danh mục

`PUT /api/categories/{id}`

```json
{
  "name": "Điện thoại thông minh",
  "description": "Danh mục đã cập nhật",
  "isActive": true
}
```

Kết quả mong đợi: `200 OK`.

### Xóa danh mục

Gọi `DELETE /api/categories/{id}`. Nếu danh mục chưa có sản phẩm, kết quả mong đợi là `200 OK`.

## 9. Demo các trường hợp lỗi

- Gọi API danh mục khi chưa Authorize: `401 Unauthorized`.
- Đăng nhập `WarehouseManager` hoặc `SalesStaff` và thử thêm/sửa/xóa: `403 Forbidden`.
- Gửi tên danh mục rỗng: `400 Bad Request`.
- Thêm lại cùng tên: `409 Conflict`.
- Xem mã không tồn tại, ví dụ `GET /api/categories/0`: `404 Not Found`.
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
ALL 236 HTTP CHECKS PASSED; Swagger security schema verified.
Temporary test data cleaned up.
```

Bộ kiểm thử tự tạo tài khoản/danh mục/sản phẩm/kho/nhà cung cấp và phiếu nhập tạm, rồi tự xóa sau khi chạy xong.

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

API dùng bảng `dbo.Products`. Schema được quản lý bằng các migration SQL có version trong thư mục `database`.
Mọi vai trò đã đăng nhập được xem. Admin và WarehouseManager được thêm, sửa và ngừng hoạt động sản phẩm; SalesStaff chỉ được xem.

| Phương thức | Đường dẫn | Chức năng |
| --- | --- | --- |
| GET | `/api/products` | Tìm kiếm, lọc, sắp xếp và phân trang sản phẩm |
| GET | `/api/products/{id}` | Chi tiết sản phẩm |
| POST | `/api/products` | Thêm sản phẩm, trả 201 |
| PUT | `/api/products/{id}` | Sửa sản phẩm, trả 200 |
| DELETE | `/api/products/{id}` | Ngừng hoạt động sản phẩm, không xóa dữ liệu, trả 200 |
| GET | `/api/products/{id}/stock` | Tổng số lượng tồn trên tất cả kho |

Tham số của `GET /api/products`:

| Tham số | Giá trị |
| --- | --- |
| `search` | Tìm trong tên hoặc SKU, tối đa 200 ký tự |
| `categoryId` | Mã danh mục, số nguyên dương |
| `isActive` | `true` hoặc `false` |
| `stockStatus` | `OutOfStock`, `Low` hoặc `Available` |
| `sortBy` | `Name`, `SKU`, `SalePrice`, `CreatedAt` hoặc `QuantityOnHand` |
| `sortDirection` | `Asc` hoặc `Desc` |
| `page` | Trang cần lấy, mặc định 1 |
| `pageSize` | Số bản ghi mỗi trang, mặc định 20, tối đa 100 |

Ví dụ:

```http
GET /api/products?search=SP&categoryId=1&isActive=true&stockStatus=Low&sortBy=SalePrice&sortDirection=Desc&page=1&pageSize=20
```

Response danh sách trả `items`, `page`, `pageSize`, `totalItems`, `totalPages`, `hasPreviousPage` và `hasNextPage`. Trang vượt quá dữ liệu trả `200 OK` với `items: []`. Quy tắc mức tồn là: `OutOfStock` khi tồn không lớn hơn 0; `Low` khi tồn lớn hơn 0 và không vượt ngưỡng tối thiểu; `Available` khi tồn lớn hơn ngưỡng tối thiểu.

Ví dụ nội dung POST/PUT (thay `categoryId` bằng mã danh mục đang tồn tại):

```json
{
  "categoryId": 1,
  "sku": "SP-001",
  "name": "Bàn phím",
  "unit": "Cái",
  "description": "Bàn phím cơ có dây",
  "imageUrl": "https://example.com/images/keyboard.jpg",
  "salePrice": 250000,
  "minimumStockLevel": 10,
  "isActive": true
}
```

Khi gọi PUT, gửi thêm `rowVersion` lấy từ response GET/POST gần nhất:

```json
{
  "categoryId": 1,
  "sku": "SP-001",
  "name": "Bàn phím",
  "unit": "Cái",
  "description": "Bàn phím cơ có dây",
  "imageUrl": "https://example.com/images/keyboard.jpg",
  "salePrice": 250000,
  "minimumStockLevel": 10,
  "isActive": true,
  "rowVersion": "AAAAAAAAB9E="
}
```

Quy tắc kiểm tra:

- `productId` là khóa tự tăng; mã người dùng nhập và cần chống trùng là `sku`.
- SKU bắt buộc, tối đa 50 ký tự ASCII in được; tên tối đa 200 ký tự, đơn vị tính tối đa 30 ký tự. Các trường chuỗi bắt buộc không được chỉ chứa khoảng trắng.
- Bỏ khoảng trắng đầu/cuối trước khi lưu. SKU trùng trả `409`, kể cả khi sửa sang SKU của sản phẩm khác. So sánh chữ hoa/thường theo collation của SQL Server (cơ sở dữ liệu hiện tại không phân biệt hoa/thường).
- Danh mục phải tồn tại, nếu sai trả `400`.
- Giá bán bắt buộc, từ `0` đến `9999999999999999.99`, tối đa 2 chữ số thập phân; thiếu, âm hoặc vượt giới hạn trả `400`. Giá bằng 0 được chấp nhận theo ràng buộc cơ sở dữ liệu hiện có.
- Mô tả tối đa 1000 ký tự, URL ảnh tối đa 2048 ký tự và phải là URL hợp lệ. Ngưỡng tồn tối thiểu là số nguyên không âm.
- PUT bắt buộc có `rowVersion`. Nếu sản phẩm đã được người khác cập nhật, API trả `409`; client phải tải lại dữ liệu trước khi thử lại.
- Tồn kho được tính từ giao dịch phiếu nhập/bán. Không gửi `quantity` hoặc `quantityOnHand` trong POST/PUT: số âm, số lẻ, null hoặc sai kiểu trả `400`; số nguyên không âm cũng trả `400` kèm thông báo cần điều chỉnh qua phiếu nhập/bán. API sản phẩm không ghi đè tồn kho.
- Sản phẩm chưa có giao dịch có tồn kho bằng 0. Dữ liệu trả về từ API tồn kho có dạng `{"productId": 1, "quantityOnHand": 0}`.
- Mã sản phẩm không tồn tại trả `404`. DELETE chỉ chuyển `isActive` về `false`, vì vậy sản phẩm vẫn còn để bảo toàn lịch sử chứng từ.
- Lỗi dữ liệu trả `ValidationProblemDetails` với trường `errors`; lỗi trùng/xung đột trả `ProblemDetails` với trường `title`.

## 14. API quản lý người dùng

Chỉ tài khoản có vai trò `Admin` được gọi các API dưới đây. `WarehouseManager`, `SalesStaff` nhận `403 Forbidden`; người chưa đăng nhập nhận `401 Unauthorized`.

| Phương thức | Đường dẫn | Chức năng |
| --- | --- | --- |
| GET | `/api/users` | Tìm kiếm, lọc và phân trang người dùng |
| GET | `/api/users/{id}` | Xem chi tiết người dùng |
| POST | `/api/users` | Tạo tài khoản đang hoạt động, trả `201 Created` |
| PUT | `/api/users/{id}` | Sửa họ tên, email, số điện thoại và vai trò |
| PUT | `/api/users/{id}/status` | Khóa hoặc mở khóa tài khoản |
| POST | `/api/users/{id}/reset-password` | Đặt lại mật khẩu và thu hồi toàn bộ phiên |

Tham số của `GET /api/users`:

| Tham số | Giá trị |
| --- | --- |
| `search` | Tìm trong họ tên hoặc email, tối đa 200 ký tự |
| `role` | `Admin`, `WarehouseManager` hoặc `SalesStaff` |
| `status` | `Active` hoặc `Locked` |
| `page` | Trang cần lấy, mặc định 1 |
| `pageSize` | Số bản ghi mỗi trang, mặc định 20, tối đa 100 |

Ví dụ tạo tài khoản:

```json
{
  "fullName": "Nguyễn Văn A",
  "email": "nguyenvana@example.com",
  "phoneNumber": "0901234567",
  "role": "SalesStaff",
  "password": "mat-khau-rieng-toi-thieu-12-ky-tu"
}
```

Ví dụ khóa tài khoản và đặt lại mật khẩu:

```json
{ "status": "Locked" }
```

```json
{ "newPassword": "mat-khau-moi-toi-thieu-12-ky-tu" }
```

Quy tắc kiểm tra và bảo mật:

- Email được bỏ khoảng trắng đầu/cuối, chuyển thành chữ thường và phải duy nhất. Email sai định dạng trả `400`; email trùng trả `409`.
- Họ tên tối đa 100 ký tự; số điện thoại tối đa 15 ký tự; vai trò phải tồn tại và đang hoạt động.
- Mật khẩu có ít nhất 12 ký tự và không quá 72 byte UTF-8 để phù hợp với BCrypt.
- Response danh sách, chi tiết, tạo và sửa không trả mật khẩu hoặc password hash.
- Khóa tài khoản làm token cũ mất hiệu lực và ngăn đăng nhập mới. Mở khóa không tự tạo phiên đăng nhập.
- Đổi vai trò hoặc thông tin nhận diện làm các phiên cũ bị thu hồi. Đặt lại mật khẩu cũng thu hồi toàn bộ phiên đang hoạt động.
- Admin không được tự khóa tài khoản hoặc tự đổi vai trò của mình.
- Trang vượt quá dữ liệu trả `200 OK` với `items: []`; mã người dùng không tồn tại trả `404 Not Found`.

## 15. API kho và nhà cung cấp

Kho: mọi vai trò đã đăng nhập được xem; `Admin` và `WarehouseManager` được tạo, sửa và ngừng hoạt động. Nhà cung cấp: chỉ `Admin` và `WarehouseManager` được xem và thay đổi. Người chưa đăng nhập nhận `401`, sai vai trò nhận `403`.

| Phương thức | Kho | Nhà cung cấp | Chức năng |
| --- | --- | --- | --- |
| GET | `/api/warehouses` | `/api/suppliers` | Tìm kiếm, lọc và phân trang |
| GET | `/api/warehouses/{id}` | `/api/suppliers/{id}` | Xem chi tiết |
| POST | `/api/warehouses` | `/api/suppliers` | Tạo, trả `201` |
| PUT | `/api/warehouses/{id}` | `/api/suppliers/{id}` | Sửa, trả `200` |
| DELETE | `/api/warehouses/{id}` | `/api/suppliers/{id}` | Chuyển `isActive` thành `false`, trả `200` |

GET danh sách hỗ trợ `search`, `isActive`, `page` (mặc định 1) và `pageSize` (mặc định 20, tối đa 100). Kho tìm theo tên/địa chỉ; nhà cung cấp tìm theo tên/mã số thuế/email/số điện thoại. Danh sách trả metadata phân trang như API sản phẩm. Trang vượt quá dữ liệu trả danh sách rỗng.

Ví dụ tạo kho:

```json
{ "name": "Kho Hà Nội", "address": "Hà Nội", "isActive": true }
```

Ví dụ tạo nhà cung cấp:

```json
{
  "name": "Nhà cung cấp A",
  "taxCode": "0123456789",
  "email": "contact@example.com",
  "phoneNumber": "0901234567",
  "address": "Hà Nội",
  "isActive": true
}
```

Tên kho bắt buộc, tối đa 100 ký tự và duy nhất. Tên nhà cung cấp bắt buộc, tối đa 200 ký tự; mã số thuế tùy chọn, tối đa 20 ký tự và duy nhất nếu được cung cấp. Email và số điện thoại phải hợp lệ. Tên rỗng và tham số phân trang sai trả `400`; trùng tên kho hoặc mã số thuế trả `409`; mã không tồn tại trả `404`. DELETE chỉ ngừng hoạt động nên kho/nhà cung cấp đã được chứng từ tham chiếu vẫn còn để bảo toàn lịch sử.
