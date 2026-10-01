# Dữ liệu mẫu và hướng dẫn test trên Swagger

Tài liệu này dùng để tự kiểm tra luồng **tạo dữ liệu → nhập kho → bán hàng → xem tồn kho** trên web Swagger của Sales Forecast System. Các ID trong ví dụ là chỗ giữ chỗ; phải thay bằng ID thực tế trả về sau mỗi lệnh. Dữ liệu được ghi vào SQL Server local, vì vậy hãy dùng mã riêng cho mỗi lần chạy, chẳng hạn thay đuôi `2209A` thành `2209B`.

## 1. Mở API và đăng nhập

Mở PowerShell tại thư mục dự án và chạy:

```powershell
.\database\Deploy.ps1 -Verify
dotnet run --project SalesForecastSystem.API --launch-profile http
```

Giữ cửa sổ này mở. Trên trình duyệt, vào **http://localhost:5112/swagger**. Nếu API báo thiếu `Jwt:Key` hoặc chưa có tài khoản Admin, làm theo mục 4 và 5 trong [Hướng dẫn chạy](HuongDanChay.md) rồi chạy lại API.

Trong Swagger, tìm **Auth → POST /api/auth/login → Try it out**. Dán JSON sau, thay email/mật khẩu bằng tài khoản Admin của bạn, rồi bấm **Execute**:

```json
{
  "email": "email-admin-cua-ban@example.com",
  "password": "mat-khau-admin-cua-ban"
}
```

Response đúng là **200** và có `accessToken`. Sao chép riêng chuỗi `accessToken`. Bấm nút **Authorize** ở góc trên Swagger, dán chuỗi đó **không thêm chữ `Bearer`**, bấm **Authorize**, rồi **Close**. Gọi `GET /api/auth/me` để chắc chắn response có `role: "Admin"`.

Với mỗi API bên dưới: mở nhóm tương ứng → chọn endpoint → **Try it out** → sửa JSON hoặc tham số → **Execute**. Đọc **Server response → Code** và **Response body**. Mỗi ID cần dùng tiếp được lấy từ `Response body`, không lấy ID minh họa trong tài liệu.

## 2. Tạo dữ liệu nền

### Bước 1: Danh mục

**POST /api/categories** — mong đợi **201**.

```json
{
  "name": "Đồ uống Demo 2209A",
  "description": "Danh mục dùng để test Swagger",
  "isActive": true
}
```

Ghi lại `categoryId` từ response: `categoryId = ______`.

### Bước 2: Kho

**POST /api/warehouses** — mong đợi **201**.

```json
{
  "name": "Kho Demo Hà Nội 2209A",
  "address": "Hà Nội",
  "isActive": true
}
```

Ghi lại `warehouseId = ______`.

### Bước 3: Nhà cung cấp

**POST /api/suppliers** — mong đợi **201**.

```json
{
  "name": "Nhà cung cấp Demo 2209A",
  "email": "supplier-2209a@example.com",
  "phoneNumber": "0901234567",
  "address": "Hà Nội",
  "isActive": true
}
```

Ghi lại `supplierId = ______`. Nếu chạy lại bộ dữ liệu, đổi đuôi ở `name` để dễ phân biệt các lần test.

### Bước 4: Khách hàng

**POST /api/customers** — mong đợi **201**.

```json
{
  "fullName": "Nguyễn Văn Demo 2209A",
  "email": "customer-2209a@example.com",
  "phoneNumber": "0912345678",
  "address": "Số 10, Hà Nội",
  "isActive": true
}
```

Ghi lại `customerId = ______`. Email khách hàng phải là duy nhất; khi đổi đuôi mã test, đổi cả email.

### Bước 5: Sản phẩm

**POST /api/products** — **thay số `1` của `categoryId` bằng ID ở bước 1**; mong đợi **201**.

```json
{
  "sku": "DEMO-CAFE-2209A",
  "name": "Cà phê Demo",
  "unit": "gói",
  "description": "Sản phẩm dùng để test nhập và bán",
  "categoryId": 1,
  "salePrice": 120000,
  "minimumStockLevel": 5,
  "isActive": true
}
```

