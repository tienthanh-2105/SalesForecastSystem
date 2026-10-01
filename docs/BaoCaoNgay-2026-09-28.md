# BÁO CÁO CÔNG VIỆC HẰNG NGÀY

**Ngày thực hiện:** Thứ Hai, ngày 28/09/2026

**Dự án:** Sales Forecast System

## 1. Mục tiêu trong ngày

- Hoàn thiện giao diện quản trị cho chức năng quản lý danh mục và sản phẩm.
- Cải thiện cách bố trí thông tin người dùng, thanh công cụ và bộ lọc dữ liệu.
- Bổ sung khả năng chọn và tải ảnh sản phẩm trực tiếp từ thiết bị.
- Kiểm tra tính tương thích giữa giao diện frontend và API backend.

## 2. Công việc đã thực hiện

### 2.1. Điều chỉnh giao diện trang quản lý danh mục

- Bổ sung bộ lọc trạng thái dạng danh sách lựa chọn gồm `Tất cả trạng thái`, `Đang hoạt động` và `Ngừng hoạt động`.
- Kết hợp điều kiện lọc trạng thái với chức năng tìm kiếm theo mã, tên và mô tả danh mục.
- Chuyển nhãn vai trò `Quản trị viên` từ sidebar lên topbar và đặt cạnh địa chỉ email.
- Tăng kích thước chữ email và nhãn vai trò để thông tin người dùng dễ quan sát hơn.
- Chuẩn hóa tiêu đề topbar thành `TRANG QUẢN TRỊ - QUẢN LÝ DANH MỤC`.
- Đồng bộ cách đặt tiêu đề khi chuyển sang trang quản lý sản phẩm và người dùng.

### 2.2. Sắp xếp lại thanh công cụ

- Chuyển nút `Làm mới` và `Thêm danh mục` xuống cùng hàng với ô tìm kiếm và bộ lọc trạng thái.
- Giữ các nút thao tác ở phía bên phải trên màn hình lớn.
- Điều chỉnh responsive để ô tìm kiếm, bộ lọc và các nút tự xuống hàng khi không gian hiển thị bị thu hẹp.

### 2.3. Cải tiến biểu mẫu thêm và sửa sản phẩm

- Mở rộng chiều rộng biểu mẫu sản phẩm từ 760 px lên tối đa 1040 px.
- Tăng vùng xem trước ảnh lên 220 px để kiểm tra ảnh rõ hơn trước khi lưu.
- Thay trường nhập URL ảnh bằng trường chọn tệp từ thiết bị.
- Hỗ trợ các định dạng JPG, PNG và WebP với dung lượng tối đa 5 MB.
- Hiển thị ảnh xem trước ngay sau khi người dùng chọn tệp.
- Khi chỉnh sửa sản phẩm, tiếp tục hiển thị ảnh hiện có nếu người dùng không chọn ảnh mới.

### 2.4. Tích hợp chức năng tải ảnh với API

- Bổ sung API `POST /api/products/images` để nhận ảnh theo định dạng `multipart/form-data`.
- Kiểm tra phần mở rộng, MIME type, dung lượng và chữ ký tệp nhằm hạn chế tệp giả mạo hoặc không hợp lệ.
- Đổi tên ảnh bằng mã ngẫu nhiên để tránh trùng tên và hạn chế rủi ro từ tên tệp do người dùng cung cấp.
- Lưu ảnh trong `wwwroot/uploads/products` để ứng dụng có thể phục vụ ảnh trực tiếp.
- Tự động nhận URL ảnh sau khi tải lên và đưa URL vào dữ liệu tạo hoặc cập nhật sản phẩm.
- Loại thư mục ảnh tải lên khi chạy khỏi phạm vi theo dõi của Git.

### 2.5. Khắc phục lỗi tích hợp

- Phân tích lỗi HTTP `405` khi lưu sản phẩm có ảnh.
- Xác định nguyên nhân là frontend đã được cập nhật nhưng tiến trình API vẫn sử dụng phiên bản cũ, chưa có endpoint tải ảnh.
- Khởi động lại API và xác nhận endpoint tải ảnh đã nhận đúng phương thức `POST`.
- Sửa khai báo nhận tệp để endpoint tải ảnh tương thích với Swagger và không làm trang tài liệu API trả lỗi.

### 2.6. Kiểm tra kỹ thuật

- Kiểm tra cú pháp JavaScript thành công.
- Build dự án ASP.NET Core thành công với 0 warning và 0 error.
- Xác nhận endpoint `/api/products/images` xuất hiện trong Swagger với phương thức `POST`.
- Xác nhận endpoint yêu cầu xác thực và không còn trả về lỗi `405 Method Not Allowed`.

## 3. Kết quả

Giao diện quản trị đã được cải thiện về bố cục, khả năng lọc dữ liệu và khả năng sử dụng trên nhiều kích thước màn hình. Trang danh mục có thể lọc nhanh theo trạng thái; thông tin tài khoản được bố trí gọn trên topbar; các nút thao tác nằm cùng thanh với ô tìm kiếm. Biểu mẫu sản phẩm rộng và dễ sử dụng hơn, đồng thời cho phép chọn ảnh trực tiếp từ thiết bị thay vì yêu cầu nhập URL thủ công.

Frontend và backend đã được tích hợp cho quy trình tải ảnh, kiểm tra ảnh, lưu tệp và gắn URL ảnh vào sản phẩm. Lỗi `405` phát sinh trong quá trình tích hợp đã được xác định và khắc phục.

## 4. Kế hoạch tiếp theo

- Kiểm thử đầy đủ thao tác thêm, sửa và ngừng kinh doanh sản phẩm trên giao diện.
- Bổ sung chức năng xóa hoặc thay thế ảnh cũ khi cập nhật sản phẩm.
- Hoàn thiện các trang quản lý khách hàng, bán hàng, nhập hàng và tồn kho.
- Chuẩn hóa thông báo lỗi tiếng Việt giữa frontend và backend.
- Kiểm tra giao diện trên máy tính bảng và điện thoại thực tế.
