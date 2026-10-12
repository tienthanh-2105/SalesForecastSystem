# Cảnh báo tồn kho — 08/10/2026

API `GET /api/reports/stock-alerts` yêu cầu JWT; Admin, WarehouseManager và SalesStaff đều được xem.

- Chỉ xét sản phẩm đang kinh doanh. Tồn kho là tổng tất cả kho từ view sổ kho; chưa có giao dịch được tính là 0.
- Cảnh báo khi tồn kho ≤ MinimumStockLevel; tồn bằng 0 là Hết hàng, kể cả ngưỡng bằng 0. Có thể chỉnh ngưỡng trong biểu mẫu sản phẩm.
- Bán chạy là top 10 sản phẩm đang kinh doanh theo số lượng từ đơn Completed trong 30 ngày lịch gồm hôm nay, theo ngày đặt đơn. Ngày tính theo giờ Việt Nam. Đồng hạng ưu tiên ProductId nhỏ hơn; không có doanh số không được gắn nhãn bán chạy.
- Ưu tiên hiển thị bán chạy, sau đó hết hàng, số lượng đã bán và ID. QuantitySold vẫn trả số lượng bán cho sản phẩm ngoài top 10.
- API đọc tồn và doanh số trong cùng transaction Serializable, không ghi dữ liệu.
- Giao diện hiển thị cảnh báo trên các trang sau đăng nhập, tự kiểm tra mỗi 60 giây khi tab đang hoạt động và khi quay lại cửa sổ. Có nút kiểm tra thủ công, thời điểm kiểm tra và bảng chi tiết thu gọn.
- Khi tồn vượt ngưỡng hoặc sản phẩm ngừng kinh doanh, cảnh báo biến mất ở lần kiểm tra tiếp theo. Lỗi tải có thông báo riêng, dữ liệu cũ được ghi rõ.

Phạm vi hiện tại: thông báo trong ứng dụng đang mở, tổng tồn toàn hệ thống; chưa gửi email/thông báo khi ứng dụng đóng và chưa cảnh báo riêng từng kho. Không dùng mô hình dự báo để quyết định cảnh báo này.

Kiểm tra: bộ HTTP/SQL trên database dùng một lần kiểm tra cả ba vai trò, 401, tồn bằng ngưỡng, tổng nhiều kho, bán chạy, xóa cảnh báo khi vượt ngưỡng, sản phẩm chưa nhập kho và sản phẩm ngừng kinh doanh. Frontend có kiểm thử hiển thị, cập nhật và lỗi tải.
