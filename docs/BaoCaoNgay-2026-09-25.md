# BÁO CÁO CÔNG VIỆC HẰNG NGÀY

**Ngày thực hiện:** Thứ Sáu, ngày 25/09/2026

**Dự án:** Sales Forecast System

## 1. Mục tiêu trong ngày

- Xây dựng module dự báo thật trong `SalesForecastSystem.ML`.
- Triển khai baseline bằng moving average và seasonal naive.
- Đánh giá mô hình bằng walk-forward validation theo đúng thứ tự thời gian.
- Tính MAE, RMSE và chọn mô hình có sai số thấp nhất.
- Tạo kết quả dự báo không âm, có khoảng dự báo và metadata để backend lưu ở bước tiếp theo.

## 2. Công việc đã thực hiện

### 2.1. Hoàn thiện cấu trúc SalesForecastSystem.ML

Đã thay phần solution folder ML rỗng bằng module Python vật lý gồm entry point, hợp đồng dữ liệu, mô hình, pipeline huấn luyện, metric và unit test. Module không truy cập SQL Server trực tiếp; dữ liệu vào và kết quả ra sử dụng JSON để giữ ranh giới rõ ràng với backend ASP.NET Core.

### 2.2. Mô hình baseline

- Moving average hỗ trợ các cửa sổ mặc định 3, 7 và 14 ngày.
- Seasonal naive hỗ trợ chu kỳ mặc định 7 ngày.
- Các tham số có thể thay đổi trong request mà không sửa mã nguồn.
- Dự báo được chặn tại 0 để không sinh số lượng âm.
- Mỗi kết quả chứa tên mô hình, phiên bản và tham số đã sử dụng.

### 2.3. Đánh giá MAE và RMSE

Pipeline sử dụng walk-forward validation. Tại mỗi ngày thuộc tập `Validation`, mô hình chỉ được dùng các quan sát có ngày trước đó; dữ liệu tương lai không được đưa ngược vào lịch sử. MAE đo sai số tuyệt đối trung bình, còn RMSE tăng mức phạt với sai số lớn. Mô hình được chọn theo RMSE thấp nhất, sau đó dùng MAE làm tiêu chí phụ.

### 2.4. Xử lý chất lượng dữ liệu

- Chỉ xem `Sold` và `NoSale` là quan sát nhu cầu hợp lệ.
- Loại `StockOut` vì lượng bán bằng 0 khi hết hàng chưa chắc là nhu cầu bằng 0.
- Loại `MissingInventoryHistory` vì không đủ căn cứ xác định trạng thái bán hàng.
- Kiểm tra chuỗi ngày liên tục, thứ tự Training trước Validation và số lượng không âm.
- Từ chối request không có điểm huấn luyện hoặc kiểm định hợp lệ.

### 2.5. Kết quả đầu ra

Kết quả JSON chứa mã sản phẩm, mã kho, mô hình được chọn, phiên bản, tham số, khoảng huấn luyện, khoảng kiểm định, khoảng dự báo, MAE, RMSE, số điểm được đánh giá, số điểm bị loại và danh sách dự báo. Mỗi ngày dự báo có số lượng, cận dưới và cận trên. Cấu trúc này sẵn sàng để API lưu vào `ForecastModels`, `ForecastRuns` và `ForecastResults`.

### 2.6. Kiểm thử

- `6/6` unit test ML đạt: công thức MAE/RMSE, validation đầu vào, chọn mô hình, dự báo không âm và loại ngày stock-out.
- Build solution thành công với 0 warning, 0 error.
- `322/322` kiểm tra HTTP đạt trên database chính.
- `370/370` kiểm tra HTTP đạt trên database dùng một lần.
- `7/7` kiểm chứng ACID đạt; schema database giữ nguyên version 8.

## 3. Kết quả

Module ML baseline đã hoàn thành và có thể chạy độc lập. Hệ thống đã có quy trình từ chuỗi dữ liệu Training/Validation đến so sánh mô hình, tính MAE/RMSE, chọn mô hình tốt nhất và sinh dự báo tương lai. Kết quả đã được chuẩn hóa để tích hợp với API và các bảng dự báo mà không đưa trách nhiệm truy cập database vào module ML.

## 4. Kế hoạch tiếp theo

- Xây dựng API tạo lần chạy dự báo.
- Gọi module ML từ backend và lưu model, lần chạy, MAE/RMSE cùng kết quả dự báo bằng transaction.
- Cung cấp API xem trạng thái và kết quả theo sản phẩm, kho và khoảng ngày.
- Ngăn tạo hai lần chạy trùng phạm vi và ghi trạng thái `Failed` khi module ML trả lỗi.
