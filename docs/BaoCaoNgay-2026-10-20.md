# BÁO CÁO NGÀY [20/10/2026]

- **Dự án:** Hệ thống Dự Báo Nhu Cầu Bán Hàng (Sales Forecast System)
- **Nhóm thực hiện:** 2 thành viên
- **Họ tên:** Phạm Minh Tài
- **Hôm trước:** Cấu Hình & Tích Hợp Toàn Diện Pipeline Dự Báo Bán Hàng Machine Learning (IForecastService, SalesForecastSystem.ML); Khắc Phục Lỗi Fixture Ràng Buộc NOT NULL Cột Code Bảng Categories Trong Script 003_verify.sql Giúp Deploy.ps1 -Verify Đạt 100% PASS; Chuẩn Hóa Đường Dẫn Thực Thi Python Msys64 UCRT64 Vào User Secrets Của Backend API; Phân Tích & Xác Minh Hợp Đồng API Dự Báo (POST /api/forecasts, GET /api/forecasts/{runId}, GET /api/forecast-data/daily); Khởi Chạy & Duy Trì Môi Trường Phát Triển Frontend Vite Dev Server (Cổng 5173).
- **Hôm nay:** Nâng Cấp Cơ Chế Pre-validation & Xử Lý Ngoại Lệ Cho Pipeline Dự Báo Machine Learning (ForecastService, ForecastDataService); Khắc Phục Lỗi Định Tuyến CreatedAtAction Trong ForecastController Bằng CreatedAtRoute('GetForecastRunById'); Xây Dựng Script Nạp Dữ Liệu Chuỗi Thời Gian 79 Ngày database/seed-timeseries-sales.sql Với Quy Luật Chu Kỳ Bán Hàng Tuần Thỏa Mãn Toàn Bộ Triggers; Tích Hợp Kiểm Thử Tự Động Toàn Diện Cho API Dự Báo Vào IntegrationChecks, Đạt Tuyệt Đối 383/383 HTTP Checks PASS và 140/140 Excel Checks PASS; Kiểm Chứng Độ Chính Xác Thuật Toán Walk-Forward Validation (SeasonalNaive & MovingAverage) Đạt 6/6 Python Unit Tests PASS.
- **Mục tiêu:** Hoàn thiện thông suốt và vững chắc toàn bộ luồng huấn luyện, đánh giá và dự báo nhu cầu bán hàng ứng dụng học máy (Machine Learning demand forecasting), chuẩn hóa dữ liệu lịch sử chuỗi thời gian thực tế, loại trừ triệt để lỗi ngoại lệ runtime và đảm bảo tính sẵn sàng 100% trước khi tích hợp giao diện người dùng Frontend.

---

## I. Bảng Phân Công Khối Lượng Công Việc Trong Ngày

