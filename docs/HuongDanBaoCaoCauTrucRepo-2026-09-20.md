# HƯỚNG DẪN BÁO CÁO CẤU TRÚC REPOSITORY VỚI MENTOR

**Ngày trình bày:** 20/09/2026  
**Dự án:** Sales Forecast System

## 1. Cách mở đầu

> Trong hai tuần qua, em tập trung xây dựng nền tảng backend và chuẩn hóa cấu trúc source code. Repo hiện được tổ chức theo kiến trúc phân tầng, định hướng Clean Architecture, gồm API, Core và Infrastructure. Ngoài ba project C#, em tách riêng database script, integration checks và tài liệu. Mục tiêu của cách tổ chức này là tách trách nhiệm, hạn chế controller chứa nghiệp vụ, giúp từng module dễ kiểm thử và dễ mở rộng.

Không nên nói dự án đang dùng Clean Architecture hoàn chỉnh. Cách gọi chính xác hơn là **layered architecture, hướng tới Clean Architecture**, vì project API vẫn tham chiếu Infrastructure để đăng ký dependency và một số thành phần xác thực còn truy cập `AppDbContext` trực tiếp.

## 2. Bức tranh tổng thể

```text
SalesForecastSystem.sln
|
+-- SalesForecastSystem.API
|   +-- HTTP, JWT, authorization, Swagger, DI
|
+-- SalesForecastSystem.Core
|   +-- DTO, interface, kiểu kết quả dùng chung, role constants
|
+-- SalesForecastSystem.Infrastructure
|   +-- EF Core, entity, mapping, service nghiệp vụ, BCrypt
|
+-- database
|   +-- schema, view, procedure, trigger, verification
|
+-- tests/SalesForecastSystem.IntegrationChecks
|   +-- chạy API thật và SQL Server thật
|
+-- docs và implementation.md
    +-- tài liệu chạy, báo cáo và kế hoạch
```

Quan hệ phụ thuộc giữa ba project:

```text
API ----------> Core
 |               ^
 +----------> Infrastructure
                    |
                    +----------> Core
```

Ý nghĩa:

- `Core` không phụ thuộc API hoặc Infrastructure nên các hợp đồng nghiệp vụ có thể ổn định khi thay đổi công nghệ bên ngoài.
- `Infrastructure` triển khai các interface được khai báo trong Core.
- `API` là điểm khởi động và composition root: nhận request, đăng ký implementation, cấu hình middleware rồi trả HTTP response.
- Controller không nên tự viết truy vấn database; controller gọi interface service.
- SQL Server bảo vệ các bất biến quan trọng của nhập hàng, bán hàng và tồn kho kể cả khi có ứng dụng khác truy cập database.

## 3. Ý nghĩa của từng tầng

### 3.1. API — cửa vào của hệ thống

Tầng API chịu trách nhiệm liên quan đến HTTP:

- Route và HTTP method.
- Nhận request/query parameter.
- Xác thực JWT và phân quyền role.
- Gọi service.
- Chuyển kết quả nghiệp vụ thành status code `200`, `201`, `400`, `401`, `403`, `404`, `409`.
- Sinh Swagger.

API không nên chứa truy vấn và luật nghiệp vụ dài. Ví dụ `ProductController` chỉ nhận request rồi gọi `IProductService`; việc kiểm tra category, chống trùng SKU và cập nhật sản phẩm nằm trong `ProductService`.

### 3.2. Core — hợp đồng của hệ thống

Core chứa những thứ các tầng khác cùng hiểu:

- DTO đầu vào/đầu ra.
- Interface mô tả service phải làm gì.
- Kiểu kết quả thành công/thất bại dùng chung.
- Hằng số vai trò.
- Validation cơ bản của request.

Core không biết SQL Server, `DbContext`, controller hoặc cách tạo JWT. Vì vậy Core đóng vai trò là ranh giới giữa bên gọi và phần triển khai.

### 3.3. Infrastructure — phần triển khai và truy cập dữ liệu

Infrastructure chứa:

- Entity ánh xạ bảng/view.
- EF Core configuration.
- `AppDbContext`.
- Service triển khai interface trong Core.
- Băm mật khẩu BCrypt.
- Seeder tạo Admin.

Việc tách `Entity` và `Configuration` giúp class entity dễ đọc, còn tên bảng, index, độ dài, precision, khóa ngoại và concurrency được quản lý tập trung.

