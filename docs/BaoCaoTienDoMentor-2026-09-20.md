# BÁO CÁO TIẾN ĐỘ VỚI MENTOR

**Ngày báo cáo:** 20/09/2026  
**Dự án:** Sales Forecast System  
**Công nghệ:** ASP.NET Core 8, Entity Framework Core 8, SQL Server, JWT, BCrypt, Swagger

## 1. Tóm tắt điều hành

Dự án đang ở giai đoạn hoàn thiện nền tảng backend và các module dữ liệu chủ. Hiện tại hệ thống đã chạy được các luồng đăng nhập, quản lý phiên, phân quyền, quản lý danh mục, sản phẩm và người dùng. Database đã có schema cho toàn bộ nghiệp vụ kho, nhập hàng, bán hàng và dự báo, nhưng các API tương ứng, thuật toán dự báo và frontend chưa được triển khai.

Tiến độ ước tính hiện tại là **khoảng 41% phạm vi MVP**. Đây là ước tính theo các hạng mục trong kế hoạch 4 tuần, không phải tỷ lệ dòng code.

Kết quả xác minh lại ngay trước buổi báo cáo ngày 20/09/2026:

- Solution build thành công: **0 warning, 0 error**.
- **202/202** kiểm tra HTTP/E2E đạt.
- **7/7** kiểm tra ACID của database đạt.
- Database đang ở **schema version 6**, gồm **17 bảng, 3 view và 2 stored procedure**.
- Dữ liệu kiểm thử tạm được dọn sau khi chạy.

## 2. Mục tiêu của hệ thống

Hệ thống hướng tới hỗ trợ doanh nghiệp:

- Quản lý người dùng và phân quyền theo vai trò.
- Quản lý danh mục, sản phẩm, kho, nhà cung cấp và khách hàng.
- Xử lý nhập hàng, bán hàng và theo dõi tồn kho.
- Tổng hợp báo cáo doanh số.
- Dự báo nhu cầu bán hàng từ dữ liệu lịch sử.
- Cảnh báo tồn kho và gợi ý số lượng cần nhập.
- Import/export Excel và cung cấp giao diện quản trị.

## 3. Kiến trúc hiện tại

```text
Swagger / client
       |
       v
ASP.NET Core API Controllers
       |
       v
Core: DTO, interface, kiểu kết quả nghiệp vụ
       |
       v
Infrastructure: service, EF Core, BCrypt
       |
       v
SQL Server: bảng, view, stored procedure, trigger, constraint
```

Vai trò từng project:

- `SalesForecastSystem.API`: nhận HTTP request, xác thực JWT, phân quyền, Swagger và ánh xạ kết quả sang HTTP status.
- `SalesForecastSystem.Core`: chứa DTO, interface service, role constants và cấu trúc response dùng chung; không phụ thuộc tầng dữ liệu.
- `SalesForecastSystem.Infrastructure`: triển khai nghiệp vụ, truy vấn EF Core, entity/configuration và seed tài khoản Admin.
- `database`: quản lý schema theo các SQL migration có version, stored procedure và kiểm tra ACID.
- `tests/SalesForecastSystem.IntegrationChecks`: chương trình kiểm thử tích hợp chạy API thật với SQL Server thật.

Dự án chọn hướng **Database First bằng SQL migration** vì database chứa nhiều quy tắc nhất quán quan trọng như transaction, trigger, view, stored procedure và khóa đồng thời. EF Core được dùng để ánh xạ và truy vấn ở tầng ứng dụng.

## 4. Luồng hoạt động chính đã hiểu

### 4.1. Đăng nhập và quản lý phiên