| STT | Nhiệm vụ trọng tâm | Nội dung triển khai chi tiết |
| :---: | :--- | :--- |
| **1** | **Nâng cấp cơ chế tiền kiểm định (Pre-validation) và bóc tách ngoại lệ cho luồng Machine Learning Backend** | – Xây dựng cơ chế kiểm tra tiền điều kiện trước khi khởi chạy tiến trình Python trong `ForecastService`: kiểm tra tổng lượng bán thực tế (`totalQuantitySold > 0`), tính hợp lệ của tập Training và Validation.<br>– Phát hiện và ngăn chặn từ sớm trường hợp dữ liệu rỗng hoặc thiếu điểm quan sát nhu cầu (`is_observed_demand`), phản hồi mã lỗi **HTTP 400 (Validation Problem)** kèm thông báo tiếng Việt cụ thể thay vì ném ngoại lệ HTTP 500.<br>– Cải tiến cơ chế bóc tách thông điệp lỗi JSON từ stderr của tiến trình Python trong `ExecuteModelAsync`, giúp API phản hồi chính xác thông tin chẩn đoán lỗi. |
| **2** | **Khắc phục lỗi định tuyến CreatedAtAction trong ForecastController bằng định danh CreatedAtRoute** | – Phát hiện lỗi `System.InvalidOperationException: No route matches the supplied values` khi gọi `POST /api/forecasts` xuất phát từ việc ASP.NET Core tự động loại bỏ hậu tố `Async` trong tên action khi sinh route.<br>– Gán định danh route rõ ràng `Name = "GetForecastRunById"` cho action `GetAsync` trong `ForecastController`.<br>– Chuyển đổi phương thức phản hồi thành `CreatedAtRoute("GetForecastRunById", new { runId = value.ForecastRunId }, value)`, chuẩn hóa 100% đồng bộ với kiến trúc toàn bộ các Controller trong hệ thống. |
| **3** | **Xây dựng kịch bản nạp dữ liệu chuỗi thời gian 79 ngày phục vụ huấn luyện và dự báo (seed-timeseries-sales.sql)** | – Thiết kế script SQL tự động nạp phiếu nhập kho đầu kỳ 1.000 đơn vị (iPhone 15) và 2.000 đơn vị (Pin Anker) đảm bảo tồn kho dương cho Kho 1.<br>– Sinh chuỗi 79 ngày bán hàng liên tục (từ 01/08/2026 đến 18/10/2026) với 92 đơn bán hàng hoàn tất (`Completed`), mô phỏng sát thực tế quy luật chu kỳ 7 ngày (cuối tuần bán cao hơn ngày thường, rải rác ngày `NoSale`).<br>– Tuân thủ nghiêm ngặt các quy tắc ràng buộc của SQL Server: chèn đơn ở trạng thái `Draft` trước khi chuyển `Completed`, thỏa mãn trigger `TR_InventoryTransactions_NonNegativeBalance` và `TR_SalesOrders_Workflow`. |
| **4** | **Bổ sung kiểm thử tích hợp tự động cho API Dự báo và nghiệm thu toàn diện hệ thống** | – Bổ sung kịch bản kiểm thử API `POST /api/forecasts` (trường hợp từ chối dữ liệu rỗng HTTP 400 và trường hợp thành công HTTP 201) cùng `GET /api/forecasts/{runId}` HTTP 200 vào `SalesForecastSystem.IntegrationChecks`.<br>– Cấu hình tự động nhận diện runtime Python UCRT64 qua biến môi trường `Forecasting__PythonExecutable` trong môi trường kiểm thử cô lập.<br>– Chạy quy trình kiểm thử tự động trên cơ sở dữ liệu tạm disposable database: 383/383 HTTP Checks PASS, 140/140 Excel Checks PASS, và 6/6 Python ML Unit Tests PASS. |

---

## II. Chi Tiết Tiến Độ Thực Hiện Công Việc

### 1. Nâng cấp cơ chế tiền kiểm định (Pre-validation) và bóc tách ngoại lệ cho luồng Machine Learning Backend

#### a. Cơ sở lý luận và Rủi ro kỹ thuật:
Mô hình dự báo nhu cầu ứng dụng học máy hoạt động dựa trên kỹ thuật Walk-Forward Validation, yêu cầu tập dữ liệu lịch sử phải có đủ điểm quan sát nhu cầu thực tế (`is_observed_demand`) ở cả hai tập Training và Validation. Nếu người dùng chọn sản phẩm hoặc kho hàng chưa từng phát sinh đơn hàng bán thành công, hoặc trong khoảng thời gian kiểm định hàng hóa bị hết hàng hoàn toàn (`StockOut`), thuật toán Python sẽ không thể tính toán các chỉ số sai số (MAE, RMSE) và ném ngoại lệ `ValueError`. Trước đây, lỗi này khiến tiến trình Python thoát với mã lỗi 1 và làm API Backend ném lỗi hệ thống HTTP 500 không rõ ràng.

#### b. Phân tích thiết kế và Luồng xử lý kỹ thuật:
- Bổ sung tầng kiểm tra tiền điều kiện ngay sau khi `ForecastDataService` trích xuất dữ liệu chuỗi thời gian:
  - **Kiểm tra tổng sản lượng bán:** Nếu `dataset.TotalQuantitySold <= 0`, lập tức trả về `ServiceResult.Failure` với mã lỗi `Validation`, thông báo sản phẩm chưa có phát sinh đơn bán hàng hoàn tất (`Completed`).
  - **Kiểm tra tính sẵn sàng của tập Training:** Kiểm tra có ít nhất một ngày có trạng thái `Sold` hoặc `NoSale` (có tồn kho sẵn sàng bán).
  - **Kiểm tra tính sẵn sàng của tập Validation:** Đảm bảo tập kiểm định có đủ quan sát để đánh giá sai số mô hình cuốn chiếu.
