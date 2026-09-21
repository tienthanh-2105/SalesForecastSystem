# Kế hoạch triển khai Sales Forecast System trong 4 tuần

## 1. Mục tiêu

Hoàn thiện phiên bản MVP của Hệ thống Dự báo Nhu cầu Bán hàng trong 4 tuần, bao gồm:

- Đăng nhập, quản lý phiên và phân quyền.
- Quản lý người dùng, danh mục, sản phẩm, kho và đối tác.
- Nhập hàng, đơn hàng và tồn kho.
- Báo cáo bán hàng và biểu đồ.
- Dự báo nhu cầu, cảnh báo tồn kho và gợi ý nhập hàng.
- Import và export Excel.
- Giao diện quản trị responsive kết nối qua REST API.
- Kiểm thử E2E được thực hiện liên tục trong suốt quá trình phát triển.

Kế hoạch áp dụng cách triển khai theo lát cắt dọc. Mỗi chức năng phải được hoàn thiện từ database, backend, API, phân quyền đến kiểm thử E2E trước khi chuyển sang chức năng tiếp theo. Không chờ hoàn thành toàn bộ hệ thống mới bắt đầu kiểm thử.

## 2. Trạng thái hiện tại

### 2.1. Phần đã hoàn thành

- Kiến trúc ASP.NET Core 8 gồm API, Core và Infrastructure.
- Kết nối SQL Server và script triển khai database.
- Database schema version 6 gồm 17 bảng, 3 view và 2 stored procedure.
- Đăng nhập bằng email và mật khẩu BCrypt.
- JWT access token, quản lý phiên đăng nhập và đăng xuất thu hồi token.
- Kiểm tra lại trạng thái tài khoản, vai trò và phiên trên mỗi request.
- Ba vai trò: Admin, Quản lý kho và Nhân viên bán hàng.
- CRUD danh mục.
- CRUD sản phẩm, soft delete, API xem tồn kho và danh sách có tìm kiếm/lọc/sắp xếp/phân trang.
- Quản lý người dùng dành cho Admin: danh sách, tạo, sửa, khóa/mở khóa và đặt lại mật khẩu.
- Swagger có cấu hình Bearer authentication.
- Bộ IntegrationChecks với 236 trường hợp HTTP đang đạt.
- Hướng dẫn cài đặt, chạy database, API và kiểm thử.

### 2.2. Phần đã có nền tảng database nhưng chưa có API hoàn chỉnh

- Kho và nhà cung cấp.
- Khách hàng.
- Phiếu nhập và chi tiết phiếu nhập.
- Đơn hàng và chi tiết đơn hàng.
- Giao dịch kho.
- Mô hình dự báo, lần chạy dự báo và kết quả dự báo.
- View tồn kho, doanh số theo ngày và tổng tiền đơn hàng.
- Stored procedure xác nhận nhập hàng và hoàn tất đơn hàng.

### 2.3. Phần chưa triển khai

- API kho, khách hàng, nhà cung cấp, nhập hàng và đơn hàng.
- Báo cáo, dashboard và biểu đồ.
- Thuật toán và lịch chạy dự báo.
- Cảnh báo tồn kho và gợi ý nhập hàng.
- Import và export Excel.
- Frontend Bootstrap.

Trước khi bắt đầu Tuần 2, tiến độ được ước tính khoảng 41 phần trăm phạm vi MVP.

### 2.4. Refactor nền tảng đã hoàn thành trước kế hoạch chức năng

- Đã áp dụng service layer và dependency inversion cho xác thực, phiên đăng nhập, danh mục và sản phẩm.
- Controller chỉ xử lý HTTP; truy cập dữ liệu và nghiệp vụ nằm trong service implementation.
- Tên class, method, entity, DTO, route, bảng và cột đang hoạt động đã chuyển sang tiếng Anh.
- Database sử dụng Database First với migration SQL có version vì schema có stored procedure, trigger, view và khóa đồng thời cần được quản lý tập trung.
- Schema hiện tại là version 6 với 17 bảng tên tiếng Anh.
- Các thao tác ghi sổ dùng transaction, `XACT_ABORT`, `UPDLOCK`, `HOLDLOCK` và application lock theo kho.
- Inventory ledger là append-only, có unique index chống ghi trùng và trigger không cho tồn kho âm.
- Sau khi hoàn thiện quản lý người dùng, bộ 202 E2E và bộ kiểm chứng ACID của database đều đạt; hiện bộ E2E đã mở rộng lên 236 trường hợp.