### 3.4. Database — nguồn bảo vệ dữ liệu cuối cùng

Database được quản lý bằng SQL script có version thay vì EF Migration. Lý do là schema có view, stored procedure, trigger, transaction và locking phức tạp. Các quy tắc như không bán âm kho hoặc không sửa ledger không nên chỉ dựa vào API.

Hiện database có 17 bảng nhưng `AppDbContext` mới ánh xạ 6 model cần cho các API đã làm. Điều này thể hiện rõ: nền tảng database cho nghiệp vụ sau đã có, còn tầng ứng dụng mới hoàn thiện Auth, Category, Product và User.

### 3.5. Integration checks — kiểm tra xuyên tầng

Project test chạy API thật với SQL Server thật, vì vậy kiểm tra được cả route, model binding, JWT, authorization, service, EF Core và constraint database. Đây là integration/E2E checks, không phải unit test.

## 4. Luồng một request đi qua các file

### Ví dụ đăng nhập

```text
POST /api/auth/login
  -> AuthController.cs
  -> IAuthService.cs
  -> AuthService.cs
  -> AppDbContext.cs -> Users/Role
  -> ITokenService.cs
  -> JwtTokenService.cs -> tạo JWT + LoginSession
  -> LoginResponse.cs
```

Khi dùng token gọi API khác:

```text
JWT middleware trong Program.cs
  -> SessionJwtEvents.cs
  -> kiểm tra LoginSessions, User, Role
  -> [Authorize] trên Controller
  -> action được phép chạy hoặc trả 401/403
```

### Ví dụ cập nhật sản phẩm

```text
PUT /api/products/{id}
  -> ProductController.cs
  -> ProductUpdateRequest trong ProductRequest.cs
  -> IProductService.cs
  -> ProductService.cs
  -> AppDbContext.cs
  -> ProductConfiguration.cs
  -> bảng Products trong SQL Server
  -> ServiceResult<ProductResponse>
  -> ApiControllerBase.cs
  -> HTTP 200, 400, 404 hoặc 409
```

Luồng này minh họa ba ý nên nhấn mạnh với mentor:

1. Request/response không dùng trực tiếp entity database.
2. Controller không chứa nghiệp vụ dài.
3. Lỗi nghiệp vụ được trả về có cấu trúc thống nhất.

## 5. Giải thích từng file ở thư mục gốc

| File/thư mục | Vai trò | Ý nghĩa khi trình bày |
| --- | --- | --- |
| `SalesForecastSystem.sln` | Gom ba project C# vào một solution Visual Studio | Build và quản lý toàn bộ backend từ một điểm |
| `.gitignore` | Loại `bin`, `obj`, `.vs`, log, database local và file sinh tự động khỏi Git | Repo chỉ lưu source cần thiết, tránh commit rác hoặc file máy cá nhân |
| `.gitattributes` | Chuẩn hóa cách Git xử lý line ending/text | Giảm diff không cần thiết giữa môi trường phát triển |
| `implementation.md` | Kế hoạch MVP 4 tuần, Definition of Done, trạng thái và luồng E2E | Là tài liệu quản lý phạm vi và tiến độ, không phải code runtime |
| `database/` | Toàn bộ schema và quy tắc SQL Server | Database được version hóa cùng source code |
| `docs/` | Hướng dẫn chạy, báo cáo và tài liệu thực tập | Tách tài liệu khỏi code thực thi |
| `tests/` | Project integration checks | Cho thấy test là một phần độc lập của solution/repo |
| `.vs/`, `bin/`, `obj/` | File cache và kết quả build cục bộ | Không phải source, không trình bày như chức năng |
| `output/`, `tmp/` | Artifact/tệp tạm trong quá trình làm việc | Cần rà soát và không nên đưa vào commit nếu không phải sản phẩm bàn giao |

### Lưu ý về `SalesForecastSystem.sln`

Solution hiện vẫn khai báo các solution folder dự kiến cho `SalesForecastSystem.ML`, gồm `app`, `preprocessing`, `training`, `prediction`, `data`, `models`, `tests`. Tuy nhiên `main.py`, `requirements.txt` và README của phần này đã bị xóa khỏi working tree. Vì vậy:

- Đây chỉ là dấu vết/khung dự kiến của phần forecast bằng Python.
- Chưa được xem là một module ML đang hoạt động.
- Sau buổi báo cáo nên cập nhật `.sln`: hoặc khôi phục module thật, hoặc xóa các entry không còn tồn tại để Solution Explorer phản ánh đúng repo.