Ghi lại `productId = ______`. `SKU` phải là duy nhất. Gọi **GET /api/products/{productId}/stock**: với sản phẩm mới, `quantityOnHand` phải là **0**.

## 3. Test nhập kho

### Bước 6: Tạo phiếu nhập nháp

**POST /api/purchases** — thay `warehouseId` và `supplierId` bằng ID đã ghi; mong đợi **201**, `status: "Draft"`.

```json
{
  "orderNumber": "PO-DEMO-2209A",
  "warehouseId": 1,
  "supplierId": 1,
  "orderDate": "2026-09-22",
  "notes": "Nhập hàng test trên Swagger"
}
```

Ghi lại `purchaseOrderId = ______`. `orderNumber` phải là duy nhất.

### Bước 7: Thêm dòng nhập

**POST /api/purchases/{purchaseOrderId}/items** — điền ID phiếu trong URL, thay `productId` bằng ID ở bước 5. Mong đợi **200**, response có một dòng và `totalAmount: 1600000`.

```json
{
  "productId": 1,
  "quantity": 20,
  "unitPrice": 80000
}
```

Trước khi xác nhận, gọi lại **GET /api/products/{productId}/stock**: tồn vẫn **0** vì phiếu còn nháp.

### Bước 8: Xác nhận nhập

**POST /api/purchases/{purchaseOrderId}/post** — endpoint này không cần body. Mong đợi **200**, `status: "Posted"`. Gọi **GET /api/products/{productId}/stock**: `quantityOnHand` phải là **20**.

Gọi `/post` thêm một lần: vẫn **200** nhưng tồn **vẫn 20**, không thành 40. Thử **DELETE /api/purchases/{purchaseOrderId}**: nhận **409**, vì phiếu đã ghi sổ không được xóa.

## 4. Test bán hàng

### Bước 9: Tạo đơn nháp

**POST /api/sales** — thay `warehouseId` và `customerId`; mong đợi **201**, `status: "Draft"`. Response phải có `customerName: "Nguyễn Văn Demo 2209A"` và `shippingAddress: "Số 20, Hà Nội"`.

```json
{
  "orderNumber": "SO-DEMO-2209A",
  "warehouseId": 1,
  "customerId": 1,
  "orderDate": "2026-09-22",
  "shippingAddress": "Số 20, Hà Nội",
  "notes": "Bán hàng test trên Swagger"
}
```

Ghi lại `salesOrderId = ______`. Nếu gọi **POST /api/sales/{salesOrderId}/complete** ngay lúc này, phải nhận **409** vì đơn chưa ở trạng thái giao hàng.

### Bước 10: Thêm dòng bán

**POST /api/sales/{salesOrderId}/items** — thay `productId`; mong đợi **200**, response có `totalAmount: 350000` vì `3 × 120000 - 10000 = 350000`.

```json
{
  "productId": 1,
  "quantity": 3,
  "unitPrice": 120000,
  "discount": 10000
}
```

### Bước 11: Chuyển trạng thái và hoàn tất

Gọi **theo đúng thứ tự**, các endpoint này không cần body:

1. **POST /api/sales/{salesOrderId}/submit** → **200**, `Pending`.
2. **POST /api/sales/{salesOrderId}/dispatch** → **200**, `Delivering`.
3. **POST /api/sales/{salesOrderId}/complete** → **200**, `Completed`.

Sau đó gọi **GET /api/products/{productId}/stock**: tồn phải là **17**. Gọi `/complete` lần nữa: response vẫn **200**, tồn **vẫn 17**. Thử sửa hoặc xóa dòng của đơn đã hoàn tất: nhận **409**.

### Bước 12: Xem chứng từ và sổ giao dịch