### 2.5. Hoàn thiện sản phẩm đã hoàn thành

- Bổ sung mô tả, URL ảnh, ngưỡng tồn tối thiểu và thời điểm cập nhật.
- Admin và WarehouseManager được tạo, sửa và ngừng hoạt động sản phẩm; SalesStaff chỉ được xem.
- DELETE sản phẩm chuyển sang soft delete để giữ toàn bộ lịch sử chứng từ.
- PUT dùng `RowVersion` và trả `409` khi dữ liệu đã được cập nhật bởi request khác.
- Ngưỡng tồn không âm được bảo vệ ở cả API và database.
- Tồn kho tiếp tục chỉ được tính từ sổ giao dịch và không thể sửa qua API sản phẩm.

### 2.6. Tìm kiếm lọc và phân trang sản phẩm đã hoàn thành

- `GET /api/products` hỗ trợ tìm theo tên hoặc SKU và lọc theo danh mục, trạng thái hoạt động, mức tồn.
- Hỗ trợ sắp xếp tăng/giảm theo tên, SKU, giá, ngày tạo và tồn kho; luôn dùng `ProductId` làm khóa phụ để thứ tự ổn định.
- Phân trang mặc định 20 bản ghi, tối đa 100 và trả đầy đủ tổng bản ghi, tổng trang, trạng thái trang trước/sau.
- Tồn kho được đọc từ `vw_InventoryBalances`, tổng hợp và lọc ngay tại SQL Server bằng truy vấn chỉ đọc; không phát sinh N+1.
- Trang vượt quá tổng số trang trả `200` với danh sách rỗng; tham số ngoài miền hợp lệ trả `400`.

### 2.7. Quản lý người dùng đã hoàn thành

- Chỉ Admin được xem danh sách, chi tiết, tạo và cập nhật người dùng.
- Danh sách hỗ trợ tìm theo họ tên hoặc email, lọc theo vai trò/trạng thái và phân trang ổn định.
- Admin khóa/mở khóa tài khoản và đặt lại mật khẩu; mật khẩu được băm bằng BCrypt và không xuất hiện trong response hoặc log.
- Khóa tài khoản, đổi vai trò, đổi thông tin nhận diện hoặc đặt lại mật khẩu đều vô hiệu hóa phiên cũ phù hợp.
- Admin không thể tự khóa hoặc tự đổi vai trò để tránh làm mất quyền quản trị ngoài ý muốn.

## 3. Nguyên tắc triển khai và kiểm thử

### 3.1. Chu trình cho mỗi chức năng

Mỗi chức năng được thực hiện theo chu trình sau:

1. Chốt yêu cầu, quyền truy cập và tiêu chí chấp nhận.
2. Cập nhật database và dữ liệu mẫu nếu cần.
3. Xây dựng entity, DTO, service và API.
4. Viết hoặc mở rộng kịch bản E2E cho luồng thành công.
5. Viết kịch bản E2E cho validation, phân quyền và lỗi nghiệp vụ.
6. Chạy toàn bộ kiểm thử hồi quy hiện có.
7. Sửa lỗi cho đến khi tất cả kiểm thử đạt.
8. Cập nhật Swagger và tài liệu sử dụng.
9. Chỉ đánh dấu hoàn thành khi đáp ứng Definition of Done.

### 3.2. Chiến lược E2E liên tục