## 6. Giải thích từng file trong `SalesForecastSystem.API`

### File project và cấu hình

| File | Vai trò |
| --- | --- |
| `SalesForecastSystem.API.csproj` | Project web .NET 8; tham chiếu JWT Bearer, Swagger, Core và Infrastructure; khai báo `UserSecretsId` để giữ secret ngoài Git |
| `Program.cs` | Entry point: đăng ký DI, DbContext, JWT, authorization, Swagger, exception handler, seeder và middleware pipeline |
| `appsettings.json` | Connection string local, logging và allowed hosts; hiện còn phụ thuộc tên SQL Server của máy phát triển |
| `appsettings.Development.json` | Ghi đè cấu hình dành cho môi trường Development |
| `Properties/launchSettings.json` | Profile chạy HTTP/HTTPS, port `5112`/`7191`, tự mở Swagger và đặt môi trường Development |
| `SalesForecastSystem.API.http` | File gửi request thủ công từ IDE; hiện vẫn gọi `/weatherforecast`, là nội dung template cũ cần cập nhật |

Điểm quan trọng trong `Program.cs`:

- Interface được nối với implementation bằng dependency injection.
- JWT kiểm tra issuer, audience, lifetime, signing key và thuật toán HMAC SHA-256.
- `ClockSkew = 0` làm token hết hạn đúng thời điểm.
- Fallback authorization policy yêu cầu đăng nhập theo mặc định; endpoint công khai phải ghi rõ `[AllowAnonymous]`.
- Swagger và seed Admin chỉ bật trong Development.

### `Controllers`

| File | Vai trò |
| --- | --- |
| `ApiControllerBase.cs` | Hàm dùng chung chuyển `ServiceResult` thành `ValidationProblem`, `404`, `409` hoặc success response; giảm lặp code ở controller |
| `AuthController.cs` | `login`, `logout`, `me`; tắt cache với dữ liệu xác thực; lấy `sub` và `jti` từ token khi logout |
| `CategoryController.cs` | API CRUD danh mục; mọi role được xem, chỉ Admin được tạo/sửa/xóa |
| `ProductController.cs` | API danh sách, chi tiết, tồn kho, tạo, sửa, soft delete sản phẩm; Admin và WarehouseManager được ghi |
| `UserController.cs` | API Admin-only để tìm/xem/tạo/sửa/khóa/mở khóa/reset password người dùng |
| `AccessCheckController.cs` | Endpoint kỹ thuật kiểm tra ma trận phân quyền ba role; hữu ích cho demo/test, không phải nghiệp vụ chính |
| `DatabaseController.cs` | Endpoint Admin kiểm tra API có kết nối được SQL Server hay không |
| `DevCheckController.cs` | Chỉ ở Development và dành cho Admin; kiểm tra tài khoản Admin seed đã tồn tại |

Ý nghĩa cách tách controller theo resource:

- Route dễ đoán: `/api/auth`, `/api/categories`, `/api/products`, `/api/users`.
- Mỗi controller có một trách nhiệm chính.
- Khi thêm module Warehouse hoặc Order có thể thêm controller mới mà không làm controller hiện tại quá lớn.

### `Services` của API

| File | Vai trò |
| --- | --- |
| `JwtTokenService.cs` | Tạo JWT 15 phút, thêm claim `sub`, `jti`, `email`, `name`, `role`; đồng thời ghi `LoginSession` để có thể thu hồi token |
| `SessionJwtEvents.cs` | Chạy sau khi chữ ký JWT hợp lệ; kiểm tra session chưa bị revoke/hết hạn, user còn Active, role còn active và role trong token chưa thay đổi |

Hai file này nằm ở API vì phụ thuộc mạnh vào framework JWT và cấu hình web. Tuy nhiên `JwtTokenService` cũng dùng `AppDbContext`; đây là một điểm coupling có thể cải thiện sau bằng cách đưa phần lưu session qua interface trong Core.

### `Swagger`

| File | Vai trò |
| --- | --- |
| `AuthorizeOperationFilter.cs` | Tự gắn yêu cầu Bearer và response `401`/`403` vào Swagger cho mọi endpoint không có `[AllowAnonymous]` |

## 7. Giải thích từng file trong `SalesForecastSystem.Core`

### File project

