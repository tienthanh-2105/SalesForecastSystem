# BÁO CÁO CÔNG VIỆC HẰNG NGÀY

**Ngày thực hiện:** Thứ Tư, ngày 23/09/2026

**Dự án:** Sales Forecast System

## 1. Mục tiêu trong ngày

- Chuyển kế hoạch báo cáo bán hàng dự kiến ngày tiếp theo sang thực hiện trong ngày 23/09/2026.
- Xây dựng API tổng hợp doanh số và dữ liệu cho biểu đồ.
- Bổ sung báo cáo tồn kho và sản phẩm dưới ngưỡng.
- Kiểm thử số liệu, phân quyền, validation và hồi quy toàn hệ thống.

## 2. Công việc đã thực hiện

### 2.1. API báo cáo bán hàng

Đã triển khai sáu API:

- `GET /api/reports/sales-summary`: tổng số đơn, số lượng bán và doanh thu.
- `GET /api/reports/sales-trend`: doanh số theo ngày, tuần hoặc tháng.
- `GET /api/reports/top-products`: sản phẩm bán tốt theo doanh thu.
- `GET /api/reports/top-categories`: danh mục bán tốt theo doanh thu.
- `GET /api/reports/sales-by-staff`: hiệu suất bán hàng theo nhân viên.
- `GET /api/reports/inventory`: tổng tồn và sản phẩm dưới ngưỡng.

Báo cáo hỗ trợ lọc theo khoảng ngày, kho, sản phẩm, danh mục và nhân viên. API xếp hạng hỗ trợ giới hạn từ 1 đến 100 kết quả. Dữ liệu xu hướng có thể nhóm theo `Day`, `Week` hoặc `Month` để dùng trực tiếp cho biểu đồ.

### 2.2. Quy tắc dữ liệu và phân quyền

- Chỉ đơn hàng có trạng thái `Completed` được tính vào doanh số.
- Đơn nháp, đơn đang xử lý, đơn hủy và đơn hoàn tất thất bại không làm thay đổi báo cáo.
- Khoảng ngày báo cáo được giới hạn tối đa 366 ngày; ngày bắt đầu sau ngày kết thúc trả `400`.
- Mã lọc phải là số dương; `top` và chu kỳ không hợp lệ trả `400`.
- Cả ba vai trò `Admin`, `WarehouseManager` và `SalesStaff` được xem báo cáo; người chưa đăng nhập nhận `401`.

### 2.3. Kiểm thử

- Build toàn bộ solution thành công với 0 warning, 0 error.
- `311/311` kiểm tra HTTP đạt trên database chính.
- `358/358` kiểm tra HTTP đạt trên database dùng một lần, gồm luồng nhập 10 sản phẩm, bán 4 sản phẩm với doanh thu 380, đối chiếu tồn 6 và các báo cáo liên quan.
- Kiểm tra đồng thời xác nhận chỉ một trong hai đơn cạnh tranh được hoàn tất và tồn kho không âm.
- `7/7` kiểm chứng ACID của database đạt; schema version 8 hoạt động ổn định.
- Trong quá trình kiểm thử đã phát hiện và sửa lỗi EF Core không dịch được truy vấn xu hướng doanh số, tránh response `500` khi gọi API.

## 3. Kết quả

Kế hoạch báo cáo bán hàng của ngày tiếp theo đã được chuyển sang và hoàn thành trong ngày 23/09/2026. Backend hiện cung cấp đủ dữ liệu cho thẻ tổng quan, biểu đồ đường doanh số, biểu đồ xếp hạng sản phẩm và danh mục, hiệu suất nhân viên và cảnh báo tồn kho.

## 4. Kế hoạch tiếp theo

- Chuẩn bị dữ liệu bán hàng theo chuỗi thời gian cho dự báo.
- Xây dựng mô hình baseline và chia tập huấn luyện, kiểm định theo thời gian.
- Tính MAE, RMSE và lưu kết quả chạy dự báo.
- Tiếp tục duy trì hồi quy báo cáo khi bổ sung dữ liệu dự báo.
