# -*- coding: utf-8 -*-
from __future__ import annotations

import sys
import zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "output/docx/Phạm Minh Tài_Báo cáo ngày 15-10-2026.docx"
TARGET = ROOT / "output/docx/Phạm Minh Tài_Báo cáo ngày 16-10-2026.docx"
NS_W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
W = f"{{{NS_W}}}"

ET.register_namespace("w", NS_W)

def create_run(text: str, bold: bool = False, italic: bool = False, size_pt: float = 13.0, font_name: str = "Times New Roman") -> ET.Element:
    r = ET.Element(W + "r")
    rPr = ET.SubElement(r, W + "rPr")
    
    rFonts = ET.SubElement(rPr, W + "rFonts")
    rFonts.set(W + "ascii", font_name)
    rFonts.set(W + "hAnsi", font_name)
    rFonts.set(W + "cs", font_name)
    
    if bold:
        ET.SubElement(rPr, W + "b")
    if italic:
        ET.SubElement(rPr, W + "i")
        
    sz = ET.SubElement(rPr, W + "sz")
    sz.set(W + "val", str(int(size_pt * 2)))
    szCs = ET.SubElement(rPr, W + "szCs")
    szCs.set(W + "val", str(int(size_pt * 2)))
    
    t = ET.SubElement(r, W + "t")
    t.text = text
    if text.startswith(" ") or text.endswith(" ") or "  " in text:
        t.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    return r

def create_p(runs_data: list[tuple[str, bool, bool]], align: str = "both", space_after_pt: float = 6.0, line_spacing: float = 1.15, size_pt: float = 13.0) -> ET.Element:
    p = ET.Element(W + "p")
    pPr = ET.SubElement(p, W + "pPr")
    
    if align != "left":
        jc = ET.SubElement(pPr, W + "jc")
        jc.set(W + "val", align)
        
    sp = ET.SubElement(pPr, W + "spacing")
    sp.set(W + "before", "0")
    sp.set(W + "after", str(int(space_after_pt * 20)))
    sp.set(W + "line", str(int(line_spacing * 240)))
    sp.set(W + "lineRule", "auto")
    
    for text, bold, italic in runs_data:
        p.append(create_run(text, bold=bold, italic=italic, size_pt=size_pt))
        
    return p

def create_heading(text: str, level: int = 1) -> ET.Element:
    if level == 1:
        return create_p([(text, True, False)], align="left", space_after_pt=6.0, line_spacing=1.15, size_pt=14.0)
    elif level == 2:
        return create_p([(text, True, False)], align="left", space_after_pt=4.0, line_spacing=1.15, size_pt=13.0)
    else:
        return create_p([(text, True, False)], align="left", space_after_pt=3.0, line_spacing=1.15, size_pt=13.0)

def create_bullet(label: str, desc: str, size_pt: float = 13.0) -> ET.Element:
    p = ET.Element(W + "p")
    pPr = ET.SubElement(p, W + "pPr")
    
    ind = ET.SubElement(pPr, W + "ind")
    ind.set(W + "left", "360")
    ind.set(W + "hanging", "360")
    
    sp = ET.SubElement(pPr, W + "spacing")
    sp.set(W + "before", "0")
    sp.set(W + "after", "80")
    sp.set(W + "line", "276")
    sp.set(W + "lineRule", "auto")
    
    jc = ET.SubElement(pPr, W + "jc")
    jc.set(W + "val", "both")
    
    p.append(create_run("• ", bold=True, size_pt=size_pt))
    if label:
        p.append(create_run(label, bold=True, size_pt=size_pt))
    p.append(create_run(desc, bold=False, size_pt=size_pt))
    return p

