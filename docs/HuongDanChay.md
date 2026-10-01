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
ALL 295 HTTP CHECKS PASSED; Swagger security schema verified.
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

## 16. API khách hàng

`Admin` và `SalesStaff` được xem và quản lý khách hàng. `WarehouseManager` nhận `403`; người chưa đăng nhập nhận `401`.

| Phương thức | Đường dẫn | Chức năng |
| --- | --- | --- |
| GET | `/api/customers` | Tìm kiếm, lọc trạng thái và phân trang |
| GET | `/api/customers/{id}` | Xem chi tiết |
| GET | `/api/customers/{id}/orders` | Xem lịch sử đơn hàng theo trang |
| POST | `/api/customers` | Tạo, trả `201` |
| PUT | `/api/customers/{id}` | Sửa, trả `200` |
| DELETE | `/api/customers/{id}` | Chuyển `isActive` thành `false`, trả `200` |

GET danh sách hỗ trợ `search` (họ tên, email hoặc số điện thoại), `isActive`, `page` (mặc định 1) và `pageSize` (mặc định 20, tối đa 100). GET lịch sử hỗ trợ `page` và `pageSize`, sắp xếp ngày đơn mới nhất trước. Trang vượt quá dữ liệu trả `items: []`.

Ví dụ tạo khách hàng:

```json
{
  "fullName": "Nguyễn Văn A",
  "email": "customer@example.com",
  "phoneNumber": "0901234567",
  "address": "Hà Nội",
  "isActive": true
}
```

Họ tên bắt buộc, tối đa 100 ký tự. Email và số điện thoại không bắt buộc nhưng phải hợp lệ nếu được cung cấp; email được chuẩn hóa chữ thường và phải duy nhất. Email trùng trả `409`; dữ liệu không hợp lệ trả `400`; mã khách hàng không tồn tại trả `404`. DELETE chỉ ngừng hoạt động để giữ tham chiếu và lịch sử đơn hàng.

## 17. API phiếu nhập

`Admin` và `WarehouseManager` được sử dụng `/api/purchases`; người chưa đăng nhập nhận `401`, `SalesStaff` nhận `403`.

| Phương thức | Đường dẫn | Chức năng |
| --- | --- | --- |
| GET | `/api/purchases` | Lọc theo `warehouseId`, `supplierId`, `status`, `fromDate`, `toDate`; phân trang |
| GET | `/api/purchases/{id}` | Xem phiếu và chi tiết |
| POST | `/api/purchases` | Tạo phiếu `Draft` |
| PUT | `/api/purchases/{id}` | Sửa phiếu nháp |
| POST / PUT / DELETE | `/api/purchases/{id}/items[/{itemId}]` | Thêm, sửa, xóa dòng nháp |
| POST | `/api/purchases/{id}/post` | Xác nhận nhập và ghi tăng tồn kho |
| POST | `/api/purchases/{id}/cancel` | Hủy phiếu chưa ghi sổ |
| DELETE | `/api/purchases/{id}` | Xóa phiếu nháp |

Ví dụ tạo phiếu rồi thêm dòng:

```json
{ "orderNumber": "PO-2026-001", "warehouseId": 1, "supplierId": 1, "orderDate": "2026-09-22", "notes": "Nhập hàng" }
```

```json
{ "productId": 1, "quantity": 10, "unitPrice": 6000 }
```

Phiếu đã `Posted` hoặc `Cancelled` không thể sửa. Gọi `post` lặp lại trên phiếu đã `Posted` không ghi tăng tồn lần thứ hai. Phiếu trống, kho hoặc sản phẩm ngừng hoạt động và dữ liệu trùng trả lỗi phù hợp (`400` hoặc `409`). Tồn kho được tính từ `InventoryTransactions`.

## 18. API đơn hàng bán

`Admin` và `SalesStaff` được sử dụng `/api/sales`; `WarehouseManager` nhận `403`.

| Phương thức | Đường dẫn | Chức năng |
| --- | --- | --- |
| GET | `/api/sales` | Lọc theo kho, khách hàng, nhân viên, trạng thái, ngày; phân trang |
| GET | `/api/sales/{id}` | Xem đơn và chi tiết |
| POST | `/api/sales` | Tạo đơn `Draft` |
| PUT | `/api/sales/{id}` | Sửa đơn nháp |
| POST / PUT / DELETE | `/api/sales/{id}/items[/{itemId}]` | Thêm, sửa, xóa dòng nháp |
| POST | `/api/sales/{id}/submit` | Chuyển sang `Pending` |
| POST | `/api/sales/{id}/dispatch` | Chuyển sang `Delivering` |
| POST | `/api/sales/{id}/complete` | Hoàn tất, ghi xuất kho và chuyển sang `Completed` |
| POST | `/api/sales/{id}/cancel` | Hủy đơn chưa hoàn tất |
| DELETE | `/api/sales/{id}` | Xóa đơn nháp |