- Bộ E2E sử dụng API thật và SQL Server thật trong môi trường kiểm thử.
- Mỗi lát cắt chức năng phải có dữ liệu kiểm thử độc lập.
- Dữ liệu tạm phải được dọn sau khi chạy, kể cả khi kiểm thử thất bại.
- Mỗi lỗi được phát hiện phải có một kịch bản kiểm thử tái hiện trước khi sửa.
- Mỗi ngày chạy các kịch bản liên quan đến chức năng đang phát triển.
- Trước khi kết thúc ngày, chạy toàn bộ bộ kiểm thử hồi quy.
- Không merge hoặc đánh dấu hoàn thành khi build lỗi hay E2E thất bại.
- Tuần 4 chỉ tập trung hồi quy toàn hệ thống, hiệu năng và nghiệm thu; không phải thời điểm bắt đầu viết E2E.

### 3.3. Nhóm kiểm thử bắt buộc

- Xác thực: chưa đăng nhập, token sai, token hết hạn và token bị thu hồi.
- Phân quyền: đúng vai trò, sai vai trò, tài khoản bị khóa và vai trò bị đổi.
- Validation: thiếu trường, sai kiểu, vượt độ dài và dữ liệu ngoài miền hợp lệ.
- Nghiệp vụ: trùng dữ liệu, không tìm thấy, xung đột và thay đổi đồng thời.
- Database: transaction, rollback, khóa ngoại, chỉ mục và không ghi nhận trùng.
- Luồng chính: nhập hàng, bán hàng, tồn kho, báo cáo, dự báo và gợi ý nhập hàng.
- Hồi quy: chức năng mới không làm hỏng các API đã hoàn thành.

### 3.4. Definition of Done

Một chức năng chỉ được xem là hoàn thành khi:

- Database và API đáp ứng đúng yêu cầu nghiệp vụ.
- Phân quyền đúng ma trận vai trò.
- Response thành công và response lỗi có cấu trúc thống nhất.
- Swagger mô tả được request, response và quyền truy cập.
- Có E2E cho luồng thành công và các lỗi quan trọng.
- Toàn bộ build và kiểm thử hồi quy đạt.
- Không để lại dữ liệu kiểm thử trong database.
- Tài liệu chạy và ví dụ sử dụng đã được cập nhật.

## 4. Tuần 1 Hoàn thiện nền tảng hiện có

### Mục tiêu tuần

Ổn định xác thực, phân quyền, người dùng, danh mục và sản phẩm để làm nền cho các luồng nhập hàng và bán hàng.

### Ngày 1 Chuẩn hóa dự án và baseline E2E

#### Triển khai

- Chốt ma trận quyền của ba vai trò.
- Xóa WeatherForecast và các thành phần mẫu không thuộc dự án.
- Chuẩn hóa ProblemDetails và ValidationProblemDetails.
- Kiểm tra lại Swagger, cấu hình môi trường và secret.
- Ghi nhận bộ 133 IntegrationChecks hiện tại làm baseline.

#### E2E thực hiện ngay

- Build toàn bộ solution.
- Chạy lại 133 kiểm tra hiện có.
- Kiểm tra tất cả API nghiệp vụ yêu cầu đăng nhập mặc định.
- Kiểm tra Swagger chỉ công khai API login.

### Ngày 2 Hoàn thiện sản phẩm — đã hoàn thành

#### Triển khai

- Bổ sung mô tả sản phẩm.
- Bổ sung URL hoặc đường dẫn ảnh sản phẩm.
- Bổ sung ngưỡng tồn tối thiểu.
- Duy trì nguyên tắc không sửa tồn kho trực tiếp trên sản phẩm.
- Cho Quản lý kho thêm, sửa và ngừng hoạt động sản phẩm theo đúng đặc tả.
- Cập nhật schema bằng script có version và có thể chạy lại an toàn.

#### E2E thực hiện ngay

- Admin và Quản lý kho CRUD sản phẩm thành công.
- Nhân viên bán hàng không được sửa sản phẩm.
- Không chấp nhận SKU trùng, dữ liệu rỗng hoặc giá không hợp lệ.
- Không cho sửa tồn kho trực tiếp.
- Kiểm tra cập nhật đồng thời và sản phẩm đang được sử dụng.

