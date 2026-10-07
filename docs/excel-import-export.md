# Mẫu Excel sản phẩm và đơn hàng

Cập nhật frontend ngày 07/10/2026 cho API ngày 05/10/2026.

## Sử dụng giao diện

Trang Sản phẩm và Đơn hàng có Nhập Excel, Xuất Excel và Tải file mẫu. File mẫu và file xuất được tải trực tiếp từ backend, kèm JWT. Xuất áp dụng bộ lọc hiện tại cho toàn bộ danh sách.

Chọn file .xlsx tối đa 5 MB và 2000 dòng, nhấn Kiểm tra file để gọi API import/preview. Giao diện hiển thị số dòng hợp lệ và lỗi theo dòng/cột. Nhập vào hệ thống chỉ bật khi toàn bộ file hợp lệ. API import kiểm tra lại và lưu nguyên tử; lỗi hoặc dữ liệu trùng không ghi một phần file. Đổi file hoặc kiểm tra lại vô hiệu hóa kết quả cũ. Sau khi lưu thành công, danh sách tự tải lại.

## Mẫu sản phẩm

Sheet `SanPham`, tiêu đề dòng 1, dữ liệu từ dòng 2. Các cột có thể đổi thứ tự nhưng phải giữ đúng tên; cột bắt buộc không được thiếu. Những cột ngoài mẫu bị từ chối.

| Cột | Bắt buộc | Quy định |
| --- | --- | --- |
| SKU | Có | Văn bản ASCII tối đa 50 ký tự, duy nhất trong file và hệ thống |
| Tên sản phẩm | Có | Tối đa 200 ký tự |
| Mã danh mục | Có | Mã danh mục đang hoạt động, không có danh mục con |
| Đơn vị | Có | Tối đa 30 ký tự |
| Giá bán | Có | Số không âm, tối đa 2 chữ số thập phân |
| Tồn kho tối thiểu | Không | Số nguyên từ 0 đến 2147483647, mặc định 0; ngưỡng cảnh báo |
| Đang kinh doanh | Không | TRUE/FALSE, mặc định TRUE |
| Mô tả | Không | Tối đa 1000 ký tự |

Không có cột tồn kho hiện tại, ProductId, RowVersion hoặc mã hệ thống. SKU được đối chiếu sau khi bỏ khoảng trắng hai đầu và chuyển chữ hoa. Mọi dòng có cùng SKU bị đánh dấu trùng; SKU đã có không được tự chuyển thành cập nhật.

Xuất sản phẩm đọc chi tiết để giữ mô tả mà API danh sách không trả về. Mã danh mục ánh xạ từ CategoryId sang code hiện tại.

## Mẫu đơn hàng

Sheet `DonHang`, tiêu đề dòng 1. Mỗi dòng là một sản phẩm; các dòng cùng Mã đơn hàng được gom thành một đơn.

| Cột | Bắt buộc | Quy định |
| --- | --- | --- |
| Mã đơn hàng | Có | Văn bản tối đa 50 ký tự; duy nhất trong hệ thống |
| Ngày đặt | Có | YYYY-MM-DD hoặc ngày Excel, từ 1753-01-01 |
| Mã kho | Có | KHO theo ID hiện tại, kho phải đang hoạt động |
| Tên khách hàng | Có | Tối đa 100 ký tự |
| Số điện thoại | Có | Văn bản gồm 8 đến 15 chữ số |
| Địa chỉ giao hàng | Có | Tối đa 255 ký tự |
| SKU | Có | Sản phẩm đang kinh doanh, không lặp trong cùng đơn |
| Số lượng | Có | Số nguyên dương, tối đa 2147483647 |
| Đơn giá | Có | Số không âm, tối đa 2 chữ số thập phân |
| Giảm giá (tiền) | Không | Số tiền giảm của toàn dòng, mặc định 0, không vượt số lượng nhân đơn giá |
| Trạng thái | Không | Chỉ Draft cho đơn mới, mặc định Draft |
| Ghi chú | Không | Tối đa 500 ký tự |

Ngày, kho, khách hàng, điện thoại, địa chỉ, trạng thái và ghi chú phải nhất quán ở mọi dòng của một đơn. Lặp Mã đơn hàng với các SKU khác nhau hợp lệ; lặp cùng Mã đơn hàng và SKU là lỗi ở tất cả dòng liên quan.

Giảm giá trong Excel dùng số tiền đúng với `SalesItemRequest.Discount`, không phải phần trăm như biểu mẫu tạo đơn. Nhờ vậy file xuất giữ số tiền giảm chính xác, không đổi qua phần trăm rồi làm tròn. Không nhập số có dấu phân cách hàng nghìn; không dùng định dạng Percentage.

File xuất giữ trạng thái thực tế, kể cả Completed/Cancelled. Khi đọc lại, mã đơn đã có và trạng thái khác Draft bị đánh dấu lỗi để không tạo bản trùng hoặc trừ tồn lại. Mã SKU được ánh xạ từ ProductId; chi tiết của mọi đơn được đọc từ API với tối đa 4 request đồng thời. Đơn không có chi tiết báo lỗi thay vì bị bỏ khỏi file.