Ví dụ tạo đơn:

```json
{ "orderNumber": "SO-2026-001", "warehouseId": 1, "customerId": 1, "orderDate": "2026-09-22", "shippingAddress": "Hà Nội" }
```

Dòng đơn gồm `productId`, `quantity`, `unitPrice` và `discount`; tiền dòng bằng `quantity × unitPrice − discount`. Hệ thống lưu tên và số điện thoại khách hàng tại thời điểm tạo hoặc sửa đơn nháp. Luồng trạng thái API là `Draft → Pending → Delivering → Completed`; có thể hủy trước `Completed`. Chi tiết chỉ sửa được ở `Draft`. Xác nhận lặp không xuất kho lần hai; thiếu hàng trả `409` và không làm giảm tồn. Khách hàng có thể để trống cho giao dịch không gắn hồ sơ khách hàng.

Kiểm thử hồi quy thông thường: `dotnet run --project tests/SalesForecastSystem.IntegrationChecks`. Kiểm thử đầy đủ nhập → bán → tồn kho trên database tự tạo rồi xóa: `./tests/RunDisposableIntegration.ps1`.

## 19. Lịch sử giao dịch kho

Mọi vai trò đã đăng nhập được gọi `GET /api/inventory-transactions`. Bộ lọc gồm `warehouseId`, `productId`, `fromDate`, `toDate`, `page` và `pageSize` (tối đa 100). Mỗi dòng cho biết số lượng tăng hoặc giảm, ngày giao dịch và mã dòng phiếu nhập/đơn hàng nguồn. API chỉ đọc; mọi ghi nhận tồn kho vẫn phải qua thủ tục xác nhận chứng từ.

## 20. Test luồng nhập - bán trên web Swagger

Xem [dữ liệu mẫu và hướng dẫn thao tác chi tiết](DuLieuMauVaHuongDanTestSwagger-2026-09-22.md) để có JSON cho từng endpoint, chỗ ghi ID, kết quả mong đợi và các trường hợp lỗi.

1. Trong thư mục dự án, chạy `./database/Deploy.ps1 -Verify`, sau đó `dotnet run --project SalesForecastSystem.API --launch-profile http`. Mở `http://localhost:5112/swagger`. Nếu API chưa có JWT hoặc Admin, thực hiện mục 4 và 5 ở trên trước khi chạy.
2. Gọi `POST /api/auth/login` bằng tài khoản Admin. Sao chép `accessToken`, bấm **Authorize** ở đầu trang và dán token, không thêm `Bearer`.
3. Tạo lần lượt các dữ liệu bên dưới. Sau mỗi lệnh, ghi lại ID trả về để dùng cho lệnh sau. Dùng tên và mã khác nhau nếu chạy lại để tránh lỗi trùng dữ liệu.

| API | JSON mẫu | Kết quả |
| --- | --- | --- |
| `POST /api/categories` | `{"name":"Demo 2209","isActive":true}` | `201`, ghi `categoryId` |
| `POST /api/warehouses` | `{"name":"Kho Demo 2209","isActive":true}` | `201`, ghi `warehouseId` |
| `POST /api/suppliers` | `{"name":"NCC Demo 2209","isActive":true}` | `201`, ghi `supplierId` |
| `POST /api/customers` | `{"fullName":"Khách Demo 2209","phoneNumber":"0901234567"}` | `201`, ghi `customerId` |
| `POST /api/products` | `{"sku":"DEMO-2209","name":"Sản phẩm Demo","unit":"cái","categoryId":1,"salePrice":100,"minimumStockLevel":0,"isActive":true}` | Thay `categoryId`; `201`, ghi `productId` |

4. Tạo phiếu nhập bằng `POST /api/purchases` với `{"orderNumber":"PO-DEMO-2209","warehouseId":1,"supplierId":1,"orderDate":"2026-09-22"}`. Thay hai ID và ghi `purchaseOrderId` từ response. Gọi `POST /api/purchases/{purchaseOrderId}/items` với `{"productId":1,"quantity":10,"unitPrice":60}`; thay ID sản phẩm. Gọi `POST /api/purchases/{purchaseOrderId}/post`. Kết quả là `200`, trạng thái `Posted`.
5. Gọi `GET /api/products/{productId}/stock`. Với sản phẩm mới chỉ có phiếu nhập trên, `quantityOnHand` phải là `10`. Gọi lại endpoint `post`: vẫn `200` và tồn vẫn `10`.
6. Tạo đơn bằng `POST /api/sales` với `{"orderNumber":"SO-DEMO-2209","warehouseId":1,"customerId":1,"orderDate":"2026-09-22","shippingAddress":"Hà Nội"}`. Thay ID và ghi `salesOrderId`. Gọi `POST /api/sales/{salesOrderId}/items` với `{"productId":1,"quantity":4,"unitPrice":100,"discount":20}`.
7. Gọi theo thứ tự `POST /api/sales/{salesOrderId}/submit`, `/dispatch`, rồi `/complete`. Đơn chuyển thành `Completed`, `totalAmount` là `380`. Gọi lại `GET /api/products/{productId}/stock`: tồn là `6`. Gọi `GET /api/inventory-transactions?warehouseId=1&productId=1` sau khi thay ID: có một dòng nhập `+10` và một dòng bán `-4`.