### Ngày 3 Tìm kiếm lọc và phân trang — đã hoàn thành

#### Triển khai

- Tìm sản phẩm theo tên hoặc SKU.
- Lọc theo danh mục, trạng thái và mức tồn.
- Sắp xếp theo tên, giá, ngày tạo hoặc tồn kho.
- Phân trang và trả metadata tổng số bản ghi, trang hiện tại và tổng số trang.
- Giữ danh mục ở dạng danh sách hiện tại vì khối lượng nhỏ; sẽ áp dụng phân trang khi có yêu cầu dữ liệu lớn.

#### E2E thực hiện ngay

- Kiểm tra từng bộ lọc riêng lẻ và kết hợp nhiều bộ lọc.
- Kiểm tra trang đầu, trang cuối, trang rỗng và kích thước trang không hợp lệ.
- Kiểm tra thứ tự ổn định khi nhiều bản ghi có cùng giá trị sắp xếp.
- Kiểm tra dữ liệu trả về không vượt quá quyền người dùng.

### Ngày 4 Quản lý người dùng — đã hoàn thành

#### Triển khai

- Admin xem danh sách người dùng.
- Admin tạo tài khoản theo vai trò.
- Admin sửa thông tin và vai trò.
- Admin khóa hoặc mở khóa tài khoản.
- Admin đặt lại mật khẩu.
- Tìm kiếm và phân trang người dùng.

#### E2E thực hiện ngay

- Chỉ Admin được truy cập API quản lý người dùng.
- Email trùng hoặc không hợp lệ bị từ chối.
- Tài khoản bị khóa không đăng nhập được và token cũ bị vô hiệu hóa.
- Token cũ bị từ chối sau khi vai trò thay đổi.
- Mật khẩu không xuất hiện trong response hoặc log.

### Ngày 5 và 6 Hồi quy tuần 1

- Chạy toàn bộ E2E của xác thực, phân quyền, danh mục, sản phẩm và người dùng.
- Kiểm tra dữ liệu tạm được dọn sạch.
- Rà soát query và chỉ mục cho tìm kiếm, lọc và phân trang.
- Cập nhật Swagger, hướng dẫn chạy và báo cáo tiến độ.
- Chốt bản ổn định tuần 1.

### Kết quả bàn giao tuần 1

- Xác thực và phân quyền đúng đặc tả.
- Admin quản lý được tài khoản.
- Quản lý kho CRUD được danh mục và sản phẩm.
- Sản phẩm có đủ thông tin cần thiết.
- Danh sách sản phẩm có tìm kiếm, lọc, sắp xếp và phân trang.
- Toàn bộ E2E tuần 1 và các kiểm thử cũ đều đạt.

Tiến độ dự kiến cuối tuần: 40 đến 45 phần trăm.

## 5. Tuần 2 Kho nhập hàng và đơn hàng

### Mục tiêu tuần

Hoàn thành luồng giao dịch tạo ra dữ liệu bán hàng và bảo đảm tồn kho nhất quán.

### Ngày 1 Kho và nhà cung cấp — đã hoàn thành ngày 21/09/2026

#### Triển khai

- CRUD kho.
- CRUD nhà cung cấp.
- Tìm kiếm, lọc trạng thái và phân trang.
- Không xóa cứng dữ liệu đã được chứng từ sử dụng.

#### E2E thực hiện ngay

- Kiểm tra ma trận quyền Admin và Quản lý kho.
- Kiểm tra tên kho, mã số thuế và dữ liệu trùng.
- Kiểm tra khóa dữ liệu đã được sử dụng.

Kết quả: hai API `warehouses` và `suppliers` đã có CRUD với DELETE dạng ngừng hoạt động, tìm kiếm/lọc/phân trang và phân quyền. Bộ HTTP/E2E hiện đạt 236/236, kiểm chứng ACID đạt 7/7. Kho và nhà cung cấp đã được phiếu nhập tham chiếu vẫn được giữ lại khi ngừng hoạt động.

### Ngày 2 Khách hàng

#### Triển khai