- Cải tiến phương thức `ExecuteModelAsync`: Khi tiến trình Python kết thúc không thành công, tiến hành đọc `stderr` và trích xuất trường `error` từ payload JSON để trả về nguyên nhân cốt lõi cho Client thay vì bọc chuỗi JSON thô trong ngoại lệ.

#### c. Kết quả thực hiện và Các thành phần triển khai:
- `ForecastService.cs`: Bổ sung đầy đủ 3 khối kiểm tra tiền điều kiện và cơ chế bóc tách thông điệp lỗi từ Python.
- Chuẩn hóa phản hồi API: Toàn bộ các vi phạm về dữ liệu đầu vào đều phản hồi mã chuẩn HTTP 400 Bad Request kèm chi tiết trường lỗi (`FieldName`).

#### d. Đánh giá kết quả theo tiêu chí:
- **Độ vững chắc (Robustness):** Triệt tiêu hoàn toàn rủi ro văng lỗi HTTP 500 khi người dùng yêu cầu dự báo cho sản phẩm mới.
- **Trải nghiệm lập trình viên & Client:** Thông báo lỗi rõ nghĩa, giúp giao diện người dùng hiển thị hướng dẫn trực quan.

---

### 2. Khắc phục lỗi định tuyến CreatedAtAction trong ForecastController bằng CreatedAtRoute

#### a. Cơ sở lý luận và Rủi ro kỹ thuật:
Trong chuẩn thiết kế RESTful API, phương thức tạo mới `POST` thành công phải trả về mã HTTP 201 Created kèm Header Location trỏ đến URI lấy thông tin chi tiết của tài nguyên vừa tạo. Ban đầu, `ForecastController` sử dụng `CreatedAtAction(nameof(GetAsync), new { runId = ... })`. Tuy nhiên trong ASP.NET Core, cơ chế định tuyến mặc định tự động cắt bỏ hậu tố `Async` (Async suffix trimming) khỏi tên action, khiến `ActionContext` không thể khớp được route và ném ngoại lệ `InvalidOperationException: No route matches the supplied values`.

#### b. Phân tích thiết kế và Luồng xử lý kỹ thuật:
- Khảo sát quy chuẩn toàn dự án: Nhận thấy tất cả các Controller khác (`ProductController`, `CategoryController`, `SalesController`, `PurchaseController`) đều sử dụng thuộc tính `Name` trên `HttpGet` và trả về thông qua `CreatedAtRoute`.
- Cập nhật `ForecastController`: Bổ sung `Name = "GetForecastRunById"` vào endpoint `[HttpGet("{runId:long}")]`.
- Chuyển đổi phương thức trả về thành `CreatedAtRoute("GetForecastRunById", new { runId = value.ForecastRunId }, value)`.

#### c. Kết quả thực hiện và Các thành phần triển khai:
- `ForecastDataController.cs`: Chuẩn hóa hoàn toàn endpoint `ForecastController` với định danh route rõ ràng.
- Header `Location`: Phản hồi HTTP 201 trả về chính xác Header `Location: /api/forecasts/{runId}` kèm đầy đủ đối tượng `ForecastRunResponse`.

#### d. Đánh giá kết quả theo tiêu chí:
- **Tính nhất quán kiến trúc:** Đảm bảo 100% Controller trong dự án tuân theo cùng một mẫu thiết kế định tuyến RESTful chuẩn.

---

### 3. Xây dựng kịch bản nạp dữ liệu chuỗi thời gian 79 ngày phục vụ huấn luyện và dự báo

#### a. Cơ sở lý luận và Yêu cầu dữ liệu bài toán ML:
Mô hình học máy chuỗi thời gian không thể dự báo chính xác nếu dữ liệu chỉ gồm một vài đơn hàng rải rác. Để thuật toán Seasonal Naive (chu kỳ 7 ngày) và Moving Average (cửa sổ 3, 7, 14 ngày) có thể học được tính quy luật, hệ thống cần một chuỗi dữ liệu lịch sử liên tục, bao gồm cả ngày thường và cuối tuần với đặc trưng phân phối doanh số thực tế.

