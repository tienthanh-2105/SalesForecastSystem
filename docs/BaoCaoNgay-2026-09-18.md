# BÁO CÁO CÔNG VIỆC HẰNG NGÀY

**Ngày thực hiện:** Thứ Sáu, ngày 18/09/2026

**Dự án:** Sales Forecast System

## 1. Mục tiêu trong ngày

- Hoàn thiện module quản lý người dùng dành cho Admin.
- Bổ sung tìm kiếm, lọc và phân trang người dùng.
- Bảo đảm khóa tài khoản, đổi vai trò và đặt lại mật khẩu vô hiệu hóa phiên cũ.
- Viết kiểm thử E2E cho phân quyền, validation và các lỗi nghiệp vụ quan trọng.
- Chạy toàn bộ hồi quy HTTP và kiểm chứng ACID của database.

## 2. Công việc đã thực hiện

### 2.1. API quản lý người dùng

Đã bổ sung các API Admin-only:

- `GET /api/users` và `GET /api/users/{id}`.
- `POST /api/users` và `PUT /api/users/{id}`.
- `PUT /api/users/{id}/status` để khóa hoặc mở khóa.
- `POST /api/users/{id}/reset-password` để đặt lại mật khẩu.

Danh sách hỗ trợ tìm theo họ tên hoặc email, lọc theo vai trò/trạng thái và phân trang tối đa 100 bản ghi mỗi trang. Thứ tự mặc định theo họ tên và mã người dùng để kết quả ổn định.

### 2.2. Validation và bảo mật

- Chuẩn hóa email thành chữ thường, chống trùng bằng kiểm tra nghiệp vụ và unique index.
- Chỉ chấp nhận ba vai trò `Admin`, `WarehouseManager`, `SalesStaff` đang hoạt động.
- Mật khẩu tối thiểu 12 ký tự, tối đa 72 byte UTF-8 và được băm bằng BCrypt.
- Response không chứa mật khẩu hoặc password hash.
- Khóa tài khoản ngăn đăng nhập và thu hồi các phiên đang hoạt động.
- Đổi vai trò/thông tin nhận diện hoặc đặt lại mật khẩu thu hồi toàn bộ phiên cũ.
- Admin không thể tự khóa hoặc tự đổi vai trò.

### 2.3. Kiểm thử

- Bổ sung 42 kiểm tra HTTP cho module người dùng.
- Toàn bộ `202/202` kiểm tra HTTP đạt, bao gồm 160 kiểm tra hồi quy trước đó.
- `7/7` kiểm chứng ACID đạt: tính nguyên tử, chống bán âm kho, bất biến chứng từ, ràng buộc số lượng/ngưỡng tồn, sổ kho append-only và miền thời gian dự báo.
- Solution và project kiểm thử build sạch, không có warning hoặc error.
- Dữ liệu tài khoản, phiên, danh mục và sản phẩm tạm được dọn sau khi kiểm thử.

## 3. Kết quả

Kế hoạch Ngày 4 của Tuần 1 đã hoàn thành. Module quản lý người dùng đáp ứng luồng database, service, API, phân quyền, Swagger và E2E. Tiến độ MVP được cập nhật lên khoảng 41 phần trăm.

## 4. Kế hoạch tiếp theo

- Chạy hồi quy tổng thể Tuần 1 và rà soát truy vấn/chỉ mục.
- Cập nhật tài liệu, chốt bản ổn định Tuần 1.
- Sau khi baseline ổn định, chuyển sang API kho và nhà cung cấp của Tuần 2.