- CRUD khách hàng.
- Tìm theo tên, email hoặc số điện thoại.
- Phân trang danh sách khách hàng.
- Chuẩn bị API xem lịch sử đơn hàng của khách hàng.

#### E2E thực hiện ngay

- Nhân viên bán hàng được quản lý khách hàng theo quyền đã chốt.
- Kiểm tra validation email và số điện thoại.
- Kiểm tra khách hàng không tồn tại và dữ liệu trùng.

### Ngày 3 Phiếu nhập hàng

#### Triển khai

- Tạo phiếu nhập nháp.
- Thêm, sửa và xóa dòng chi tiết.
- Xác nhận nhập hàng.
- Hủy hoặc xóa phiếu nhập chưa ghi sổ.
- Xem lịch sử nhập hàng theo kho, nhà cung cấp và thời gian.

#### E2E thực hiện ngay

- Tạo phiếu và xác nhận làm tăng tồn kho đúng số lượng.
- Gọi xác nhận hai lần không làm tăng tồn kho hai lần.
- Phiếu đã xác nhận không được sửa hoặc xóa.
- Một dòng lỗi làm toàn bộ transaction rollback.

### Ngày 4 và 5 Đơn hàng

#### Triển khai

- Tạo đơn hàng và chi tiết đơn hàng.
- Lưu thông tin khách hàng và địa chỉ giao hàng tại thời điểm đặt.
- Sửa đơn hàng khi còn ở trạng thái cho phép.
- Cập nhật trạng thái Chờ xử lý, Đang giao, Đã giao và Đã hủy.
- Hoàn tất đơn và ghi giao dịch xuất kho.
- Xem lịch sử đơn theo khách hàng, nhân viên, trạng thái và thời gian.

#### E2E thực hiện ngay

- Tạo đơn hợp lệ và tính tổng tiền chính xác.
- Đơn đã giao làm giảm tồn kho đúng một lần.
- Không bán vượt tồn kho.
- Không cho chuyển trạng thái sai thứ tự.
- Không sửa chi tiết sau khi đơn hoàn tất.
- Hủy đơn chưa hoàn tất không ảnh hưởng tồn kho.
- Hai yêu cầu bán đồng thời không làm tồn kho âm.

### Ngày 6 Hồi quy tuần 2

- Chạy E2E liên hoàn: tạo sản phẩm, nhập kho, tạo khách hàng, bán hàng và xem tồn.
- Chạy lại toàn bộ kiểm thử tuần 1.
- Kiểm tra transaction và các trường hợp retry.
- Cập nhật Swagger và tài liệu nghiệp vụ.
- Chốt bản ổn định tuần 2.

### Kết quả bàn giao tuần 2

- Có API kho, nhà cung cấp, khách hàng, nhập hàng và đơn hàng.
- Tồn kho được tính từ giao dịch đã ghi sổ.
- Không ghi nhận nhập hoặc bán trùng.
- Không cho phép tồn kho âm.
- Có lịch sử nhập hàng, đơn hàng và giao dịch kho.
- E2E toàn bộ luồng bán hàng đạt.

Tiến độ dự kiến cuối tuần: 65 đến 70 phần trăm.

## 6. Tuần 3 Báo cáo dự báo và gợi ý nhập hàng

### Mục tiêu tuần

Hoàn thành giá trị chính của hệ thống: báo cáo bán hàng, dự báo nhu cầu và đề xuất nhập hàng.

### Ngày 1 Báo cáo bán hàng

#### Triển khai

- Doanh số theo ngày, tuần và tháng.
- Doanh thu và số lượng bán theo khoảng thời gian.
- Top sản phẩm và top danh mục.
- Hiệu suất bán hàng theo nhân viên.
- Tổng hợp tồn kho và sản phẩm dưới ngưỡng.
- API dữ liệu cho biểu đồ đường, cột và tròn.

#### E2E thực hiện ngay

- Tạo bộ dữ liệu bán hàng có kết quả biết trước.
- So sánh kết quả API với tổng tiền và số lượng kỳ vọng.
- Kiểm tra lọc theo kho, sản phẩm, danh mục, nhân viên và thời gian.
- Kiểm tra đơn nháp hoặc đã hủy không được tính vào doanh số.