| File | Vai trò |
| --- | --- |
| `SalesForecastSystem.Core.csproj` | Class library .NET 8, không tham chiếu Infrastructure/API; là tầng hợp đồng trung tâm |

### `Common`

| File | Vai trò |
| --- | --- |
| `PagedResponse.cs` | Response phân trang dùng chung: items, page, pageSize, totalItems, totalPages và trạng thái có trang trước/sau |
| `ServiceError.cs` | Định nghĩa ba nhóm lỗi nghiệp vụ: NotFound, Conflict, Validation; kèm message và field liên quan |
| `ServiceResult.cs` | Bao kết quả service thành success hoặc failure mà không để controller phụ thuộc exception cho lỗi nghiệp vụ thông thường |

Ý nghĩa: service không cần biết HTTP status. API mới là tầng quyết định `NotFound` tương ứng `404`, `Conflict` tương ứng `409`.

### `DTOs/Auth`

| File | Vai trò |
| --- | --- |
| `LoginRequest.cs` | Request đăng nhập; validate email, required và độ dài |
| `LoginResponse.cs` | Response gồm access token, loại token, thời gian hết hạn và thông tin user an toàn |

### `DTOs/Categories`

| File | Vai trò |
| --- | --- |
| `CategoryRequest.cs` | Dữ liệu tạo/sửa danh mục và validation độ dài |
| `CategoryResponse.cs` | Dữ liệu danh mục trả ra client |

### `DTOs/Products`

| File | Vai trò |
| --- | --- |
| `ProductQueryRequest.cs` | Query tìm kiếm/lọc/sắp xếp/phân trang; định nghĩa enum stock status, sort field, sort direction |
| `ProductRequest.cs` | Request tạo/sửa, validation SKU, URL ảnh, giá, ngưỡng tồn; từ chối chỉnh tồn kho trực tiếp; chứa `ProductUpdateRequest` có `RowVersion` |
| `ProductResponse.cs` | Chi tiết sản phẩm, gồm `RowVersion` Base64 để cập nhật có kiểm soát đồng thời |
| `ProductListItemResponse.cs` | Dòng hiển thị danh sách: category, giá, số tồn và stock status |
| `ProductStockResponse.cs` | Response tối giản cho endpoint xem tổng tồn của một sản phẩm |

Tách list item và detail response giúp endpoint danh sách không phải trả mọi trường, đồng thời thể hiện rõ mục đích sử dụng.

### `DTOs/Users`

| File | Vai trò |
| --- | --- |
| `UserRequests.cs` | Gom query, create, update, status và reset-password request; chuẩn hóa tập role/status hợp lệ và validation mật khẩu |
| `UserResponse.cs` | Thông tin người dùng an toàn; cố ý không có password hoặc password hash |

### `Helpers`

| File | Vai trò |
| --- | --- |
| `RoleNames.cs` | Một nguồn duy nhất cho `Admin`, `WarehouseManager`, `SalesStaff` và các nhóm role dùng trong `[Authorize]` |

### `Interfaces/Services`

| File | Vai trò |
| --- | --- |
| `IAuthService.cs` | Hợp đồng kiểm tra đăng nhập và trả `LoginResponse` |
| `ITokenService.cs` | Hợp đồng tạo access token; giúp `AuthService` không phụ thuộc class tạo JWT cụ thể |
| `ISessionService.cs` | Hợp đồng thu hồi một phiên đăng nhập |
| `ICategoryService.cs` | Hợp đồng CRUD danh mục |
| `IProductService.cs` | Hợp đồng query/CRUD/stock sản phẩm |
| `IUserService.cs` | Hợp đồng query, CRUD quản trị, khóa/mở khóa và reset password người dùng |

Interface có lợi cho dependency inversion và khả năng thay implementation/test double về sau. Tuy vậy repo hiện chủ yếu dùng integration test, chưa có unit test dùng mock.

## 8. Giải thích từng file trong `SalesForecastSystem.Infrastructure`

### File project

| File | Vai trò |
| --- | --- |
| `SalesForecastSystem.Infrastructure.csproj` | Class library .NET 8; tham chiếu EF Core SQL Server, EF tools, BCrypt và Core |

### `Data`

| File | Vai trò |
| --- | --- |
| `AppDbContext.cs` | Cửa ngõ EF Core; khai báo DbSet cho Role, User, LoginSession, Category, Product và view InventoryBalance; tự nạp mọi configuration trong assembly |

### `Data/Configurations`