1. Client gửi email và mật khẩu đến `POST /api/auth/login`.
2. `AuthService` chuẩn hóa email, kiểm tra trạng thái tài khoản/vai trò và xác minh mật khẩu BCrypt.
3. Hệ thống tạo JWT access token và đồng thời lưu một phiên đăng nhập trong bảng `LoginSessions`.
4. Với mỗi request có token, hệ thống kiểm tra lại phiên, trạng thái tài khoản và vai trò. Vì vậy token cũ có thể bị vô hiệu hóa ngay khi đăng xuất, khóa tài khoản, đổi vai trò hoặc đặt lại mật khẩu.
5. `POST /api/auth/logout` thu hồi phiên hiện tại; token cũ sau đó nhận `401 Unauthorized`.

### 4.2. Phân quyền

- `Admin`: quản lý danh mục, sản phẩm và người dùng.
- `WarehouseManager`: xem danh mục; xem, tạo, sửa và ngừng hoạt động sản phẩm.
- `SalesStaff`: chỉ xem danh mục và sản phẩm trong phạm vi hiện có.
- Người chưa đăng nhập nhận `401`; đăng nhập nhưng sai vai trò nhận `403`.

### 4.3. Danh mục và sản phẩm

- Danh mục có đủ CRUD; chỉ Admin được thay đổi dữ liệu.
- Sản phẩm hỗ trợ tìm kiếm, lọc, sắp xếp và phân trang.
- Sản phẩm được soft delete bằng cách chuyển `isActive = false` để giữ lịch sử chứng từ.
- `RowVersion` được dùng để phát hiện hai người cùng cập nhật một sản phẩm; dữ liệu cũ trả `409 Conflict`.
- Tồn kho không được sửa trực tiếp qua API sản phẩm. Tồn kho được tính từ sổ giao dịch kho, giúp tránh sai lệch dữ liệu.

### 4.4. Quản lý người dùng

- Chỉ Admin được xem, tạo, sửa, khóa/mở khóa và đặt lại mật khẩu.
- Email được chuẩn hóa và chống trùng.
- Password hash không xuất hiện trong response.
- Admin không thể tự khóa hoặc tự đổi vai trò của chính mình.
- Các thao tác nhạy cảm như khóa tài khoản, đổi vai trò và reset password sẽ thu hồi phiên cũ.

### 4.5. Bảo vệ dữ liệu kho ở database

- Nhập hàng và hoàn tất đơn bán được thiết kế qua stored procedure có transaction.
- Ghi nhận tồn kho dùng ledger `InventoryTransactions`, không dùng một cột số lượng có thể sửa tùy ý.
- Ledger là append-only; không cho sửa hoặc xóa lịch sử.
- Trigger ngăn mọi thao tác làm tồn kho âm.
- Khóa database giúp hai request đồng thời không cùng tiêu thụ một lượng tồn.
- Retry thao tác ghi sổ không tạo giao dịch trùng.

## 5. Các hạng mục đã hoàn thành và có thể demo

| Hạng mục | Trạng thái | Bằng chứng |
| --- | --- | --- |
| Kiến trúc API/Core/Infrastructure | Hoàn thành nền tảng | Solution build sạch |
| SQL Server và schema có version | Hoàn thành nền tảng | Version 6, 17 bảng |
| Đăng nhập, JWT, session, logout | Hoàn thành | E2E login/logout/token bị thu hồi đạt |
| Phân quyền 3 vai trò | Hoàn thành | Các trường hợp 401/403/200 đạt |
| CRUD danh mục | Hoàn thành | API và validation E2E đạt |
| Quản lý sản phẩm | Hoàn thành | CRUD, soft delete, tồn kho, RowVersion đạt |
| Tìm kiếm/lọc/sắp xếp/phân trang sản phẩm | Hoàn thành | E2E trường hợp hợp lệ và không hợp lệ đạt |
| Quản lý người dùng | Hoàn thành trên máy local | 42 kiểm tra module và hồi quy đạt |
| Quy tắc ACID cho nhập/bán/tồn | Hoàn thành ở database | 7/7 kiểm tra database đạt |
| Swagger và hướng dẫn chạy | Hoàn thành cho phạm vi hiện tại | Có Bearer auth và ví dụ request |