#### b. Phân tích thiết kế và Luồng xử lý kỹ thuật:
- Viết kịch bản `database/seed-timeseries-sales.sql`:
  - **Phiếu nhập hàng đầu kỳ (`PO-INIT-20260801-01`):** Nhập 1.000 đơn vị iPhone 15 và 2.000 đơn vị Pin dự phòng Anker vào Kho 1 vào ngày 01/08/2026, ghi sổ kho an toàn.
  - **Vòng lặp sinh đơn hàng hàng ngày:** Chạy liên tục từ 01/08/2026 đến 18/10/2026 (79 ngày liên tiếp).
  - **Mô phỏng chu kỳ tuần:** Sử dụng `DATEPART(dw, @Date)`, thiết lập ngày Thứ Bảy và Chủ Nhật có sản lượng bán cao hơn ngày thường từ 2 đến 3 lần; thứ Sáu tăng nhẹ; xen kẽ một vài ngày không bán hàng (`NoSale`).
  - **Tuân thủ toàn vẹn CSDL:** Tạo đơn hàng với trạng thái `Draft`, chèn chi tiết đơn `SalesOrderItems`, sau đó cập nhật `Completed` và ghi nhận xuất kho vào `InventoryTransactions`, thỏa mãn tuyệt đối tất cả các trigger kiểm soát kho.

#### c. Kết quả thực hiện và Các thành phần triển khai:
- `database/seed-timeseries-sales.sql`: Tạo mới kịch bản nạp chuỗi thời gian tự động.
- Dữ liệu thực tế trong `SalesForecastingDB`: Nâng tổng số đơn hoàn tất từ 15 lên 92 đơn và tổng số giao dịch kho lên 207 bản ghi, tạo nền tảng dữ liệu phong phú.

#### d. Đánh giá kết quả theo tiêu chí:
- **Độ tin cậy của thuật toán ML:** Cung cấp đầy đủ dữ liệu cho các mô hình tự động so sánh, tính toán MAE/RMSE và dự báo chuẩn xác.

---

### 4. Bổ sung kiểm thử tích hợp tự động cho API Dự báo và nghiệm thu toàn diện hệ thống

#### a. Cơ sở lý luận và Mục đích kiểm thử:
Để đảm bảo luồng dự báo bán hàng hoạt động trơn tru trong mọi quy trình CI/CD và không phụ thuộc vào dữ liệu cố định của máy phát triển, bộ kiểm thử tích hợp độc lập trên cơ sở dữ liệu tạm (disposable database) cần bao phủ toàn bộ các trường hợp nghiệp vụ của API dự báo.

#### b. Phân tích thiết kế và Luồng xử lý kỹ thuật:
- Cập nhật `SalesForecastSystem.IntegrationChecks/Program.cs`:
  - Cấu hình biến môi trường `Forecasting__PythonExecutable` tự động trỏ đến `C:\msys64\ucrt64\bin\python.exe` khi khởi chạy tiến trình API.
  - Thêm bài kiểm thử từ chối chạy dự báo trên sản phẩm không có đơn bán hàng: `POST /api/forecasts` nhận mã 400.
  - Thêm bài kiểm thử chạy dự báo thành công: `POST /api/forecasts` nhận mã 201, kiểm tra `status='Completed'`, `modelName`, và 7 ngày dự báo.
  - Thêm bài kiểm thử đọc lại kết quả: `GET /api/forecasts/{runId}` nhận mã 200.
- Thực thi toàn bộ quy trình kiểm định tự động:
  - `powershell -ExecutionPolicy Bypass -File tests/RunDisposableIntegration.ps1`
  - `powershell -ExecutionPolicy Bypass -File tests/RunDisposableIntegration.ps1 -ExcelOnly`
  - `python -m unittest discover -s SalesForecastSystem.ML/tests`

