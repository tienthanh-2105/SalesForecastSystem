# Frontend React + Vite

Frontend cũ trong API vẫn được giữ. React chạy riêng trong development.

## Chạy development

Cài Node LTS kèm npm nếu terminal chưa nhận `npm.cmd`. Không thay đổi execution policy của PowerShell; dùng `npm.cmd`.

Terminal 1 tại gốc repository:

```powershell
dotnet run --project SalesForecastSystem.API --launch-profile http
```

Terminal 2 tại thư mục `frontend`:

```powershell
npm.cmd ci
npm.cmd run dev
```

Mở http://localhost:5173. API chạy tại http://localhost:5112. Muốn đổi target proxy, tạo `.env.local` từ `.env.example`. Không đặt bí mật vào biến frontend.

Trên máy hiện tại chưa có npm trên PATH, có thể chạy `./scripts/StartReact.ps1` từ gốc repository. Script tự dùng runtime Codex có sẵn, không sửa PATH hệ thống. Các lệnh npm khác có thể chạy từ thư mục frontend bằng `../scripts/Npm.ps1 test`, `../scripts/Npm.ps1 run build`.

Nếu terminal chặn file `.ps1`, chạy `powershell -ExecutionPolicy Bypass -File ./scripts/StartReact.ps1`; tùy chọn này chỉ áp dụng cho tiến trình đó, không thay đổi chính sách hệ thống.

## Kiểm tra

```powershell
npm.cmd run lint
npm.cmd test
npm.cmd run build
npx.cmd playwright install chromium
npm.cmd run test:e2e
```

E2E mặc định giả lập API và không thao tác database. Nghiệm thu API thật phải dùng tài khoản/database kiểm thử riêng.

Nếu dùng Chrome đã cài thay cho Chromium tải về: `$env:PW_CHANNEL='chrome'` trước khi chạy test E2E.

## Publish riêng

Từ gốc repository:

```powershell
./scripts/PublishReact.ps1 -Destination E:/SalesForecastPublish
```

Script build React rồi copy vào `wwwroot` của bản publish; không xóa uploads. Chưa triển khai server. Cấu hình connection string/JWT qua biến môi trường hoặc cấu hình riêng của server; user-secrets development không được publish. Chạy API đã publish là đủ, không cần Node server. Không dùng `vite preview` làm production.

Dừng Vite trước khi publish vì `npm ci` cài lại dependency và Windows có thể khóa `esbuild.exe` đang chạy. Thư mục publish phải ở ngoài repository; script từ chối gốc ổ đĩa và source để tránh ghi đè nhầm.

## Khôi phục

Nhánh `codex/progress-2026-10-01`, commit `57528cd` giữ bản cũ. Frontend cũ cũng vẫn còn nguyên trong source API. Khi cần rollback server, dùng bản publish cũ đã lưu; giữ uploads và cấu hình server. Không reset hoặc ghi đè các thay đổi chưa commit.

## Giới hạn hợp đồng hiện tại

Lưu đơn hàng là nhiều request, không phải giao dịch nguyên tử. Nếu lỗi sau khi header được lưu, ứng dụng tải lại đơn theo ID, yêu cầu người dùng kiểm tra các dòng trước khi tiếp tục. Không tự retry mutation. Mã đơn dùng timestamp; mã hệ thống do backend quyết định.

## Kết quả kiểm tra ngày 01-10-2026

- TypeScript, ESLint và build Vite: thành công.
- 15 kiểm thử Vitest: đạt.
- 12 kiểm thử Playwright trên Chrome: đạt, gồm ảnh desktop/mobile, phân quyền, CRUD danh mục/kho, upload/xung đột sản phẩm, URL/Back/Forward và luồng đơn hàng/lưu một phần.
- 380 kiểm tra HTTP backend trên database SQL Server tạm: đạt; database tạm đã được dọn. Harness dùng output riêng để không khóa API đang chạy.
- Publish script: đã chạy thành công vào thư mục tạm riêng. SPA shell tải được khi chưa đăng nhập; URL giao diện trả HTML, API/uploads/assets thiếu trả 404 không phải HTML; assets có hash dùng cache immutable.
- npm audit sau khi nâng Vitest lên bản vá: 0 lỗ hổng.

Kiểm thử trình duyệt dùng API giả lập; kiểm thử HTTP backend chạy riêng trên SQL Server thật với database tạm. Chưa triển khai lên server và chưa thao tác CRUD frontend trên database nghiệp vụ của người dùng.