### Ngày 2 Chuẩn bị dữ liệu dự báo

#### Triển khai

- Tổng hợp số lượng bán theo ngày từ đơn đã hoàn tất.
- Tạo chuỗi thời gian liên tục.
- Điền 0 cho ngày không bán theo quy tắc đã chốt.
- Phân biệt không bán, thiếu dữ liệu và hết hàng.
- Chia tập huấn luyện và kiểm định theo thời gian.

#### E2E thực hiện ngay

- Dữ liệu nguồn không chứa đơn nháp hoặc đã hủy.
- Không để rò rỉ dữ liệu sau ngày bắt đầu dự báo.
- Ngày không bán được xử lý đúng quy tắc.
- Kết quả tổng hợp khớp với báo cáo bán hàng.

### Ngày 3 Mô hình dự báo

#### Triển khai

- Mô hình baseline bằng seasonal naive hoặc trung bình trượt.
- Mô hình chính bằng phương pháp thống kê hoặc Machine Learning phù hợp.
- Tính MAE và RMSE trên tập kiểm định.
- Lưu phiên bản, tham số, khoảng dữ liệu và kết quả mô hình.

#### E2E thực hiện ngay

- Một lần chạy hợp lệ tạo đủ kết quả trong khoảng dự báo.
- Ngày dự báo ngoài phạm vi bị từ chối.
- Số lượng dự báo không âm.
- MAE và RMSE được lưu đúng.
- Lần chạy lỗi ghi trạng thái và thông báo lỗi, không để dữ liệu nửa chừng.

### Ngày 4 API và lịch chạy dự báo

#### Triển khai

- API tạo lần chạy dự báo.
- API xem trạng thái và kết quả.
- Xem kết quả theo sản phẩm, danh mục và kho.
- Lịch chạy hàng tuần hoặc hàng tháng.
- Ngăn hai lần chạy trùng cùng phạm vi.

#### E2E thực hiện ngay

- Tạo lần chạy, chờ hoàn tất và đọc kết quả qua API.
- Kiểm tra quyền chạy và quyền xem dự báo.
- Kiểm tra retry và xử lý tiến trình lỗi.
- Kiểm tra lịch chạy không tạo job trùng.

### Ngày 5 Cảnh báo và gợi ý nhập hàng

#### Triển khai

Áp dụng công thức ban đầu:

```text
Số lượng đề xuất nhập
= Nhu cầu dự báo
+ Tồn kho an toàn
- Tồn kho hiện tại
- Số lượng đang chờ nhập
```

- Cảnh báo sản phẩm dưới ngưỡng tồn.
- Cảnh báo sản phẩm bán nhanh nhưng sắp hết.
- Trả số lượng đề xuất nhập và lý do đề xuất.
- Không đề xuất số lượng âm.

#### E2E thực hiện ngay

- Tồn kho đủ thì không cảnh báo hoặc đề xuất 0.
- Tồn kho thiếu thì số lượng đề xuất đúng công thức.
- Phiếu nhập đang chờ được trừ khỏi đề xuất.
- Thay đổi dự báo hoặc tồn kho làm kết quả được cập nhật đúng.

### Ngày 6 Hồi quy tuần 3

- Chạy E2E từ nhập hàng và bán hàng đến báo cáo và dự báo.
- Kiểm tra số liệu giữa tồn kho, doanh số và dự báo không mâu thuẫn.
- Chạy lại toàn bộ E2E tuần 1 và tuần 2.
- Cập nhật Swagger và tài liệu mô hình.
- Chốt bản ổn định tuần 3.

### Kết quả bàn giao tuần 3

- Có báo cáo bán hàng và dữ liệu biểu đồ.
- Có mô hình baseline và mô hình dự báo chính.
- Có MAE và RMSE để đánh giá.
- Có API và lịch chạy dự báo.
- Có cảnh báo tồn kho và gợi ý nhập hàng.
- E2E toàn bộ chuỗi dữ liệu nghiệp vụ đạt.

