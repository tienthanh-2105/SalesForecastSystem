# Hướng dẫn sử dụng Bộ dữ liệu nhập hàng (Purchase Orders)

File `sample-purchases.json` cung cấp dữ liệu mẫu gồm 4 nhà cung cấp uy tín và 7 phiếu nhập hàng đại diện cho các kịch bản thực tế trong doanh nghiệp:
- **Nhập hàng đầu kỳ / số lượng lớn** (`Posted`): Tạo số dư tồn kho phục vụ bán hàng và dự báo nhu cầu.
- **Nhập bổ sung định kỳ** (`Posted`): Bổ sung hàng điện thoại, laptop, phụ kiện cho từng kho (Hà Nội, TP.HCM).
- **Phiếu nhập nháp** (`Draft`): Kiểm thử luồng kiểm đếm thực tế, sửa thông tin dòng hàng hoặc bấm xác nhận nhập kho trên UI.
- **Phiếu nhập đã hủy** (`Cancelled`): Kiểm thử trường hợp hủy phiếu do nhà cung cấp đứt hàng.

---

## 1. Cách 1: Tự động nạp trực tiếp vào SQL Server bằng script

Nếu bạn đang chạy database cục bộ (`SalesForecastingDB`), có thể chạy file script `database/seed-purchases.sql` qua PowerShell:

```powershell
sqlcmd -S localhost -d SalesForecastingDB -E -i .\database\seed-purchases.sql
```
*(Hoặc mở file `database/seed-purchases.sql` trong SQL Server Management Studio / Azure Data Studio và nhấn Execute).*

Script được viết an toàn (`idempotent`), tự động:
1. Tạo 4 nhà cung cấp mẫu nếu chưa có.
2. Ánh xạ đúng SKU sản phẩm sang `ProductId`.
3. Tạo phiếu nhập và dòng hàng.
4. Gọi `dbo.usp_PostPurchaseOrder` để ghi sổ giao dịch kho (`InventoryTransactions`) đúng chuẩn quy tắc ACID của hệ thống.

---

## 2. Cách 2: Nhập qua giao diện Web (React Frontend)

1. Đăng nhập với tài khoản có quyền Quản lý kho (ví dụ: `warehouse.demo@example.com` hoặc tài khoản `Admin`).
2. Vào menu **Phiếu nhập hàng** (`/purchases`).
3. Nếu nhà cung cấp chưa có trong danh sách:
   - Bấm **Thêm nhà cung cấp**.
   - Điền Tên NCC, Mã số thuế (10 chữ số), Số điện thoại, Địa chỉ từ file `sample-purchases.json`.
4. Bấm **Thêm phiếu nhập**:
   - Chọn Kho nhập (Kho TP. Hồ Chí Minh hoặc Hà Nội).
   - Chọn Nhà cung cấp.
   - Chọn Ngày nhập.
   - Chọn Mã sản phẩm, nhập Số lượng và Đơn giá tương ứng.
   - Bấm **Lưu phiếu nháp**.
5. Khi hàng thực tế về kho, tại dòng phiếu vừa tạo bấm **Xác nhận nhập kho** để kích hoạt tăng số dư tồn kho.

---

## 3. Cách 3: Nhập qua Swagger API

1. Truy cập Swagger tại `http://localhost:5112/swagger` và đăng nhập lấy JWT token từ `POST /api/auth/login`.
2. Bấm nút **Authorize** và dán token.
3. Tạo nhà cung cấp: `POST /api/suppliers`
   ```json
   {
     "name": "Công ty Cổ phần Bán lẻ Kỹ thuật số FPT (Synnex FPT)",
     "taxCode": "0101258670",
     "phoneNumber": "02473006666",
     "email": "contact@synnexfpt.com.vn",
     "address": "Tòa nhà FPT Cầu Giấy, Phố Duy Tân, Cầu Giấy, Hà Nội",
     "isActive": true
   }
   ```
4. Tạo đầu phiếu nhập: `POST /api/purchases`
   ```json
   {
     "orderNumber": "PO-20261001-HCM01",
     "warehouseId": 1,
     "supplierId": 1,
     "orderDate": "2026-10-01",
     "notes": "Nhập kho đợt 1 - Điện thoại Apple chính hãng"
   }
   ```
   *(Lấy `purchaseOrderId` trả về từ response).*
5. Thêm chi tiết dòng hàng: `POST /api/purchases/{purchaseOrderId}/items`
   ```json
   {
     "productId": 1,
     "quantity": 25,
     "unitPrice": 20500000
   }
   ```
6. Xác nhận nhập kho: `POST /api/purchases/{purchaseOrderId}/post`
   *(Response trả về HTTP 200 với trạng thái `Posted`, kiểm tra tồn kho tại `GET /api/products/{productId}/stock` sẽ thấy tồn kho đã tăng tương ứng).*