#### c. Kết quả thực hiện và Các thành phần triển khai:
- `IntegrationChecks` (Full flow): Đạt **383/383 HTTP Checks PASS 100%**.
- `ExcelChecks` (Reports & FIFO): Đạt **140/140 Assertions PASS 100%**.
- `SalesForecastSystem.ML` Unit Tests: Đạt **6/6 Unit Tests PASS 100%**.

#### d. Đánh giá kết quả theo tiêu chí:
- **Chất lượng mã nguồn:** Toàn bộ hệ thống Backend, Database, Machine Learning và Tests đều ở trạng thái xanh tuyệt đối.

---

## III. Khó Khăn Gặp Phải Và Giải Pháp Khắc Phục

### 1. Lỗi 'No route matches the supplied values' khi phản hồi kết quả CreatedAtAction trong ForecastController
- **Nguy cơ:** Khi người dùng gọi `POST /api/forecasts`, dù dữ liệu đã được tính toán và lưu thành công vào CSDL, API vẫn ném lỗi hệ thống HTTP 500 do ASP.NET Core không tìm thấy route tương ứng với tên action có hậu tố `Async`.
- **Giải pháp:** Bổ sung `Name = "GetForecastRunById"` vào endpoint `[HttpGet]` và chuyển sang sử dụng `CreatedAtRoute("GetForecastRunById", new { runId = value.ForecastRunId }, value)`. Nhờ đó, API phản hồi mã HTTP 201 Created hoàn hảo kèm Header Location chính xác.

### 2. Ràng buộc trigger SQL Server yêu cầu đơn hàng và phiếu nhập mới phải ở trạng thái Draft
- **Nguy cơ:** Khi chạy script nạp chuỗi thời gian, câu lệnh INSERT trực tiếp đơn hàng với trạng thái `Completed` hoặc phiếu nhập `Posted` bị chặn bởi các trigger `TR_SalesOrders_RequireDraftOnInsert` và `TR_PurchaseOrders_RequireDraftOnInsert`.
- **Giải pháp:** Điều chỉnh luồng xử lý trong script SQL: luôn chèn bản ghi ở trạng thái `Draft`, sau đó chèn các chi tiết mặt hàng, rồi mới thực hiện UPDATE chuyển sang `Posted`/`Completed` và ghi sổ `InventoryTransactions`, tuân thủ đúng quy trình nghiệp vụ hệ thống.

### 3. Thiếu dữ liệu bán hàng lịch sử đủ dài để đánh giá tính chu kỳ của mô hình Machine Learning
- **Nguy cơ:** Dữ liệu ban đầu chỉ có một vài đơn hàng phát sinh trong tháng 10, khiến thuật toán Seasonal Naive không thể tìm thấy quan sát cách đó 7 ngày và thuật toán Walk-Forward Validation bị thiếu điểm kiểm định.
- **Giải pháp:** Xây dựng kịch bản `database/seed-timeseries-sales.sql` sinh tự động 79 ngày liên tiếp với 92 đơn hoàn tất, thể hiện rõ quy luật doanh số cuối tuần tăng cao, giúp mô hình Machine Learning đánh giá chính xác chỉ số MAE/RMSE và chọn được mô hình tối ưu.

---

## IV. Kế Hoạch Ngày Tiếp Theo (21/10/2026)

1. **Phát triển giao diện Dự báo bán hàng (Forecast Module) trên Frontend React:**
   - Xây dựng trang `/forecasts` với biểu mẫu chọn Kho hàng, Sản phẩm, khoảng ngày lịch sử, số ngày dự báo và tỷ lệ kiểm định.
   - Trực quan hóa kết quả: Bảng số liệu chi tiết theo từng ngày (sản lượng dự báo, cận dưới, cận trên, doanh thu ước tính) và thẻ thông tin mô hình tối ưu (MAE, RMSE).
2. **Vẽ biểu đồ chuỗi thời gian trực quan:**
   - Tích hợp biểu đồ đường (Line Chart) thể hiện doanh số lịch sử thực tế nối liền với đường dự báo tương lai kèm dải khoảng tin cậy.
3. **Cấu hình menu điều hướng và phân quyền:**
   - Bổ sung liên kết "Dự báo nhu cầu" vào Sidebar và phân quyền truy cập cho vai trò `Admin` và `WarehouseManager`.
