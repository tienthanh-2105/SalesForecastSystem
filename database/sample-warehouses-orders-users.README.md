# Bộ dữ liệu mẫu kho, người dùng và đơn hàng

File `sample-warehouses-orders-users.json` chứa 3 kho, 3 tài khoản (đúng 1 Admin, 1 WarehouseManager, 1 SalesStaff) và 5 đơn đủ trạng thái Nháp, Chờ xử lý, Đang giao, Đã giao, Đã hủy. Tất cả thông tin cá nhân/địa chỉ là dữ liệu giả. Chỉ dùng mật khẩu mẫu trên môi trường thử nghiệm.

## Cách nhập qua Swagger/API

Ứng dụng chưa có chức năng import toàn bộ file này. Không gửi cả file JSON làm một request.

1. Đăng nhập bằng Admin hiện có. Gửi từng object trong `users` đến `POST /api/users`. Nếu đang có Admin và muốn toàn hệ thống chỉ có một Admin, giữ tài khoản đó, bỏ qua Admin mẫu; không xóa tài khoản hiện tại. Tài khoản mới được tạo với trạng thái Active.
2. Gửi từng `warehouses[].request` đến `POST /api/warehouses`, ghi lại `warehouseId` trả về ứng với `ref`. Mã `KHO-xxxx` được hiển thị theo ID; không nhập mã hoặc giả định ID bắt đầu từ 1 trên database đã có dữ liệu.
3. Tra các SKU trong `productReferences` qua `GET /api/products?search=...` để lấy `productId`. Nếu SKU chưa có, tạo sản phẩm/danh mục tương ứng trước. Không dùng mã hệ thống SP-xxxx làm SKU hoặc productId.
4. Đăng nhập bằng `sales.demo@example.com` để tạo đơn; backend tự lấy người tạo từ token. Với mỗi đơn, thêm `warehouseId` thực tế vào `request`, rồi gửi đến `POST /api/sales`. `customerId: null` nghĩa là khách lẻ, không cần tạo khách hàng mới.
5. Gửi từng dòng hàng đến `POST /api/sales/{salesOrderId}/items`, thay `sku` bằng `productId` thực tế và chỉ gửi `productId`, `quantity`, `unitPrice`, `discount`.
6. Thực hiện các bước trong `transitions` bằng `POST /api/sales/{salesOrderId}/{action}` theo đúng thứ tự. `targetStatus` chỉ mô tả kết quả mong muốn; không gửi trạng thái trực tiếp khi tạo đơn. `expectedTotalAmount` là tổng đối chiếu, không phải trường gửi lên API.

## Lưu ý tồn kho

Đơn Completed cần kho TP. Hồ Chí Minh có ít nhất 1 iPhone SKU XLA-IP17-256 và 1 laptop SKU LAP-HP14AMD trước khi hoàn tất. Phải tạo và ghi sổ phiếu nhập thật trên môi trường thử nghiệm để có tồn kho; không sửa số lượng trong form sản phẩm hoặc bỏ qua kiểm tra tồn kho. Việc hoàn tất đơn sẽ trừ tồn kho. Nếu API báo thiếu hàng, dừng ở trạng thái Đang giao cho tới khi bổ sung hàng.

Kho Đà Nẵng là kho ngừng hoạt động dùng để kiểm tra bộ lọc, không dùng cho đơn mới. Giảm giá trong mỗi dòng là số tiền giảm của toàn dòng, không phải phần trăm hay giảm trên mỗi đơn vị.

Nhập lại có thể gây lỗi trùng email, tên kho, mã đơn; kiểm tra dữ liệu tồn tại trước khi tạo, không tự tạo bản trùng. File này chưa được nhập vào database.
