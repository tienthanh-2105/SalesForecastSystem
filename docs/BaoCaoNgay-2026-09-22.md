# BÁO CÁO CÔNG VIỆC HẰNG NGÀY

**Ngày thực hiện:** Thứ Ba, ngày 22/09/2026

**Dự án:** Sales Forecast System

## 1. Mục tiêu trong ngày

- Hoàn thiện module quản lý khách hàng cho Admin và Nhân viên bán hàng.
- Bổ sung tìm kiếm theo họ tên, email hoặc số điện thoại và phân trang danh sách.
- Chuẩn bị API xem lịch sử đơn hàng của từng khách hàng.
- Kiểm tra dữ liệu đầu vào, dữ liệu trùng, khách hàng không tồn tại và phân quyền.
- Chạy toàn bộ kiểm thử hồi quy HTTP và kiểm chứng ACID của database.

## 2. Công việc đã thực hiện

### 2.1. API quản lý khách hàng

Đã bổ sung các API:

- `GET /api/customers` và `GET /api/customers/{id}` để xem danh sách, chi tiết.
- `POST /api/customers` và `PUT /api/customers/{id}` để tạo, sửa thông tin.
- `DELETE /api/customers/{id}` để ngừng hoạt động, không xóa bản ghi.
- `GET /api/customers/{id}/orders` để xem lịch sử đơn hàng.

Danh sách hỗ trợ tìm theo họ tên, email hoặc số điện thoại; lọc trạng thái hoạt động và phân trang tối đa 100 bản ghi mỗi trang. Lịch sử đơn hàng được sắp theo ngày đơn mới nhất và có phân trang. Khách hàng đã ngừng hoạt động vẫn xem được thông tin và lịch sử đơn hàng.

### 2.2. Database, validation và phân quyền

- Nâng schema database lên version 7, thêm trạng thái `IsActive` cho khách hàng.
- Thêm unique index cho email khách hàng khi email được cung cấp; email được chuyển về chữ thường trước khi lưu.
- Kiểm tra họ tên bắt buộc; email và số điện thoại phải đúng định dạng nếu được cung cấp.
- Email trùng trả `409`, dữ liệu không hợp lệ trả `400`, khách hàng không tồn tại trả `404`.
- `Admin` và `SalesStaff` được xem và quản lý khách hàng; `WarehouseManager` bị từ chối với `403`, người chưa đăng nhập nhận `401`.
- Cập nhật Swagger và hướng dẫn chạy API cho các đường dẫn mới.

### 2.3. Kiểm thử

- Bổ sung 30 kiểm tra HTTP cho module khách hàng: tạo, sửa, xem, tìm kiếm, phân trang, phân quyền, validation, email trùng, lịch sử đơn hàng và ngừng hoạt động khi đã có đơn hàng tham chiếu.
- Toàn bộ `266/266` kiểm tra HTTP đạt, gồm 236 kiểm tra hồi quy trước đó.
- `7/7` kiểm chứng ACID của database đạt; chạy lại script triển khai xác nhận schema version 7 có thể áp dụng lặp lại.
- Solution build sạch với 0 warning, 0 error. Dữ liệu tạm được dọn sau khi kiểm thử.

## 3. Kết quả

Kế hoạch Ngày 2 của Tuần 2 đã hoàn thành. Module khách hàng đáp ứng luồng database, service, API, phân quyền, Swagger và E2E. Thông tin khách hàng và liên kết với đơn hàng được giữ lại khi ngừng hoạt động.

## 4. Kế hoạch tiếp theo

- Xây dựng API phiếu nhập nháp và các dòng chi tiết phiếu nhập.
- Triển khai xác nhận nhập hàng để ghi tăng tồn kho qua stored procedure hiện có.
- Kiểm thử xác nhận lặp, rollback khi có dòng lỗi và quyền truy cập của từng vai trò.

## 5. Công việc triển khai sớm sau báo cáo

- Hoàn thiện API phiếu nhập: tạo/sửa/xóa nháp, quản lý dòng, hủy, xác nhận nhập và xem lịch sử có lọc/phân trang.
- Hoàn thiện API đơn hàng: tạo/sửa/xóa nháp, quản lý dòng, lưu thông tin khách hàng và địa chỉ giao hàng, chuyển trạng thái, hủy và hoàn tất xuất kho.
- Nâng schema lên version 8 cho trạng thái `Pending`, `Delivering` và thông tin khách hàng tại thời điểm đặt. Xác nhận nhập và hoàn tất đơn tiếp tục đi qua stored procedure với khóa kho và transaction.
- Thêm bài kiểm thử liên hoàn trên database dùng một lần để không để lại chứng từ đã ghi sổ trong database chính.
- Bổ sung API chỉ đọc lịch sử giao dịch kho với lọc và phân trang.
- Kết quả mới: build 0 warning, 0 error; 295/295 kiểm tra HTTP trên database chính; 336/336 kiểm tra HTTP trên database dùng một lần; 7/7 kiểm chứng ACID. Bài kiểm thử đồng thời xác nhận chỉ một trong hai đơn cạnh tranh được hoàn tất, tồn kho không âm.

Phần hồi quy Tuần 2 đã được chạy sớm. Ngày tiếp theo có thể dành cho rà soát nghiệp vụ, demo với mentor và bắt đầu API báo cáo bán hàng.