## 6. Phần mới có nền tảng database, chưa có API hoàn chỉnh

- Kho và nhà cung cấp.
- Khách hàng.
- Phiếu nhập và chi tiết phiếu nhập.
- Đơn hàng bán và chi tiết đơn hàng.
- Giao dịch kho.
- Mô hình dự báo, lần chạy dự báo và kết quả dự báo.
- View tồn kho, doanh số theo ngày và tổng tiền đơn hàng.
- Stored procedure xác nhận nhập hàng và hoàn tất đơn hàng.

Các bảng và quy tắc database đã tồn tại, nhưng người dùng chưa thể thao tác đầy đủ qua API.

## 7. Phần chưa triển khai

- API kho, nhà cung cấp, khách hàng, phiếu nhập và đơn hàng bán.
- API báo cáo và dashboard.
- Chuẩn bị dữ liệu, thuật toán và lịch chạy dự báo.
- Cảnh báo tồn kho và gợi ý nhập hàng.
- Import/export Excel.
- Frontend Bootstrap.

## 8. Rủi ro và vấn đề cần xử lý

1. **Thay đổi local chưa được chốt thành commit:** working tree hiện có 19 file sửa, 20 file xóa và 40 file chưa được Git theo dõi. Dù build/test đang đạt, trạng thái này có nguy cơ mất thay đổi hoặc khó review/merge. Cần rà soát và chia thành các commit nhỏ theo chức năng.
2. **Chức năng cốt lõi “dự báo” chưa bắt đầu ở tầng ứng dụng:** tên dự án là Sales Forecast System nhưng phần demo hiện tại chủ yếu là nền tảng quản trị. Cần ưu tiên sớm một lát cắt dự báo tối thiểu để chứng minh giá trị chính.
3. **Nghiệp vụ nhập/bán mới có database:** stored procedure đã có nhưng chưa có service/controller để tạo chứng từ và xác nhận giao dịch.
4. **Cấu hình database phụ thuộc máy local:** connection string đang chứa tên SQL Server của máy phát triển. Cần chuyển sang User Secrets hoặc biến môi trường cho các môi trường khác.
5. **Chưa có CI tự động:** kiểm thử hiện chạy bằng chương trình console trên máy local; chưa thấy workflow để build/test tự động khi push.
6. **Chưa có unit test tách biệt:** 202 kiểm tra hiện là integration/E2E. Đây là bằng chứng tốt cho luồng tổng thể nhưng một số nghiệp vụ nhỏ sẽ khó khoanh vùng lỗi nhanh.

## 9. Kế hoạch đề xuất tiếp theo

### Ưu tiên ngay sau buổi báo cáo

1. Rà soát thay đổi local, dọn file sinh ra và commit riêng các nhóm: refactor English schema, Product, User, test và tài liệu.
2. Chạy lại build, 202 HTTP checks và 7 ACID checks sau khi commit.
3. Tạo tag hoặc nhánh ổn định cho baseline Tuần 1.

### Lát cắt chức năng tiếp theo

1. API kho và nhà cung cấp.
2. API khách hàng.
3. API phiếu nhập gọi `usp_PostPurchaseOrder` và kiểm tra tồn kho tăng.
4. API đơn hàng gọi `usp_CompleteSalesOrder` và kiểm tra không bán âm kho.
5. Báo cáo doanh số từ `vw_DailySales`.
6. Sau khi có dữ liệu lịch sử bán hàng, triển khai baseline forecast và API kết quả dự báo.

Mỗi lát cắt tiếp tục theo chu trình: database → service → controller → phân quyền → E2E → tài liệu.

## 10. Kịch bản demo 5 phút