Nếu nhận `401`, đăng nhập và Authorize lại. Nếu nhận `403`, kiểm tra vai trò của tài khoản. Nếu nhận `409` ở bước tạo, dùng `orderNumber` hoặc `sku` mới; nếu nhận `409` khi hoàn tất đơn, kiểm tra tồn kho và thứ tự trạng thái. Các thao tác tạo và xác nhận trong hướng dẫn ghi dữ liệu thật vào database local, nên dùng mã `DEMO` riêng và tránh làm trên dữ liệu thật của doanh nghiệp.

## 21. API báo cáo bán hàng

Mọi vai trò đã đăng nhập được xem báo cáo. Người chưa đăng nhập nhận `401`. Báo cáo doanh số chỉ tính đơn có trạng thái `Completed`.

| Đường dẫn | Nội dung |
| --- | --- |
| `GET /api/reports/sales-summary` | Tổng số đơn, số lượng bán và doanh thu |
| `GET /api/reports/sales-trend` | Dữ liệu xu hướng theo `Day`, `Week` hoặc `Month` |
| `GET /api/reports/top-products` | Xếp hạng sản phẩm theo doanh thu |
| `GET /api/reports/top-categories` | Xếp hạng danh mục theo doanh thu |
| `GET /api/reports/sales-by-staff` | Hiệu suất nhân viên theo doanh thu |
| `GET /api/reports/inventory` | Tổng tồn kho và trạng thái dưới ngưỡng |

Các báo cáo bán hàng hỗ trợ `fromDate`, `toDate`, `warehouseId`, `productId`, `categoryId` và `staffUserId`. Khoảng ngày tối đa 366 ngày. Các API xếp hạng nhận `top` từ 1 đến 100. Báo cáo tồn kho nhận `warehouseId` và `belowMinimumOnly`.

Ví dụ kiểm tra trên Swagger sau khi đăng nhập và **Authorize**:

```text
GET /api/reports/sales-summary?fromDate=2026-09-01&toDate=2026-09-30
GET /api/reports/sales-trend?period=Day&warehouseId=1
GET /api/reports/top-products?top=10
GET /api/reports/top-categories?top=5
GET /api/reports/sales-by-staff?top=10
GET /api/reports/inventory?warehouseId=1&belowMinimumOnly=true
```

Nếu chưa có đơn `Completed`, các danh sách doanh số trả mảng rỗng và tổng quan trả các giá trị bằng 0. Để đối chiếu nhanh, hoàn thành đơn mẫu ở mục 20 rồi lọc theo đúng `warehouseId` và `productId`: tổng quan phải có `orderCount = 1`, `quantitySold = 4`, `revenue = 380`; tồn kho của sản phẩm là `6`.

## 22. API chuẩn bị dữ liệu dự báo

Mọi vai trò đã đăng nhập được gọi `GET /api/forecast-data/daily`. API tạo chuỗi dữ liệu bán hàng liên tục theo ngày cho một sản phẩm, dùng làm đầu vào cho bước huấn luyện mô hình.

Tham số bắt buộc:

- `productId`: mã sản phẩm.
- `fromDate` và `toDate`: khoảng dữ liệu, tối thiểu 2 ngày và tối đa 1095 ngày.

Tham số tùy chọn:

- `warehouseId`: giới hạn dữ liệu tại một kho.
- `validationPercentage`: tỷ lệ tập kiểm định từ 10 đến 50, mặc định 20.

Ví dụ:

```text
GET /api/forecast-data/daily?productId=1&warehouseId=1&fromDate=2026-09-20&toDate=2026-09-24&validationPercentage=40
```

Mỗi ngày có `quantitySold`, `closingStock`, `dataStatus` và `datasetSplit`. API chỉ cộng số lượng từ đơn `Completed`; ngày không bán có `quantitySold = 0`. Trạng thái dữ liệu gồm:

- `Sold`: có phát sinh bán hàng.
- `NoSale`: không bán nhưng còn tồn kho.
- `StockOut`: không bán và tồn kho cuối ngày bằng 0 hoặc âm.
- `MissingInventoryHistory`: chưa có giao dịch kho để xác định khả năng sẵn có.

Các điểm cũ được gắn `Training`; phần cuối chuỗi được gắn `Validation`. `validationStartDate` cho biết ngày bắt đầu tập kiểm định. Sản phẩm hoặc kho không tồn tại trả `404`; khoảng ngày hoặc tỷ lệ sai trả `400`; chưa đăng nhập trả `401`.