def create_table(rows_data: list[tuple[str, str]]) -> ET.Element:
    tbl = ET.Element(W + "tbl")
    tblPr = ET.SubElement(tbl, W + "tblPr")
    
    tblW = ET.SubElement(tblPr, W + "tblW")
    tblW.set(W + "w", "0")
    tblW.set(W + "type", "auto")
    
    tblJc = ET.SubElement(tblPr, W + "jc")
    tblJc.set(W + "val", "center")
    
    tblBorders = ET.SubElement(tblPr, W + "tblBorders")
    for border_name in ["top", "left", "bottom", "right", "insideH", "insideV"]:
        b = ET.SubElement(tblBorders, W + border_name)
        b.set(W + "val", "single")
        b.set(W + "sz", "4")
        b.set(W + "space", "0")
        b.set(W + "color", "000000")
        
    tblGrid = ET.SubElement(tbl, W + "tblGrid")
    col1 = ET.SubElement(tblGrid, W + "gridCol")
    col1.set(W + "w", "3200")
    col2 = ET.SubElement(tblGrid, W + "gridCol")
    col2.set(W + "w", "6000")
    
    # Header row
    hdr_tr = ET.SubElement(tbl, W + "tr")
    trPr = ET.SubElement(hdr_tr, W + "trPr")
    ET.SubElement(trPr, W + "tblHeader")
    
    for text, w_val in [("Nhiệm vụ trọng tâm", "3200"), ("Nội dung triển khai chi tiết", "6000")]:
        tc = ET.SubElement(hdr_tr, W + "tc")
        tcPr = ET.SubElement(tc, W + "tcPr")
        tcW = ET.SubElement(tcPr, W + "tcW")
        tcW.set(W + "w", w_val)
        tcW.set(W + "type", "dxa")
        
        tcMar = ET.SubElement(tcPr, W + "tcMar")
        for m_name in ["top", "bottom"]:
            m = ET.SubElement(tcMar, W + m_name)
            m.set(W + "w", "120")
            m.set(W + "type", "dxa")
        for m_name in ["left", "right"]:
            m = ET.SubElement(tcMar, W + m_name)
            m.set(W + "w", "160")
            m.set(W + "type", "dxa")
            
        p = create_p([(text, True, False)], align="center", space_after_pt=2.0, size_pt=12.5)
        tc.append(p)
        
    # Data rows
    for task_title, task_bullets in rows_data:
        tr = ET.SubElement(tbl, W + "tr")
        
        # Col 1
        tc1 = ET.SubElement(tr, W + "tc")
        tcPr1 = ET.SubElement(tc1, W + "tcPr")
        tcW1 = ET.SubElement(tcPr1, W + "tcW")
        tcW1.set(W + "w", "3200")
        tcW1.set(W + "type", "dxa")
        p1 = create_p([(task_title, True, False)], align="left", space_after_pt=2.0, size_pt=12.0)
        tc1.append(p1)
        
        # Col 2
        tc2 = ET.SubElement(tr, W + "tc")
        tcPr2 = ET.SubElement(tc2, W + "tcPr")
        tcW2 = ET.SubElement(tcPr2, W + "tcW")
        tcW2.set(W + "w", "6000")
        tcW2.set(W + "type", "dxa")
        
        bullets = [b.strip() for b in task_bullets.split("\n") if b.strip()]
        for b in bullets:
            p2 = create_p([("– ", True, False), (b, False, False)], align="both", space_after_pt=2.0, size_pt=12.0)
            tc2.append(p2)
            
    return tbl