Tiến độ dự kiến cuối tuần: 85 đến 90 phần trăm.

## 7. Tuần 4 Excel frontend và hoàn thiện

### Mục tiêu tuần

Hoàn thiện khả năng sử dụng, kiểm thử hồi quy toàn hệ thống và chuẩn bị bản bàn giao.

### Ngày 1 Export Excel

#### Triển khai

- Export sản phẩm, đơn hàng, tồn kho, doanh số, dự báo và gợi ý nhập hàng.
- Định dạng tên cột, ngày, số lượng và tiền tệ.
- Áp dụng đúng bộ lọc và quyền của người xuất.

#### E2E thực hiện ngay

- Tải file qua API và kiểm tra file mở được.
- Kiểm tra tên sheet, tiêu đề cột và số dòng.
- Đối chiếu dữ liệu trong file với dữ liệu API.
- Kiểm tra người dùng không xuất được dữ liệu ngoài quyền.

### Ngày 2 Import Excel

#### Triển khai

- Import danh sách sản phẩm.
- Import đơn hàng hoặc lịch sử bán hàng theo mẫu được chốt.
- Chế độ kiểm tra trước khi lưu.
- Báo số dòng thành công, thất bại và nguyên nhân theo dòng.
- Ngăn dữ liệu trùng.

#### E2E thực hiện ngay

- File hợp lệ được import đầy đủ.
- File sai cấu trúc bị từ chối.
- Dòng lỗi được báo chính xác nhưng không làm sai dữ liệu hợp lệ theo chính sách đã chốt.
- Import lại cùng file không tạo dữ liệu trùng.
- File vượt giới hạn hoặc sai định dạng bị từ chối.

### Ngày 3 và 4 Frontend Bootstrap

#### Triển khai

- Đăng nhập và đăng xuất.
- Sidebar thay đổi theo vai trò.
- Quản lý người dùng, danh mục và sản phẩm.
- Kho, nhập hàng, khách hàng và đơn hàng.
- Dashboard, biểu đồ doanh số và dự báo.
- Cảnh báo tồn kho và gợi ý nhập hàng.
- Import và export Excel.
- Responsive cho desktop, tablet và mobile.

#### E2E thực hiện ngay

- Tự động hóa các hành trình người dùng quan trọng trên giao diện.
- Kiểm tra điều hướng và nút chức năng theo vai trò.
- Kiểm tra validation hiển thị đúng lỗi từ API.
- Kiểm tra giao diện tại các kích thước màn hình chính.
- Kiểm tra phiên hết hạn và đăng xuất đưa người dùng về trang đăng nhập.

### Ngày 5 Hồi quy bảo mật và hiệu năng

- Chạy toàn bộ E2E API và giao diện.
- Kiểm tra truy cập chéo vai trò.
- Kiểm tra upload file, dữ liệu đầu vào và thông tin nhạy cảm trong log.
- Kiểm tra phân trang và báo cáo với tập dữ liệu lớn.
- Rà soát query chậm, N+1 và chỉ mục thiếu.
- Kiểm tra cấu hình production không lộ lỗi nội bộ hoặc secret.
- Kiểm tra các luồng đồng thời của nhập và bán hàng.

### Ngày 6 Nghiệm thu và bàn giao

- Chạy toàn bộ kiểm thử trên database sạch.
- Chạy lại lần cuối trên dữ liệu demo.
- Thực hiện luồng E2E nghiệm thu:

```text
Admin tạo tài khoản
-> Quản lý kho tạo sản phẩm
-> Quản lý kho nhập hàng
-> Nhân viên tạo khách hàng và đơn hàng
-> Hoàn tất giao hàng
-> Kiểm tra tồn kho và báo cáo
-> Chạy dự báo
-> Xem cảnh báo và gợi ý nhập hàng
-> Export báo cáo Excel
```

- Hoàn thiện README, hướng dẫn triển khai và tài liệu API.
- Hoàn thiện ERD và sơ đồ kiến trúc.
- Viết báo cáo tổng kết và chuẩn bị kịch bản demo.
- Gắn phiên bản release ổn định.