Các giá tiền nhập giới hạn đến 9999999999999.99 để nằm trong phạm vi biểu diễn chính xác theo cent của JavaScript. Database hỗ trợ phạm vi decimal lớn hơn; cần xử lý số thập phân bằng backend nếu mở rộng giới hạn nhập.

## API đã triển khai

Các endpoint yêu cầu JWT Bearer. Upload dùng `multipart/form-data`, trường `file`, file `.xlsx` tối đa 5 MB và 2000 dòng. GET export nhận các bộ lọc của API danh sách tương ứng, xuất tất cả trang; không dùng Page/PageSize để xuất một phần.

| Method | Endpoint | Chức năng | Quyền |
| --- | --- | --- | --- |
| GET | `/api/products/template` | Tải mẫu sản phẩm | Các vai trò hiện có |
| GET | `/api/products/export` | Xuất sản phẩm | Các vai trò hiện có |
| POST | `/api/products/import/preview` | Kiểm tra, không lưu | Admin, WarehouseManager |
| POST | `/api/products/import` | Import sản phẩm | Admin, WarehouseManager |
| GET | `/api/sales/template` | Tải mẫu đơn hàng | Admin, SalesStaff |
| GET | `/api/sales/export` | Xuất đơn hàng | Admin, SalesStaff |
| POST | `/api/sales/import/preview` | Kiểm tra, không lưu | Admin, SalesStaff |
| POST | `/api/sales/import` | Import đơn hàng | Admin, SalesStaff |

Response import/preview gồm `totalRows`, `validRows`, `isValid`, `errors` (row, column, message), `createdCount`, `createdIds`, `conflict`. Row là số dòng Excel; row 0 là lỗi toàn file. Preview trả 200 với kết quả hợp lệ hoặc lỗi nội dung. Import trả 200 khi lưu thành công, 400 khi nội dung sai, 409 khi xung đột database; 401/403 cho xác thực/phân quyền, 413 khi quá dung lượng. File sai định dạng/đuôi hoặc thiếu upload bị từ chối 400.

Backend kiểm tra độc lập quyền, định dạng, mã trùng và dữ liệu tham chiếu trước khi lưu; không tin kết quả phía trình duyệt hoặc kết quả preview trước đó. Import chạy trong transaction Serializable, một dòng lỗi khiến cả file không được lưu.

- Tạo sản phẩm theo SKU, ánh xạ mã danh mục sang CategoryId, không thay đổi tồn kho.
- Gom chi tiết theo Mã đơn hàng, ánh xạ kho và SKU sang ID, xử lý khách hàng theo quy tắc nghiệp vụ đã thống nhất.
- Khách hàng được đối chiếu theo số điện thoại. Nếu có đúng một khách hàng hoạt động và tên khớp thì dùng lại; chưa có thì tạo trong cùng transaction. Số điện thoại có nhiều khách hàng, khác tên hoặc khách hàng ngừng hoạt động bị từ chối. Mỗi chi tiết phải đủ tồn kho hiện tại như thao tác thêm chi tiết vào đơn Nháp.
- Tạo đầu đơn và tất cả chi tiết trong một transaction; đơn mới ở Draft, không gọi hoàn thành hoặc xuất kho trong import.
- Chặn import lại bằng mã duy nhất trên database. Trả lỗi có số dòng Excel, cột và nội dung lỗi; không tự bỏ qua bản trùng.
- Nếu mở rộng nhập lịch sử, cần luồng riêng được thống nhất về doanh thu và tồn kho; không bật ghi nhận trạng thái Completed từ mẫu hiện tại.

Giới hạn đối chiếu ở giao diện hiện tại là 2000 sản phẩm/đơn/kho mỗi danh sách. Nếu dữ liệu lớn hơn, cần API kiểm tra theo các mã có trong file thay vì tải toàn bộ danh sách. File XLSX được xử lý bằng ExcelJS, tải thư viện theo yêu cầu để không thêm phần Excel vào tải trang ban đầu.

Backend dùng ClosedXML và đối chiếu trực tiếp các mã trong file với database. File mẫu API có SanPham/DonHang trống, ThamChieu và HuongDan. File xuất giữ dữ liệu văn bản, số tiền và trạng thái thực tế; import không cập nhật/ghi đè bản ghi đã có.

Kiểm thử HTTP và SQL trên database tạm (tự dọn sau khi chạy): `powershell -ExecutionPolicy Bypass -File tests/RunDisposableIntegration.ps1 -ExcelOnly`. Đã vượt qua 79 kiểm tra: quyền, file mẫu/xuất, preview không ghi, import sản phẩm/đơn, dữ liệu trùng/sai, giới hạn file, giữ số 0 đầu điện thoại, không đổi tồn kho, rollback cả khách hàng/đầu đơn/chi tiết khi database lỗi và hai request import đồng thời.