- **GET /api/purchases/{purchaseOrderId}**: `Posted`, một dòng nhập 20 sản phẩm.
- **GET /api/sales/{salesOrderId}**: `Completed`, `totalAmount: 350000`.
- **GET /api/customers/{customerId}/orders**: thấy đơn `SO-DEMO-2209A`.
- **GET /api/inventory-transactions?warehouseId={warehouseId}&productId={productId}**: với sản phẩm mới chỉ có hai giao dịch, một dòng `quantity: 20` từ phiếu nhập và một dòng `quantity: -3` từ đơn bán.

## 5. Test lỗi nghiệp vụ và phân quyền

| Thao tác | Kết quả đúng |
| --- | --- |
| Tạo sản phẩm lại với `sku: "DEMO-CAFE-2209A"` | **409** trùng SKU |
| Gọi `GET /api/products/0` | **404** không tồn tại |
| Gọi `GET /api/sales?page=0` | **400** trang không hợp lệ |
| Thêm dòng bán với `quantity: 1`, `unitPrice: 100`, `discount: 200` | **400** giảm giá vượt tiền dòng |
| Tạo đơn mới bán 18 sản phẩm từ kho đang còn 17, thêm dòng, submit, dispatch, complete | **409**, tồn vẫn 17; có thể gọi `/cancel` để hủy đơn |
| Bỏ token rồi gọi API nghiệp vụ | **401** chưa đăng nhập |

Để thử trường hợp **không đủ hàng**, tạo đơn khác bằng **POST /api/sales** (thay hai ID):

```json
{
  "orderNumber": "SO-OVER-2209A",
  "warehouseId": 1,
  "customerId": 1,
  "orderDate": "2026-09-22"
}
```

Ghi `salesOrderId` mới, rồi **POST /api/sales/{salesOrderId}/items**:

```json
{
  "productId": 1,
  "quantity": 18,
  "unitPrice": 120000,
  "discount": 0
}
```

Thay `productId`, gọi `/submit` → `/dispatch` → `/complete`. Hai bước đầu nhận **200**; `/complete` nhận **409** vì tồn chỉ còn **17**. Kiểm tra lại stock vẫn **17**, sau đó gọi **POST /api/sales/{salesOrderId}/cancel** để hủy đơn chưa hoàn tất.

Để kiểm tra **403**, Admin có thể tạo tài khoản `SalesStaff` bằng **POST /api/users** (email chưa dùng, mật khẩu ít nhất 12 ký tự):

```json
{
  "fullName": "Nhân viên Demo 2209A",
  "email": "salesstaff-2209a@example.com",
  "phoneNumber": "0987654321",
  "role": "SalesStaff",
  "password": "DemoLocal@22092026"
}
```

Đăng nhập tài khoản mới, bấm **Authorize** và thay token Admin bằng token `SalesStaff`. Gọi **GET /api/customers** hoặc **GET /api/sales** phải nhận **200**; gọi **GET /api/purchases** hoặc **POST /api/products** phải nhận **403**. Mật khẩu mẫu chỉ dùng trên máy local, không dùng cho tài khoản thật.

## 6. Nếu kết quả khác dự kiến

- **Không mở được Swagger:** xem terminal API còn chạy không; dùng đúng `http://localhost:5112/swagger` với profile `http`.
- **401:** đăng nhập lại, sao chép đúng `accessToken` và Authorize; token cũ có thể đã hết hạn hoặc bị thu hồi.
- **403:** kiểm tra `role` qua `GET /api/auth/me`.
- **400:** kiểm tra JSON hợp lệ, các ID đã được thay, sản phẩm/kho/nhà cung cấp đang hoạt động.
- **409 khi tạo:** đổi đuôi `2209A` ở tên, SKU, email và số phiếu/đơn rồi thử lại.
- **409 khi hoàn tất:** kiểm tra trạng thái đã là `Delivering` và tồn trong kho đủ số lượng.

Phiếu đã `Posted` và đơn đã `Completed` giữ lịch sử giao dịch kho, nên không thể xóa qua API. Hãy dùng dữ liệu demo riêng trên database local. Nếu chỉ cần kiểm thử tự động mà không giữ dữ liệu, chạy `./tests/RunDisposableIntegration.ps1` thay cho thao tác thủ công trên Swagger.
