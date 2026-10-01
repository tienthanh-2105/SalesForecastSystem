# BÁO CÁO CÔNG VIỆC HẰNG NGÀY

**Ngày thực hiện:** Thứ Năm, ngày 24/09/2026

**Dự án:** Sales Forecast System

## 1. Mục tiêu trong ngày

- Chuẩn bị dữ liệu đầu vào cho chức năng dự báo nhu cầu.
- Tổng hợp số lượng bán theo ngày từ các đơn đã hoàn tất.
- Tạo chuỗi thời gian liên tục và bổ sung ngày không bán bằng 0.
- Phân loại trạng thái dữ liệu theo lịch sử tồn kho.
- Chia tập huấn luyện và kiểm định theo thứ tự thời gian.

## 2. Công việc đã thực hiện

### 2.1. API dữ liệu dự báo theo ngày

Đã triển khai `GET /api/forecast-data/daily`. API nhận sản phẩm, kho, khoảng ngày và tỷ lệ kiểm định; trả thông tin sản phẩm, ngày bắt đầu tập kiểm định, tổng số lượng bán cùng danh sách điểm dữ liệu liên tục.

### 2.2. Tổng hợp và chuẩn hóa dữ liệu

- Chỉ lấy chi tiết đơn có trạng thái `Completed`.
- Tổng hợp số lượng bán theo sản phẩm, kho và ngày đặt hàng.
- Điền `quantitySold = 0` cho ngày không có giao dịch bán.
- Tính tồn kho cuối ngày từ sổ `InventoryTransactions`.
- Phân loại từng ngày thành `Sold`, `NoSale`, `StockOut` hoặc `MissingInventoryHistory`.
- Giới hạn khoảng dữ liệu từ 2 đến 1095 ngày.

### 2.3. Chia tập dữ liệu

Chuỗi được chia theo thứ tự thời gian. Các điểm cũ thuộc tập `Training`; phần cuối chuỗi thuộc tập `Validation`. Tỷ lệ kiểm định nhận từ 10 đến 50 phần trăm và mặc định là 20 phần trăm. Cách chia này ngăn dữ liệu tương lai xuất hiện trong tập huấn luyện.

### 2.4. Kiểm thử

- Build solution thành công với 0 warning, 0 error.
- `322/322` kiểm tra HTTP đạt trên database chính.
- `370/370` kiểm tra HTTP đạt trên database dùng một lần.
- Đã kiểm tra quyền của ba vai trò, tham số thiếu hoặc sai, sản phẩm và kho không tồn tại.
- Đã đối chiếu chuỗi 5 ngày với giao dịch nhập 10, bán 4, tồn cuối ngày 6 và tổng số lượng bán 4.
- Đã xác nhận đơn hủy và đơn hoàn tất thất bại không được đưa vào dữ liệu dự báo.
- `7/7` kiểm chứng ACID đạt; schema database giữ nguyên version 8.

## 3. Kết quả

Phần chuẩn bị dữ liệu dự báo đã hoàn thành. Backend hiện cung cấp chuỗi dữ liệu sạch, liên tục và được chia theo thời gian để dùng cho mô hình baseline và mô hình Machine Learning ở bước tiếp theo.

## 4. Kế hoạch tiếp theo

- Xây dựng mô hình baseline bằng trung bình trượt hoặc seasonal naive.
- Cài đặt thư viện ML.NET cho mô hình dự báo chính.
- Tính MAE và RMSE trên tập kiểm định.
- Lưu phiên bản, tham số và kết quả chạy mô hình.