| File | Vai trò |
| --- | --- |
| `RoleConfiguration.cs` | Map `Role` tới `dbo.Roles`, khóa chính, độ dài, unique index tên role |
| `UserConfiguration.cs` | Map `User`, email unique, index role/status, quan hệ nhiều user thuộc một role |
| `LoginSessionConfiguration.cs` | Map session, index user/expiry và quan hệ với user; không cascade delete |
| `CategoryConfiguration.cs` | Map category và unique index tên danh mục |
| `ProductConfiguration.cs` | Map kiểu/độ dài/precision/index/foreign key; cấu hình `RowVersion` cho optimistic concurrency |
| `InventoryBalanceConfiguration.cs` | Map read-only keyless entity tới view `vw_InventoryBalances` |

`DeleteBehavior.Restrict` có ý nghĩa giữ lịch sử và ngăn xóa cha làm mất dữ liệu con ngoài ý muốn.

### `Entities`

| File | Vai trò |
| --- | --- |
| `Role.cs` | Mô hình role và navigation tới danh sách user |
| `User.cs` | Mô hình tài khoản, password hash, status, role và login sessions; chứa constants `UserStatuses` |
| `LoginSession.cs` | Mô hình phiên JWT với thời điểm tạo, hết hạn và thu hồi |
| `Category.cs` | Mô hình danh mục và navigation tới sản phẩm |
| `Product.cs` | Mô hình sản phẩm, giá, ngưỡng tồn, trạng thái, timestamps và row version |
| `InventoryBalance.cs` | Dòng dữ liệu đọc từ view, gồm warehouse, product và quantity on hand |

Entity mô tả dữ liệu lưu trữ; DTO mô tả dữ liệu trao đổi với client. Không trả entity trực tiếp giúp tránh lộ `PasswordHash`, navigation property hoặc cấu trúc database nội bộ.

### `Services`

| File | Vai trò |
| --- | --- |
| `AuthService.cs` | Chuẩn hóa email, đọc user/role, kiểm tra trạng thái, xác minh BCrypt rồi gọi token service |
| `SessionService.cs` | Thu hồi session bằng `ExecuteUpdateAsync` mà không cần load entity |
| `CategoryService.cs` | CRUD category, trim dữ liệu, phát hiện trùng tên và chặn xóa category đang có product |
| `ProductService.cs` | Query sản phẩm, tổng hợp tồn từ view, search/filter/sort/page, validate reference/SKU, RowVersion concurrency và soft delete |
| `UserService.cs` | Quản trị user, chuẩn hóa email, hash password, kiểm tra role, phân trang và thu hồi toàn bộ session khi thông tin bảo mật thay đổi |

### `Seeders`

| File | Vai trò |
| --- | --- |
| `DataSeeder.cs` | Tạo Admin đầu tiên trong Development nếu email chưa tồn tại; validate password, hash BCrypt work factor 12 và dùng transaction |

Seeder không tự đổi mật khẩu Admin đã tồn tại, tránh việc mỗi lần chạy ứng dụng lại ghi đè tài khoản thật.

## 9. Giải thích từng file trong `database`

### Thứ tự deploy đang dùng

`Deploy.ps1` chạy:

```text
001 -> 004 -> 005 -> 006 -> 007 -> 008 -> 009
                                   |
                                   +-> thêm 003 khi có -Verify
```

| File | Vai trò |
| --- | --- |
| `Deploy.ps1` | Orchestrator gọi `sqlcmd`, dừng ngay nếu script lỗi, hỗ trợ `-Server` và `-Verify` |
| `001_schema.sql` | Baseline ban đầu: tạo database và toàn bộ schema bằng tên tiếng Việt nếu chưa có |
| `002_operations.sql` | Phiên bản legacy của view/procedure/trigger tên tiếng Việt; hiện không còn nằm trong danh sách deploy vì đã được thay bằng `006` |
| `003_verify.sql` | Kiểm tra rollback-based cho atomic posting, idempotency, chống bán âm, bất biến chứng từ, constraint, append-only ledger và forecast horizon |
| `004_auth_sessions.sql` | Bổ sung bảng phiên đăng nhập vào schema tiếng Việt cũ trước khi rename |
| `005_english_schema.sql` | Migration lớn đổi schema/tên object sang tiếng Anh, tạo lại constraint và index; bảo toàn dữ liệu hiện có |
| `006_english_operations.sql` | Tạo 3 view, 2 stored procedure và các trigger bằng tên tiếng Anh; cài cơ chế transaction/locking/bảo vệ ledger |
| `007_schema_cleanup.sql` | Dọn constraint/default cũ còn sót sau refactor schema |
| `008_english_defaults.sql` | Chuẩn hóa tên default constraint và giá trị mặc định trên schema tiếng Anh |
| `009_product_enhancements.sql` | Bổ sung description, image URL, minimum stock, updated time, row version và index phục vụ API sản phẩm |
| `README.md` | Giải thích chiến lược Database First, bảng, view, procedure, ACID, role database và cách verify |