def build_full_document_xml() -> bytes:
    doc = ET.Element(W + "document")
    body = ET.SubElement(doc, W + "body")
    
    # Title
    body.append(create_p([("BÁO CÁO NGÀY [16/10/2026]", True, False)], align="center", space_after_pt=14.0, size_pt=18.0))
    
    # Metadata
    body.append(create_p([
        ("Dự án: ", True, False),
        ("Hệ thống Dự Báo Nhu Cầu Bán Hàng (Sales Forecast System)", False, False)
    ], space_after_pt=4.0))
    
    body.append(create_p([
        ("Nhóm thực hiện: ", True, False),
        ("2 thành viên", False, False)
    ], space_after_pt=4.0))
    
    body.append(create_p([
        ("Họ tên: ", True, False),
        ("Phạm Minh Tài", False, False)
    ], space_after_pt=6.0))
    
    body.append(create_p([
        ("Hôm trước: ", True, False),
        ("Rà Soát Tồn Kho Đa Kho & Chuyển Đổi Trạng Thái Đơn Hàng, Người Dùng Sang Enum Nghiệp Vụ; Chuẩn Hóa Cấu Hình EF Core Lưu Tên Chuỗi & API Serialization JsonStringEnumConverter; Sửa Đổi Đồng Bộ Entity, DTO, Service Trong Sales/Purchase/User; Core Và Infrastructure Build Thành Công.", False, False)
    ], space_after_pt=6.0))
    
    body.append(create_p([
        ("Hôm nay: ", True, False),
        ("Nghiệm Thu Toàn Diện Đợt Chuyển Đổi Enum, Khắc Phục Lỗi Biên Dịch Toàn Solution .NET 8 (0 Cảnh Báo, 0 Lỗi); Cải Tiến Cơ Chế Bóc Tách Ngoại Lệ SQL Server (Deadlock 1205 & Trùng Khóa 2601/2627) Qua GetBaseException(); Vượt Qua Toàn Bộ 140 Assertions Bộ Kiểm Thử Tích Hợp Excel & Báo Cáo Lợi Nhuận FIFO; Sửa Lỗi Ambiguous Query Trong Bộ Unit Test Frontend Đưa Toàn Bộ 33 Tests Vitest Về Trạng Thái Xanh; Chuẩn Hóa Regex Email Tài Khoản Người Dùng.", False, False)
    ], space_after_pt=6.0))
    
    body.append(create_p([
        ("Mục tiêu: ", True, False),
        ("Tập trung nguồn lực hoàn tất đợt refactor Enum nghiệp vụ, bảo đảm 100% test suites (Backend Integration và Frontend Unit Test) chạy xanh tuyệt đối, loại trừ triệt để nguy cơ deadlock và che giấu lỗi database, sẵn sàng pipeline dữ liệu chuỗi thời gian cho giai đoạn huấn luyện Machine Learning Tuần 9.", False, False)
    ], space_after_pt=10.0))
    
    # Section I
    body.append(create_heading("I. Bảng Phân Công Khối Lượng Công Việc Trong Ngày", level=1))
    
    table_data = [
        (
            "1. Nghiệm thu chuyển đổi Enum nghiệp vụ toàn diện & Đồng bộ Solution .NET 8",
            "Đồng bộ hóa 3 nhóm enum nghiệp vụ SalesOrderStatus, PurchaseOrderStatus, UserStatus trong SalesForecastSystem.Core/Enums.\n"
            "Cấu hình HasConversion<string>() trong EF Core và JsonStringEnumConverter(allowIntegerValues: false) để giữ tương thích ngược 100% với SQL Server và Frontend.\n"
            "Biên dịch thành công toàn bộ SalesForecastSystem.sln và các project kiểm thử (0 cảnh báo, 0 lỗi)."
        ),
        (
            "2. Cải tiến cơ chế bóc tách ngoại lệ Database (GetBaseException) & Chống Deadlock",
            "Nâng cấp hàm nhận diện xung đột IsDatabaseConflict và DatabaseError duyệt cây ngoại lệ gốc qua GetBaseException() trong ExcelService, CustomerService, PurchaseService, SalesService, UserService, WarehouseService.\n"
            "Bóc tách chuẩn xác mã lỗi SqlException 1205 (deadlock), 2601/2627 (trùng khóa), 547 (khóa ngoại) bị EF Core bọc lồng qua nhiều tầng DbUpdateException.\n"
            "Thực hiện rollback giao dịch an toàn và dọn sạch ChangeTracker để ngăn chặn rò rỉ trạng thái lỗi sang request kế tiếp."
        ),
        (
            "3. Kiểm thử tích hợp Excel và Báo cáo lợi nhuận (SalesForecastSystem.ExcelChecks)",
            "Chạy bộ kiểm thử tự động trên cơ sở dữ liệu cô lập disposable database (RunDisposableIntegration.ps1 -ExcelOnly).\n"
            "Vượt qua toàn bộ 140/140 assertions (bao gồm 61 assertions báo cáo lợi nhuận FIFO, bảo toàn số liệu lỗ, phân tích doanh số nhóm nhân viên và import file Excel đồng thời)."
        ),
        (
            "4. Khắc phục lỗi kiểm thử giao diện Frontend (SalesAnalysisPage & Vitest)",
            "Sửa lỗi truy vấn ambiguous text 'Nhân viên A' trong day5-day6.test.tsx do giao diện mới render đồng thời tên nhân viên tại bảng xếp hạng và danh sách chọn nhóm.\n"
            "Toàn bộ 6 test suites (33/33 tests) Vitest chạy xanh tuyệt đối; hoàn tất npm run build (tsc type check và Vite production bundle) trong 5.38s không có lỗi."
        ),
        (
            "5. Chuẩn hóa Regex Email người dùng & Pipeline dữ liệu dự báo chuỗi thời gian",
            "Cập nhật AccountValidation.EmailPattern cho phép khoảng trắng đầu cuối để hỗ trợ cơ chế trim/normalize email trong tầng Service mà không bị Model Binding chặn trước.\n"
            "Rà soát endpoint GET /api/forecast-data/daily, kiểm tra tính toàn vẹn chuỗi dữ liệu bán hàng theo ngày, gán nhãn dữ liệu (Sold, NoSale, StockOut, MissingInventoryHistory) chuẩn bị cho ML."
        )
    ]
    body.append(create_table(table_data))
    body.append(create_p([], space_after_pt=10.0))
    
    # Section II
    body.append(create_heading("II. Chi Tiết Tiến Độ Thực Hiện Công Việc", level=1))
    
    # Task 1
    body.append(create_heading("1. Nghiệm thu chuyển đổi Enum nghiệp vụ toàn diện & Đồng bộ Solution .NET 8", level=2))
    body.append(create_heading("a. Cơ sở lý luận và Rủi ro kỹ thuật:", level=3))
    body.append(create_p([
        ("Trước đây, các trường trạng thái đơn bán hàng, đơn nhập hàng và người dùng đều được lưu và truyền nhận dưới dạng chuỗi tự do (string). Việc sử dụng chuỗi dẫn đến nguy cơ lỗi chính tả (typo), thiếu kiểm tra kiểu dữ liệu tĩnh tại thời điểm biên dịch (compile-time type safety) và khó bảo trì khi mở rộng nghiệp vụ. Tuy nhiên, nếu chuyển sang enum số thông thường (int), hệ thống sẽ làm vỡ dữ liệu lịch sử trong SQL Server, phá vỡ các Stored Procedure, Trigger kiểm tra trạng thái và gây lỗi giao diện Frontend hiện hành.", False, False)
    ]))
    body.append(create_heading("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", level=3))
    body.append(create_p([
        ("Tiến hành thiết lập mô hình enum định kiểu chặt chẽ nhưng duy trì tính tương thích lưu trữ dạng chuỗi:\n", False, False),
        ("– Tạo 3 enum nghiệp vụ trong SalesForecastSystem.Core/Enums: SalesOrderStatus (Draft=1, Pending=2, Delivering=3, Completed=4, Cancelled=5), PurchaseOrderStatus (Draft=1, Posted=2, Cancelled=3) và UserStatus (Active=1, Locked=2).\n", False, False),
        ("– Cấu hình Fluent API EF Core thông qua HasConversion<string>() kết hợp IsUnicode(false) và HasMaxLength(20). Dữ liệu SQL Server vẫn lưu Draft, Pending, Completed nguyên bản, không cần chạy migration thay đổi kiểu cột của bảng.\n", False, False),
        ("– Cấu hình JsonStringEnumConverter(allowIntegerValues: false) tại Program.cs: API tiếp tục truyền nhận tên trạng thái dạng chuỗi và từ chối nghiêm ngặt các giá trị số nguyên không hợp lệ.", False, False)
    ]))
    body.append(create_heading("c. Kết quả thực hiện và Các thành phần triển khai:", level=3))
    body.append(create_bullet("SalesForecastSystem.Core/Enums: ", "Thêm mới 3 tệp enum SalesOrderStatus.cs, PurchaseOrderStatus.cs, UserStatus.cs."))
    body.append(create_bullet("SalesForecastSystem.Infrastructure/Data/Configurations: ", "Cập nhật SalesOrderConfiguration.cs, PurchaseOrderConfiguration.cs, UserConfiguration.cs áp dụng HasConversion<string>()."))
    body.append(create_bullet("SalesForecastSystem.Core/DTOs: ", "Chuyển đổi các trường Status trong SalesRequests/Responses, PurchaseRequests/Responses, UserRequests/Responses sang enum."))
    body.append(create_bullet("SalesForecastSystem.Infrastructure/Services: ", "Đồng bộ logic so sánh và cập nhật trạng thái trong SalesService, PurchaseService, UserService, AuthService, ReportService, ForecastDataService, ExcelService."))
    body.append(create_bullet("Biên dịch: ", "Toàn bộ Solution SalesForecastSystem.sln và các project liên quan biên dịch thành công 100% với .NET 8 SDK (0 warning, 0 error)."))
    body.append(create_heading("d. Đánh giá kết quả theo tiêu chí:", level=3))
    body.append(create_p([
        ("+ Tính an toàn kiểu: Loại bỏ hoàn toàn lỗi gõ chuỗi sai trong code backend C#, bắt lỗi logic ngay từ bước build.\n", False, False),
        ("+ Tính tương thích ngược: Bảo toàn 100% dữ liệu SQL Server, stored procedures, triggers và không làm gián đoạn frontend.", False, False)
    ]))
    
    # Task 2
    body.append(create_heading("2. Cải tiến cơ chế bóc tách ngoại lệ Database (GetBaseException) & Chống Deadlock", level=2))
    body.append(create_heading("a. Cơ sở lý luận và Rủi ro kỹ thuật:", level=3))
    body.append(create_p([
        ("Khi xảy ra xung đột dữ liệu đồng thời hoặc vi phạm ràng buộc tại SQL Server, Entity Framework Core sẽ bọc SqlException bên trong DbUpdateException. Trong các tình huống có SqlServerExecutionStrategy hoặc batch execution, SqlException bị bọc sâu qua nhiều tầng inner exception. Việc chỉ kiểm tra ex.InnerException as SqlException khiến code nhận giá trị null, dẫn đến việc bỏ sót ngoại lệ và ném lỗi 500 Internal Server Error thay vì trả về mã HTTP 409 Conflict hoặc xử lý retry.", False, False)
    ]))
    body.append(create_heading("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", level=3))
    body.append(create_p([
        ("Cải tiến toàn diện cơ chế nhận diện lỗi cơ sở dữ liệu tại tầng Infrastructure:\n", False, False),
        ("– Trong ExcelService: Viết lại hàm IsDatabaseConflict duyệt toàn bộ cây InnerException bằng vòng lặp while/for để phát hiện SqlException ở bất kỳ cấp độ lồng nào, nhận diện chính xác các mã lỗi 2601, 2627 (trùng unique key), 547 (vi phạm foreign key) và 1205, 1222 (deadlock / lock timeout).\n", False, False),
        ("– Trong CustomerService, CategoryService, ProductService, SupplierService, UserService, WarehouseService: Chuyển đổi toàn bộ điều kiện bắt lỗi sang ex.GetBaseException() is SqlException { Number: ... }.\n", False, False),
        ("– Thực hiện rollback transaction và gọi context.ChangeTracker.Clear() ngay khi bắt được conflict, loại bỏ hoàn toàn các entity lỗi khỏi bộ nhớ theo dõi để không làm ảnh hưởng các lệnh lưu tiếp theo.", False, False)
    ]))
    body.append(create_heading("c. Kết quả thực hiện và Các thành phần triển khai:", level=3))
    body.append(create_bullet("ExcelService.cs: ", "Nâng cấp IsDatabaseConflict duyệt đệ quy InnerException, xử lý an toàn transaction deadlock."))
    body.append(create_bullet("CustomerService.cs & UserService.cs: ", "Sử dụng GetBaseException() bắt lỗi 2601/2627 trùng email và 547 vi phạm liên kết."))
    body.append(create_bullet("CategoryService, ProductService, SupplierService, WarehouseService: ", "Chuẩn hóa GetBaseException() cho toàn bộ các thao tác lưu trữ nghiệp vụ."))
    body.append(create_heading("d. Đánh giá kết quả theo tiêu chí:", level=3))
    body.append(create_p([
        ("+ Tính tin cậy hệ thống: Xử lý mượt mà kịch bản tranh chấp ghi đồng thời, phản hồi mã HTTP 409 chuẩn RFC thay vì sập lỗi 500.\n", False, False),
        ("+ Tính toàn vẹn dữ liệu: ChangeTracker luôn sạch sau lỗi, ngăn ngừa tình trạng lưu nhầm dữ liệu bẩn.", False, False)
    ]))

    # Task 3
    body.append(create_heading("3. Kiểm thử tích hợp Excel và Báo cáo lợi nhuận (SalesForecastSystem.ExcelChecks)", level=2))
    body.append(create_heading("a. Cơ sở lý luận và Tính nghiêm ngặt kiểm thử:", level=3))
    body.append(create_p([
        ("Chức năng Import/Export Excel và tính toán báo cáo lợi nhuận kế toán (FIFO Cost of Goods Sold) là module có độ phức tạp cao, đòi hỏi kiểm thử trên môi trường SQL Server thật với dữ liệu biến động liên tục. Cần bảo đảm quy trình import đồng thời 2 file cùng chứa 1 SKU phải có 1 request thành công (200) và 1 request bị từ chối (409) mà không gây deadlock treo hệ thống.", False, False)
    ]))
    body.append(create_heading("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", level=3))
    body.append(create_p([
        ("Thực thi kịch bản kiểm thử độc lập thông qua script RunDisposableIntegration.ps1 -ExcelOnly:\n", False, False),
        ("– Tự động tạo cơ sở dữ liệu kiểm thử cô lập SalesForecastTest_<guid>, nạp đầy đủ các migration từ 001 đến 014.\n", False, False),
        ("– Chạy kiểm tra xác thực quyền truy cập từng vai trò đối với API tải template, xuất file và nhập file Excel.\n", False, False),
        ("– Kiểm tra cơ chế atomic rollback khi import file chứa dữ liệu lỗi hoặc vi phạm trigger.\n", False, False),
        ("– Kiểm tra race condition với 2 tác vụ import đồng thời SKU 'RACE-SKU': kiểm chứng 1 tác vụ 200, 1 tác vụ 409, CSDL chỉ lưu duy nhất 1 sản phẩm.\n", False, False),
        ("– Kiểm tra công thức tính doanh thu, giá vốn hàng bán FIFO và lợi nhuận gộp bảo toàn số âm khi bán lỗ.", False, False)
    ]))
    body.append(create_heading("c. Kết quả thực hiện và Các thành phần triển khai:", level=3))
    body.append(create_bullet("RunDisposableIntegration.ps1: ", "Thực thi kiểm thử trên instance SQL Server Express nội bộ."))
    body.append(create_bullet("SalesForecastSystem.ExcelChecks/Program.cs: ", "Đạt tuyệt đối ALL 140 HTTP AND SQL ASSERTIONS PASSED (trong đó có 61 report assertions)."))
    body.append(create_heading("d. Đánh giá kết quả theo tiêu chí:", level=3))
    body.append(create_p([
        ("+ Độ chính xác nghiệp vụ: Số liệu doanh thu 398đ, giá vốn 400đ và lợi nhuận -2đ khớp chính xác 100% với logic kế toán.\n", False, False),
        ("+ Khả năng chịu tải đồng thời: Cơ chế chống deadlock hoạt động hoàn hảo dưới tải giao dịch song song.", False, False)
    ]))

    # Task 4
    body.append(create_heading("4. Khắc phục lỗi kiểm thử giao diện Frontend (SalesAnalysisPage & Vitest)", level=2))
    body.append(create_heading("a. Cơ sở lý luận và Rủi ro kiểm thử giao diện:", level=3))
    body.append(create_p([
        ("Trong đợt nâng cấp giao diện phân tích doanh số SalesAnalysisPage, ô nhập mã nhân viên dạng chuỗi trước đây đã được thay thế bằng bộ chọn đa chọn (multi-select checkbox directory) có tìm kiếm theo tên. Khi chạy kiểm thử Vitest, hàm findByText('Nhân viên A') bị lỗi ambiguous query do text 'Nhân viên A' xuất hiện đồng thời ở thẻ <td> trong bảng xếp hạng và thẻ <span> trong danh sách checkbox.", False, False)
    ]))
    body.append(create_heading("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", level=3))
    body.append(create_p([
        ("– Cập nhật file kiểm thử frontend/src/test/day5-day6.test.tsx: Thay thế truy vấn đơn lẻ bằng (await screen.findAllByText('Nhân viên A')).length > 0 để kiểm tra sự hiện diện của dữ liệu mà không bị xung đột DOM.\n", False, False),
        ("– Giữ nguyên quy trình mô phỏng người dùng: mở dropdown 'Chọn nhân viên theo tên', click checkbox 'Nhân viên A', click 'Xem doanh số nhóm' và xác nhận URL gọi API chứa đúng tham số StaffUserIds=1.\n", False, False),
        ("– Chạy toàn bộ 6 test files Vitest và kiểm tra build production bằng Vite.", False, False)
    ]))
    body.append(create_heading("c. Kết quả thực hiện và Các thành phần triển khai:", level=3))
    body.append(create_bullet("frontend/src/test/day5-day6.test.tsx: ", "Khắc phục câu lệnh assert, đưa test suite về trạng thái PASS."))
    body.append(create_bullet("frontend/src/features/reports/SalesAnalysisPage.tsx: ", "Bộ chọn nhân viên hoạt động mượt mà, lọc trực quan theo tên."))
    body.append(create_bullet("Kết quả Vitest: ", "6 passed (6 test files), 33 passed (33 tests), thời gian chạy 1.54s."))
    body.append(create_bullet("Kết quả Build: ", "tsc -b && vite build hoàn tất trong 5.38s, tạo gói dist tối ưu với 126 modules transformed."))
    body.append(create_heading("d. Đánh giá kết quả theo tiêu chí:", level=3))
    body.append(create_p([
        ("+ Tính ổn định giao diện: Frontend đồng bộ hoàn hảo với các API Backend sau khi chuyển sang Enum.\n", False, False),
        ("+ Trải nghiệm người dùng (UX): Thao tác chọn nhân viên trực quan, dễ dùng hơn nhiều so với việc nhập ID thủ công.", False, False)
    ]))

    # Task 5
    body.append(create_heading("5. Chuẩn hóa Regex Email người dùng & Pipeline dữ liệu dự báo chuỗi thời gian", level=2))
    body.append(create_heading("a. Cơ sở lý luận và Yêu cầu chuẩn hóa dữ liệu:", level=3))
    body.append(create_p([
        ("Tại DTO tạo người dùng UserWriteRequest, việc sử dụng RegularExpression(EmailPattern) với biểu thức không cho phép khoảng trắng đầu cuối khiến các request gửi kèm khoảng trắng (ví dụ '  user@example.com  ') bị ASP.NET Core Model Binding từ chối trước khi kịp đi vào hàm Trim() trong Service. Đồng thời, để chuẩn bị cho giai đoạn huấn luyện Machine Learning Tuần 9, pipeline dữ liệu chuỗi thời gian cần bảo đảm tính liên tục và phân loại chính xác các ngày không bán hàng.", False, False)
    ]))
    body.append(create_heading("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", level=3))
    body.append(create_p([
        ("– Cập nhật AccountValidation.EmailPattern: Mở rộng biểu thức chính quy thành ^\\s*[^\\s@]+@[^\\s@.]+(?:\\.[^\\s@.]+)+\\s*$ cho phép khoảng trắng đầu cuối hợp lệ, sau đó tầng Service sẽ tự động Trim() và ToLowerInvariant().\n", False, False),
        ("– Rà soát endpoint GET /api/forecast-data/daily: Xác nhận thuật toán tổng hợp số lượng bán theo ngày, phân bổ closingStock, tự động điền giá trị 0 cho ngày không bán, phân loại trạng thái NoSale (còn tồn nhưng không bán) và StockOut (hết tồn kho), chia tập Training/Validation phục vụ kiểm định walk-forward.", False, False)
    ]))
    body.append(create_heading("c. Kết quả thực hiện và Các thành phần triển khai:", level=3))
    body.append(create_bullet("AccountValidation.cs: ", "Chuẩn hóa EmailPattern hỗ trợ khoảng trắng đầu cuối phục vụ normalization."))
    body.append(create_bullet("ForecastDataService.cs & ForecastDataController.cs: ", "Kiểm tra chuỗi dữ liệu chuỗi thời gian theo ngày."))
    body.append(create_bullet("SalesForecastSystem.ML: ", "Module baseline (Moving Average, Seasonal Naive) sẵn sàng nhận dữ liệu."))
    body.append(create_heading("d. Đánh giá kết quả theo tiêu chí:", level=3))
    body.append(create_p([
        ("+ Tính linh hoạt của API: Chấp nhận dữ liệu người dùng nhập có khoảng trắng ngẫu nhiên và chuẩn hóa sạch sẽ.\n", False, False),
        ("+ Tính sẵn sàng dữ liệu: Pipeline time-series đạt chuẩn, sẵn sàng cho bước huấn luyện mô hình ML.", False, False)
    ]))
    
    # Section III
    body.append(create_heading("III. Khó Khăn Gặp Phải Và Giải Pháp Khắc Phục", level=1))
    
    body.append(create_heading("1. Lỗi che giấu ngoại lệ SQL Server do cơ chế bọc lồng (Exception Wrapping) của EF Core khi xử lý Deadlock và Trùng khóa:", level=2))
    body.append(create_p([
        ("Nguy cơ: ", True, False),
        ("Khi chạy kiểm thử import Excel đồng thời, một giao dịch bị SQL Server chọn làm nạn nhân deadlock (lỗi 1205). Tuy nhiên, EF Core đã bọc SqlException sâu 2 tầng bên trong SqlServerExecutionStrategy và DbUpdateException. Code cũ kiểm tra (ex as SqlException ?? ex.InnerException as SqlException) bị trả về null, khiến lỗi không được nhận diện là xung đột dữ liệu mà bị ném ra ngoài dưới dạng HTTP 500 Internal Server Error, làm trượt bài kiểm thử đồng thời.", False, False)
    ]))
    body.append(create_p([
        ("Giải pháp: ", True, False),
        ("Cải tiến logic bắt lỗi bằng cách duyệt đệ quy cây InnerException trong ExcelService và áp dụng ex.GetBaseException() is SqlException sqlEx trong tất cả các service nghiệp vụ (Customer, Product, Category, User, Supplier, Warehouse). Nhờ đó, hệ thống bóc tách chính xác mã lỗi 1205 và 2601/2627 ở mọi cấp độ lồng, tự động bắt lỗi và phản hồi HTTP 409 Conflict chuẩn mực.", False, False)
    ]))
    
    body.append(create_heading("2. Xung đột truy vấn phần tử kiểm thử Frontend do DOM render đồng thời nhiều node trùng văn bản:", level=2))
    body.append(create_p([
        ("Nguy cơ: ", True, False),
        ("Sau khi nâng cấp giao diện phân tích doanh số nhóm với tính năng tìm kiếm và checkbox danh sách nhân viên, text 'Nhân viên A' xuất hiện tại 2 vị trí trên cùng một trang (trong ô bảng xếp hạng và trong nhãn checkbox). Lệnh await screen.findByText('Nhân viên A') trong Vitest ném lỗi 'Found multiple elements with the text', khiến test suite day5-day6 bị fail.", False, False)
    ]))
    body.append(create_p([
        ("Giải pháp: ", True, False),
        ("Điều chỉnh câu lệnh truy vấn trong test thành (await screen.findAllByText('Nhân viên A')).length > 0 để kiểm tra sự tồn tại của dữ liệu mà không bị chặn bởi tính duy nhất, kết hợp sử dụng getByRole('checkbox', ...) khi tương tác chọn nhân viên, đưa 100% các bài test frontend về trạng thái pass.", False, False)
    ]))
    
    body.append(create_heading("3. Kẹt xác thực Model Binding đối với trường Email chứa khoảng trắng đầu/cuối:", level=2))
    body.append(create_p([
        ("Nguy cơ: ", True, False),
        ("Kịch bản kiểm thử tích hợp cố tình gửi payload email có khoảng trắng đầu cuối (ví dụ '  user@example.com  ') để kiểm tra tính năng chuẩn hóa của hệ thống. Biểu thức regex nghiêm ngặt ^[^\\s@]+...$ trước đây đã chặn ngay từ tầng Model Binding với mã HTTP 400 trước khi dữ liệu kịp đi vào hàm Trim() trong Service.", False, False)
    ]))
    body.append(create_p([
        ("Giải pháp: ", True, False),
        ("Cập nhật biểu thức chính quy trong AccountValidation.EmailPattern cho phép chứa khoảng trắng tùy chọn ở hai đầu (^\\s*...\\s*$). Khi request vượt qua tầng Model Validation, tầng Service sẽ thực hiện Trim().ToLowerInvariant() một cách an toàn và nhất quán.", False, False)
    ]))
    
    # Section properties at the end
    sectPr = ET.SubElement(body, W + "sectPr")
    pgSz = ET.SubElement(sectPr, W + "pgSz")
    pgSz.set(W + "w", "11906")
    pgSz.set(W + "h", "16838")
    
    pgMar = ET.SubElement(sectPr, W + "pgMar")
    pgMar.set(W + "top", "1134")
    pgMar.set(W + "right", "1134")
    pgMar.set(W + "bottom", "1134")
    pgMar.set(W + "left", "1418")
    pgMar.set(W + "header", "708")
    pgMar.set(W + "footer", "708")
    pgMar.set(W + "gutter", "0")
    
    return ET.tostring(doc, encoding="utf-8", xml_declaration=True)

def main() -> None:
    new_doc_xml = build_full_document_xml()
    
    with zipfile.ZipFile(SOURCE, "r") as source:
        with zipfile.ZipFile(TARGET, "w") as target:
            for item in source.infolist():
                if item.filename == "word/document.xml":
                    target.writestr(item, new_doc_xml)
                else:
                    target.writestr(item, source.read(item.filename))
                    
    print("SUCCESS_TARGET_CREATED")

if __name__ == "__main__":
    main()