### Kết quả bàn giao tuần 4

- Import và export Excel hoạt động.
- Giao diện quản trị sử dụng được trên các kích thước màn hình chính.
- E2E API và giao diện bao phủ các hành trình nghiệp vụ quan trọng.
- Toàn bộ luồng nghiệm thu hoạt động trên database sạch.
- Có tài liệu cài đặt, vận hành, kiểm thử và báo cáo cuối kỳ.

Tiến độ dự kiến cuối tuần: hoàn thành MVP.

## 8. Luồng E2E trọng tâm cần duy trì

### Luồng 1 Xác thực và phân quyền

```text
Tạo tài khoản
-> đăng nhập
-> truy cập API đúng quyền
-> bị từ chối ở API sai quyền
-> đăng xuất
-> token cũ bị từ chối
```

### Luồng 2 Nhập hàng

```text
Tạo sản phẩm và kho
-> tạo nhà cung cấp
-> lập phiếu nhập
-> xác nhận phiếu
-> tồn kho tăng
-> xác nhận lại không làm tăng lần hai
```

### Luồng 3 Bán hàng

```text
Tạo khách hàng
-> tạo đơn hàng
-> thêm sản phẩm
-> chuyển trạng thái
-> hoàn tất giao hàng
-> tồn kho giảm
-> doanh số tăng
```

### Luồng 4 Bảo vệ tồn kho

```text
Chuẩn bị tồn kho giới hạn
-> gửi hai yêu cầu bán đồng thời
-> chỉ yêu cầu hợp lệ được ghi nhận
-> tồn kho không âm
-> không có giao dịch kho trùng
```

### Luồng 5 Dự báo và nhập hàng

```text
Tạo lịch sử bán hàng
-> chạy tổng hợp dữ liệu
-> chạy dự báo
-> lưu kết quả và sai số
-> phát hiện tồn thấp
-> sinh số lượng đề xuất nhập
```

### Luồng 6 Excel

```text
Import dữ liệu mẫu
-> kiểm tra dữ liệu trong hệ thống
-> xuất báo cáo
-> mở và đối chiếu nội dung file
```

### Luồng 7 Giao diện hoàn chỉnh

```text
Đăng nhập trên frontend
-> thực hiện nghiệp vụ theo vai trò
-> xem dashboard
-> chạy dự báo
-> tải báo cáo
-> đăng xuất
```

## 9. Phạm vi ưu tiên khi có rủi ro tiến độ

### Bắt buộc

- Xác thực và phân quyền.
- Sản phẩm, kho, nhập hàng và đơn hàng.
- Tồn kho chính xác và không âm.
- Báo cáo doanh số.
- Một baseline và một mô hình dự báo có đánh giá sai số.
- Cảnh báo tồn kho và gợi ý nhập hàng.
- E2E cho các luồng nghiệp vụ chính.
- Tài liệu cài đặt và demo.

### Có thể giản lược

- Ảnh sản phẩm dùng URL thay vì hệ thống lưu file riêng.
- Chỉ hỗ trợ một mẫu import sản phẩm và một mẫu lịch sử bán hàng.
- Chỉ dùng các biểu đồ đường, cột và tròn cần thiết.
- Chỉ triển khai lịch dự báo hàng tuần trong MVP.
- Frontend ưu tiên luồng desktop, sau đó điều chỉnh responsive cho các màn hình chính.

## 10. Theo dõi tiến độ hằng ngày

Cuối mỗi ngày cần ghi nhận:

- Chức năng đã triển khai.
- Kịch bản E2E đã thêm hoặc cập nhật.
- Kết quả build và tổng số kiểm thử đạt hoặc thất bại.
- Lỗi còn tồn tại.
- Thay đổi database và API.
- Rủi ro ảnh hưởng kế hoạch tuần.
- Công việc ưu tiên cho ngày tiếp theo.

Không báo cáo một chức năng là hoàn thành nếu E2E của chức năng đó chưa đạt.