1. Chạy `dotnet build` để cho thấy solution build sạch.
2. Chạy API và mở Swagger.
3. Đăng nhập Admin, copy access token và Authorize.
4. Gọi `GET /api/auth/me` để chứng minh danh tính/role.
5. Tạo một danh mục và một sản phẩm.
6. Gọi danh sách sản phẩm với search/filter/page để demo truy vấn.
7. Đăng nhập SalesStaff và thử tạo sản phẩm để nhận `403`.
8. Đăng xuất rồi gọi lại `/api/auth/me` bằng token cũ để nhận `401`.
9. Kết thúc bằng kết quả `202/202 HTTP checks passed` và `7/7 ACID checks passed`.

Không nên demo thuật toán dự báo ở thời điểm này vì chưa được triển khai. Có thể mở schema các bảng forecast để trình bày phần nền tảng và kế hoạch tiếp theo.

## 11. Lời trình bày ngắn với mentor

> Hiện tại em đã hoàn thành nền tảng backend của hệ thống bằng ASP.NET Core 8 và SQL Server. Luồng đăng nhập dùng JWT nhưng có quản lý session ở database, nên hệ thống có thể thu hồi token khi đăng xuất, khóa tài khoản, đổi vai trò hoặc reset mật khẩu. Em đã hoàn thiện các module danh mục, sản phẩm và quản lý người dùng, gồm validation, phân quyền, tìm kiếm và phân trang.
>
> Ở tầng database, em đã thiết kế đủ schema cho kho, nhập hàng, bán hàng và dự báo. Hai nghiệp vụ ghi sổ nhập và bán dùng stored procedure có transaction; tồn kho dùng ledger append-only và có cơ chế ngăn bán âm. Hiện solution build sạch, 202 kiểm tra HTTP và 7 kiểm tra ACID đều đạt.
>
> Tiến độ MVP em ước tính khoảng 41%. Phần còn thiếu lớn nhất là API nhập/bán, báo cáo, thuật toán dự báo và frontend. Việc ưu tiên tiếp theo của em là chốt baseline hiện tại thành các commit dễ review, sau đó hoàn thiện API kho/nhà cung cấp, nhập hàng và đơn hàng để tạo dữ liệu bán thực tế cho phần dự báo.

## 12. Câu hỏi mentor có thể hỏi

### Tại sao dùng cả JWT và bảng session?

JWT thuần túy khó thu hồi trước khi hết hạn. Bảng `LoginSessions` cho phép kiểm tra và thu hồi token ngay khi logout, khóa tài khoản, đổi quyền hoặc reset password.

### Tại sao dùng Database First thay vì EF Migration?

Database đang sở hữu các quy tắc phức tạp như stored procedure, trigger, view, transaction và locking. SQL migration có version giúp các quy tắc này rõ ràng, chạy lặp an toàn và dễ kiểm chứng trực tiếp.

### Tại sao không lưu số lượng tồn trực tiếp trong bảng sản phẩm?

Tồn kho được suy ra từ ledger giao dịch để có lịch sử kiểm toán và tránh việc sửa số lượng không có chứng từ. View tổng hợp cung cấp số tồn hiện tại.

### Dự báo đã chạy chưa?

Chưa. Schema lưu model/run/result và ràng buộc dữ liệu đã có, nhưng pipeline xử lý dữ liệu, thuật toán, lịch chạy và API dự báo chưa được triển khai. Đây là phần tiếp theo sau khi luồng nhập/bán tạo được dữ liệu lịch sử đáng tin cậy.

### 202 kiểm tra có phải unit test không?

Không. Đây là integration/E2E checks chạy API thật với SQL Server thật. Chúng kiểm tra tốt luồng tổng thể, HTTP status, validation, phân quyền và hồi quy. Unit test riêng chưa được bổ sung.

### Tiến độ 41% được tính như thế nào?

Đây là ước tính theo các hạng mục MVP trong kế hoạch 4 tuần: nền tảng, auth, danh mục, sản phẩm và người dùng đã hoàn thành; nhập/bán, báo cáo, forecast, Excel và frontend còn lại. Không nên diễn giải 41% là tỷ lệ dòng code.