Ý nghĩa lịch sử của chuỗi migration:

- Repo khởi đầu với schema tiếng Việt.
- Sau đó thêm session authentication.
- Tiếp theo refactor toàn bộ database đang hoạt động sang tiếng Anh mà không xóa dữ liệu.
- Các operation tiếng Việt được thay bằng phiên bản tiếng Anh.
- Các script sau cùng làm sạch default/constraint và mở rộng Product.

### Ba view hiện tại

- `vw_InventoryBalances`: tồn hiện tại theo kho và sản phẩm.
- `vw_DailySales`: doanh số hoàn thành theo ngày, kho và sản phẩm.
- `vw_SalesOrderTotals`: tổng tiền của đơn bán.

### Hai stored procedure hiện tại

- `usp_PostPurchaseOrder`: xác nhận phiếu nhập và tăng tồn trong một transaction.
- `usp_CompleteSalesOrder`: hoàn tất đơn bán sau khi khóa/kiểm tra tồn, không cho tồn âm.

### Vì sao các quy tắc này nằm ở database?

Nếu chỉ kiểm tra tồn ở service, hai request bán hàng đồng thời có thể cùng đọc một số tồn và đều thành công. Stored procedure dùng transaction, `UPDLOCK`, `HOLDLOCK` và application lock theo kho để bảo vệ tính nhất quán ở nơi dữ liệu thực sự được ghi.

## 10. Giải thích project kiểm thử

| File | Vai trò |
| --- | --- |
| `tests/SalesForecastSystem.IntegrationChecks/SalesForecastSystem.IntegrationChecks.csproj` | Console app .NET 8 tham chiếu API project để khởi chạy/kiểm tra hệ thống |
| `tests/SalesForecastSystem.IntegrationChecks/Program.cs` | Test runner tự viết: chuẩn bị dữ liệu, chạy các request, kiểm tra status/body/Swagger, restart API khi cần và dọn dữ liệu tạm |

Hiện có 202 HTTP checks. Các nhóm chính:

- Anonymous/invalid/expired token.
- Login, logout, token bị revoke và session sau restart.
- Ma trận phân quyền Admin/WarehouseManager/SalesStaff.
- CRUD/validation/xung đột của category và product.
- Search/filter/sort/pagination.
- RowVersion concurrency.
- Quản lý user, khóa tài khoản, đổi role và reset password.

Điểm cần trình bày trung thực:

- Đây là integration/E2E checks, không phải xUnit/NUnit unit tests.
- Ưu điểm là kiểm chứng luồng thật xuyên qua nhiều tầng.
- Nhược điểm là file `Program.cs` lớn, lỗi khó khoanh vùng hơn và chưa tích hợp CI.

## 11. Giải thích từng file tài liệu

| File | Vai trò |
| --- | --- |
| `docs/HuongDanChay.md` | Hướng dẫn cài SQL Server, deploy database, cấu hình User Secrets, chạy API, Swagger và test |
| `docs/BaoCaoNgay-2026-09-14.md` | Báo cáo giai đoạn dựng database, JWT/session, phân quyền, category và integration checks ban đầu |
| `docs/BaoCaoNgay-2026-09-18.md` | Báo cáo module quản lý user và kết quả 202 checks |
| `docs/BaoCaoTienDoMentor-2026-09-20.md` | Báo cáo tổng quan tiến độ, phần đã làm/chưa làm, rủi ro và demo |
| `docs/HuongDanBaoCaoCauTrucRepo-2026-09-20.md` | Tài liệu hiện tại dùng để giải thích cấu trúc repo và từng file |
| `docs/CHƯƠNG TRÌNH THỰC TẬP TỐT NGHIỆP - BE.docx` | Tài liệu yêu cầu/chương trình thực tập |
| `docs/QUI ĐỊNH THỂ THỨC TRÌNH BÀY.docx` | Quy định trình bày tài liệu/báo cáo |
| `docs/Phạm Minh Tài_Báo cáo ngày 15-09-2026.pdf` | Bản báo cáo PDF đã xuất trước đó |

