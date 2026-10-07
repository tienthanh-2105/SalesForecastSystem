# API báo cáo lợi nhuận và nhóm nhân viên

Ngày cập nhật: 06/10/2026. Cả hai endpoint yêu cầu Bearer JWT và cho phép Admin, WarehouseManager, SalesStaff xem báo cáo như các API báo cáo hiện có. Chỉ tính đơn Completed; doanh thu đã trừ giảm giá dòng hàng.

## Lợi nhuận gộp

`GET /api/reports/profit?FromDate=2026-10-01&ToDate=2026-10-31&Period=Day`

Nhận cùng bộ lọc ngày, kho, sản phẩm, danh mục và nhân viên với báo cáo doanh số. Period nhận Day, Week hoặc Month. Khoảng ngày bao gồm hai đầu, tối đa 366 ngày khi truyền cả FromDate và ToDate. Tuần bắt đầu thứ Hai. Kết quả gồm CostMethod, Totals và Periods để frontend vẽ biểu đồ.

- Revenue: doanh thu của các dòng đơn hoàn tất trong bộ lọc.
- CostOfGoodsSold: giá vốn theo FIFO riêng từng kho và sản phẩm, lấy giá nhập từ chi tiết phiếu nhập đã ghi vào sổ giao dịch kho. Thứ tự FIFO là InventoryTransactionId, tức thứ tự ghi nhận vào sổ; không dùng ngày đặt đơn để đảo thứ tự lô nhập.
- GrossProfit = Revenue - CostOfGoodsSold. Đây là lợi nhuận gộp hàng bán, chưa trừ chi phí vận hành, thuế hoặc các khoản chi ngoài dữ liệu đơn hàng.
- MissingCostQuantity: số lượng bán chưa xác định được giá vốn. Nếu thiếu giao dịch xuất, nguồn nhập không có giá hoặc thiếu số lượng nguồn, CostOfGoodsSold và GrossProfit của tổng/kỳ liên quan trả null; không coi hàng thiếu giá là giá vốn 0.

Ví dụ nhập 20 cái giá 100, bán 2 cái thì giá vốn 200. Nhập thêm 5 cái giá 200, bán tiếp 19 cái thì FIFO dùng 18 cái giá 100 và 1 cái giá 200, giá vốn 2000. Lô nhập sau không thay đổi giá vốn lần bán trước. Giá vốn được làm lại từ lịch sử tất cả giao dịch liên quan trước lần xuất, kể cả ngoài khoảng ngày báo cáo; đơn chưa Completed cũng có thể đã xuất kho nên vẫn được tiêu thụ trong FIFO để các lần bán sau có giá vốn đúng.

API đọc dữ liệu nhất quán trong transaction Serializable và không ghi database. Chưa lưu giá vốn vào từng dòng bán; khi lịch sử nhập/xuất được sửa trực tiếp ngoài ứng dụng, giá vốn báo cáo có thể thay đổi. Dữ liệu lớn cần tối ưu bằng lưu giá vốn hoặc checkpoint sổ kho thay vì tính lại toàn bộ lịch sử mỗi lần.

## Doanh số theo nhóm nhân viên

`GET /api/reports/sales-by-group?GroupName=NhomA&StaffUserIds=1&StaffUserIds=2`

Nhóm trong API này là tập nhân viên do người dùng chọn cho mỗi lần báo cáo, không phải vai trò phân quyền hay phòng ban lưu trong database. Chưa thêm chức năng quản lý nhóm cố định. Truyền StaffUserIds lặp lại để chọn nhiều thành viên; tối đa 100 ID, ID phải dương và tồn tại. ID trùng được loại bỏ. Không truyền đồng thời StaffUserId đơn lẻ. GroupName mặc định là Nhóm bán hàng, tối đa 100 ký tự.

Nhận bộ lọc FromDate, ToDate, WarehouseId, ProductId, CategoryId. Kết quả gồm GroupName, StaffUserIds, tổng OrderCount/QuantitySold/Revenue và Members với thống kê từng người. Một đơn nhiều chi tiết chỉ được đếm một lần. Nhân viên không có doanh số vẫn xuất hiện với số liệu 0; tài khoản hiện bị khóa vẫn có thể xem doanh số lịch sử. Nhân viên là người tạo đơn, cùng quy tắc của sales-by-staff.

Trong Swagger, chọn Try it out, nhập các ID vào mảng StaffUserIds, rồi Execute. ID không tồn tại hoặc bộ lọc sai trả 400; chưa đăng nhập trả 401.

## Kiểm thử

Chạy `powershell -ExecutionPolicy Bypass -File tests/RunDisposableIntegration.ps1 -ExcelOnly`. Bộ kiểm thử HTTP/SQL sử dụng database tạm, kiểm tra FIFO qua nhiều lô, lô nhập sau lần bán, giảm giá, bán lỗ, ngày/tuần/tháng, thiếu giá vốn, nhóm có nhiều thành viên, ID trùng/sai và thành viên không có doanh số, bên cạnh kiểm thử Excel hiện có.

## Frontend ngày 07/10/2026

Mở mục Báo cáo hoặc đường dẫn `/reports` sau khi đăng nhập. Cả ba vai trò được truy cập theo quyền API hiện tại. Chọn ngày, chu kỳ và các mã kho/sản phẩm/danh mục/nhân viên để xem lợi nhuận gộp. Kết quả gồm doanh thu, giá vốn, lợi nhuận, số đơn, số lượng và bảng theo kỳ. Thiếu giá vốn hiển thị cảnh báo và Chưa xác định.

Báo cáo nhóm dùng tên nhóm và danh sách mã nhân viên cách nhau bằng dấu phẩy; mã lặp được loại bỏ. Bộ lọc nhân viên đơn lẻ chỉ áp dụng cho lợi nhuận. Mục tra cứu lấy tối đa 100 nhân viên từ báo cáo sales-by-staff; vẫn có thể nhập mã nhân viên không xuất hiện trong danh sách này. Nhóm chỉ dùng cho mỗi lần báo cáo.

Kiểm thử frontend sử dụng API mock, không tạo dữ liệu nghiệp vụ: unit test kiểm tra preview/import, JWT, chống gửi trùng, lỗi dòng Excel, giá vốn null và query nhóm; Playwright kiểm tra tải file, bộ lọc xuất và truy cập báo cáo trên mobile bằng SalesStaff. Kiểm thử SQL/HTTP thực tế tiếp tục dùng hướng dẫn phía trên.