## 12. Lợi ích và đánh đổi của cách tổ chức hiện tại

### Lợi ích

- Dễ tìm code theo trách nhiệm.
- Controller mỏng và nghiệp vụ có thể tái sử dụng.
- DTO ngăn lộ cấu trúc database và dữ liệu nhạy cảm.
- Interface giảm phụ thuộc trực tiếp giữa tầng gọi và implementation.
- EF mapping tách riêng giúp entity gọn.
- Database script có version và có thể deploy lặp lại.
- Quy tắc tồn kho được bảo vệ ở database, không chỉ ở API.
- Integration checks chứng minh luồng end-to-end thật sự chạy.

### Đánh đổi/hạn chế

- Có nhiều file hơn so với viết tất cả trong controller.
- Một thay đổi field có thể phải cập nhật DTO, entity, configuration, service, SQL và test.
- API hiện vẫn biết Infrastructure, nên chưa tách biệt tuyệt đối.
- Một số file template/solution entry đã lỗi thời.
- Chưa có repository layer; service dùng `DbContext` trực tiếp. Với quy mô hiện tại điều này đơn giản và chấp nhận được, nhưng khi nghiệp vụ lớn hơn có thể cân nhắc query service/repository chuyên biệt.
- Integration test runner đang là một file lớn và chưa có CI.

## 13. Các vấn đề nên chủ động nói với mentor

1. **Working tree chưa được chốt:** có nhiều file modified/deleted/untracked. Code đang chạy và test đạt nhưng cần chia commit để mentor review được.
2. **Solution còn entry ML cũ:** phần forecast Python chưa hoạt động; cần dọn `.sln` hoặc triển khai đúng cấu trúc.
3. **File `.http` lỗi thời:** vẫn gọi WeatherForecast đã xóa.
4. **Connection string gắn với máy local:** nên chuyển tên server/database sang User Secrets hoặc environment variable.
5. **Một số controller kỹ thuật:** `AccessCheck`, `Database`, `DevCheck` hữu ích trong phát triển nhưng cần xem xét bỏ/giới hạn khi production.
6. **Chưa có CI và unit test:** hiện chỉ có integration/E2E runner local.
7. **Forecast chưa triển khai ở application layer:** mới có schema/ràng buộc database và solution folder dự kiến.

Chủ động nêu các điểm này thể hiện bạn hiểu trạng thái repo và biết việc cần cải thiện, không phải dự án đang thất bại.

## 14. Kịch bản trình bày 10 phút

### Phút 0–1: Mục tiêu và kết quả hai tuần

> Trong hai tuần qua em ưu tiên dựng nền tảng đáng tin cậy trước: database, authentication/session, authorization, category, product, user và integration checks. Hiện solution build sạch, 202 HTTP checks và 7 database checks đạt.

### Phút 1–3: Mở cây thư mục

> Repo được chia thành API, Core, Infrastructure, database, tests và docs. Core giữ hợp đồng; Infrastructure triển khai và truy cập dữ liệu; API nhận HTTP request và cấu hình framework. Database script được tách vì có nhiều procedure, trigger và locking.

Mở lần lượt:

1. `SalesForecastSystem.sln`.
2. Ba file `.csproj` để chỉ dependency.
3. `Program.cs` để chỉ DI và middleware.

### Phút 3–5: Đi một luồng đăng nhập

Mở theo thứ tự:

1. `AuthController.cs`.
2. `IAuthService.cs`.
3. `AuthService.cs`.
4. `JwtTokenService.cs`.
5. `SessionJwtEvents.cs`.
6. `LoginSession.cs` và `LoginSessionConfiguration.cs`.

Thông điệp: JWT có session server-side nên logout/khóa tài khoản/đổi role có hiệu lực ngay.

### Phút 5–7: Đi một luồng sản phẩm

Mở theo thứ tự:

1. `ProductController.cs`.
2. `ProductRequest.cs` và `ProductQueryRequest.cs`.
3. `IProductService.cs`.
4. `ProductService.cs`.
5. `Product.cs` và `ProductConfiguration.cs`.

Thông điệp: controller mỏng, validation tách rõ, tồn không sửa trực tiếp, cập nhật dùng RowVersion và xóa là soft delete.

### Phút 7–8: Database

Mở `Deploy.ps1`, `006_english_operations.sql`, `003_verify.sql`.

> Các thao tác ảnh hưởng tồn kho đi qua stored procedure có transaction/locking. Ledger là append-only và trigger chặn tồn âm. Script verify chạy rollback nên không để lại dữ liệu nghiệp vụ.

### Phút 8–9: Kiểm thử

Mở `tests/.../Program.cs` và terminal kết quả test.

> Bộ kiểm thử gọi API và SQL Server thật. Hiện 202 HTTP checks và 7 ACID checks đều đạt. Dữ liệu test được dọn sau khi chạy.

### Phút 9–10: Trạng thái và bước tiếp theo

> Phần đã hoàn thiện là nền tảng, Auth, Category, Product và User. Database đã có khung nhập/bán/forecast, nhưng API nhập bán, báo cáo, thuật toán forecast và frontend chưa làm. Việc ngay tiếp theo là chốt các thay đổi thành commit dễ review, dọn solution/file template, sau đó triển khai Warehouse, Supplier và Purchase Order theo cùng lát cắt từ database đến E2E.

## 15. Các câu mentor có thể hỏi

### Vì sao không viết trực tiếp EF Core trong controller?

Để controller chỉ xử lý HTTP. Service chứa nghiệp vụ có thể tái sử dụng và kiểm thử; khi thay route hoặc giao diện, luật nghiệp vụ không bị trộn với HTTP.

### Vì sao cần DTO khi đã có Entity?

Entity phản ánh dữ liệu lưu trữ, còn DTO là hợp đồng với client. DTO giúp validate đầu vào, kiểm soát trường trả ra và ngăn lộ `PasswordHash` hoặc navigation property.

### Vì sao có Interface và Service?

Interface mô tả khả năng hệ thống; implementation mô tả cách thực hiện. API phụ thuộc hợp đồng nên giảm coupling và có thể thay implementation/test double.

### Vì sao không có Repository?

EF Core `DbContext` đã cung cấp Unit of Work và repository-like API. Với phạm vi hiện tại, service dùng DbContext trực tiếp giúp ít abstraction dư thừa. Khi query/nghiệp vụ tăng, có thể tách query service hoặc repository ở nơi thực sự cần.

### Vì sao Database First?

Vì phần database có procedure, trigger, view và concurrency/locking là thành phần cốt lõi. SQL migration giúp kiểm soát chính xác các object này và chạy idempotent trên database hiện có.

### Tại sao JWT còn cần `LoginSessions`?

JWT chỉ kiểm tra chữ ký và thời hạn thì không thu hồi ngay được. `LoginSessions` cho phép logout, khóa user, đổi role hoặc reset password làm token cũ mất hiệu lực ngay.

### Tại sao `RowVersion` cần thiết?

Nó ngăn một người ghi đè thay đổi của người khác. Client phải gửi version vừa đọc; nếu database đã đổi, API trả `409` để client tải lại.

### Phần forecast nằm ở đâu?

Hiện các bảng `ForecastModels`, `ForecastRuns`, `ForecastResults` và constraint đã có trong database. Solution còn khung ML cũ, nhưng pipeline xử lý dữ liệu, model, API và lịch chạy chưa được triển khai. Không nên nói forecast đã hoàn thành.

## 16. Phiên bản nói ngắn trong 90 giây

> Repo của em được chia thành ba project chính. API là cửa vào HTTP và phụ trách JWT, phân quyền, Swagger; Core chứa DTO, interface và kết quả nghiệp vụ dùng chung; Infrastructure triển khai service bằng EF Core, entity mapping và BCrypt. Ngoài ra em tách database script, integration checks và docs.
>
> Khi một request sản phẩm đi vào, Controller nhận request và gọi `IProductService`; `ProductService` xử lý validation nghiệp vụ và truy vấn qua `AppDbContext`; kết quả được `ApiControllerBase` ánh xạ thành status code thống nhất. Entity không được trả thẳng ra client mà đi qua DTO.
>
> Phần database dùng migration SQL vì có view, stored procedure, trigger và locking cho tồn kho. Điều này giúp bảo vệ transaction và chống bán âm ngay tại database. Test project chạy API và SQL Server thật; hiện 202 HTTP checks và 7 database checks đều đạt. Hạn chế hiện tại là thay đổi local chưa được chia commit, solution còn khung ML cũ và các API nhập/bán/forecast chưa hoàn thành.
