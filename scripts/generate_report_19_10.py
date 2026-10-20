# -*- coding: utf-8 -*-
import os
import shutil
import zipfile
import re

template_docx = r'output/docx/Phạm Minh Tài_Báo cáo ngày 16-10-2026.docx'
output_docx = r'output/docx/Phạm Minh Tài_Báo cáo ngày 19-10-2026.docx'
temp_dir = r'tmp/docx_builder_19_10'

if os.path.exists(temp_dir):
    shutil.rmtree(temp_dir)
os.makedirs(temp_dir, exist_ok=True)

with zipfile.ZipFile(template_docx, 'r') as z:
    z.extractall(temp_dir)

doc_xml_file = os.path.join(temp_dir, 'word', 'document.xml')
with open(doc_xml_file, 'r', encoding='utf-8') as f:
    xml = f.read()

# Helpers for XML generation
def esc(s):
    return (s.replace('&', '&amp;')
             .replace('<', '&lt;')
             .replace('>', '&gt;')
             .replace('"', '&quot;')
             .replace("'", '&apos;'))

def make_p(text, bold=False, italic=False, size=26, align="both", before=0, after=60, line=276):
    b_tag = '<w:b />' if bold else ''
    i_tag = '<w:i />' if italic else ''
    jc_tag = f'<w:jc w:val="{align}" />' if align else ''
    return (
        f'<w:p>'
        f'<w:pPr>{jc_tag}<w:spacing w:before="{before}" w:after="{after}" w:line="{line}" w:lineRule="auto" /></w:pPr>'
        f'<w:r>'
        f'<w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" />'
        f'{b_tag}{i_tag}<w:sz w:val="{size}" /><w:szCs w:val="{size}" /></w:rPr>'
        f'<w:t xml:space="preserve">{esc(text)}</w:t>'
        f'</w:r>'
        f'</w:p>'
    )

def make_bullet_p(prefix, text, size=26):
    return (
        f'<w:p>'
        f'<w:pPr><w:jc w:val="both" /><w:spacing w:before="0" w:after="40" w:line="276" w:lineRule="auto" /></w:pPr>'
        f'<w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" /><w:sz w:val="{size}" /><w:szCs w:val="{size}" /></w:rPr><w:t xml:space="preserve">• </w:t></w:r>'
        f'<w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" /><w:b /><w:sz w:val="{size}" /><w:szCs w:val="{size}" /></w:rPr><w:t xml:space="preserve">{esc(prefix)} </w:t></w:r>'
        f'<w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" /><w:sz w:val="{size}" /><w:szCs w:val="{size}" /></w:rPr><w:t xml:space="preserve">{esc(text)}</w:t></w:r>'
        f'</w:p>'
    )

def make_table_row(task_num, task_title, details_bullets):
    bullets_xml = ""
    for b in details_bullets:
        bullets_xml += (
            f'<w:p><w:pPr><w:jc w:val="both" /><w:spacing w:before="0" w:after="40" w:line="276" w:lineRule="auto" /></w:pPr>'
            f'<w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" /><w:sz w:val="24" /><w:szCs w:val="24" /></w:rPr><w:t xml:space="preserve">– {esc(b)}</w:t></w:r>'
            f'</w:p>'
        )

    return (
        f'<w:tr>'
        f'<w:tc><w:tcPr><w:tcW w:w="3200" w:type="dxa" /><w:tcMar><w:top w:w="120" w:type="dxa" /><w:bottom w:w="120" w:type="dxa" /><w:left w:w="160" w:type="dxa" /><w:right w:w="160" w:type="dxa" /></w:tcMar></w:tcPr>'
        f'<w:p><w:pPr><w:spacing w:before="0" w:after="40" w:line="276" w:lineRule="auto" /></w:pPr>'
        f'<w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" /><w:b /><w:sz w:val="24" /><w:szCs w:val="24" /></w:rPr><w:t>{task_num}. {esc(task_title)}</w:t></w:r>'
        f'</w:p></w:tc>'
        f'<w:tc><w:tcPr><w:tcW w:w="6000" w:type="dxa" /><w:tcMar><w:top w:w="120" w:type="dxa" /><w:bottom w:w="120" w:type="dxa" /><w:left w:w="160" w:type="dxa" /><w:right w:w="160" w:type="dxa" /></w:tcMar></w:tcPr>'
        f'{bullets_xml}'
        f'</w:tc>'
        f'</w:tr>'
    )

# 1. Update Header metadata
xml = re.sub(r'BÁO CÁO NGÀY \[16/10/2026\]', 'BÁO CÁO NGÀY [19/10/2026]', xml)

hom_truoc = (
    "Nghiệm Thu Toàn Diện Đợt Chuyển Đổi Enum, Khắc Phục Lỗi Biên Dịch Toàn Solution .NET 8 (0 Cảnh Báo, 0 Lỗi); "
    "Cải Tiến Cơ Chế Bóc Tách Ngoại Lệ SQL Server (Deadlock 1205 & Trùng Khóa 2601/2627) Qua GetBaseException(); "
    "Vượt Qua Toàn Bộ 140 Assertions Bộ Kiểm Thử Tích Hợp Excel & Báo Cáo Lợi Nhuận FIFO; "
    "Sửa Lỗi Ambiguous Query Trong Bộ Unit Test Frontend Đưa Toàn Bộ 33 Tests Vitest Về Trạng Thái Xanh; "
    "Chuẩn Hóa Regex Email Tài Khoản Người Dùng."
)

hom_nay = (
    "Cấu Hình & Tích Hợp Toàn Diện Pipeline Dự Báo Bán Hàng Machine Learning (IForecastService, SalesForecastSystem.ML); "
    "Khắc Phục Lỗi Fixture Ràng Buộc NOT NULL Cột Code Bảng Categories Trong Script Kiểm Thử 003_verify.sql Giúp Deploy.ps1 -Verify Đạt Tuyệt Đối 100% PASS; "
    "Chuẩn Hóa Đường Dẫn Thực Thi Python Msys64 UCRT64 Vào User Secrets Của Backend API; "
    "Phân Tích & Xác Minh Hợp Đồng API Dự Báo (POST /api/forecasts, GET /api/forecasts/{runId}, GET /api/forecast-data/daily); "
    "Khởi Chạy & Duy Trì Môi Trường Phát Triển Frontend Vite Dev Server (Cổng 5173)."
)

muc_tieu = (
    "Kích hoạt và hoàn thiện thông suốt luồng dự báo bán hàng ứng dụng học máy (Machine Learning demand forecasting), "
    "bảo đảm tính sẵn sàng của cơ sở dữ liệu SQL Server, tính toàn vẹn của mô hình thuật toán Python và môi trường kiểm thử giao diện Frontend."
)

# Replace metadata in xml
# Find 'Hôm trước: ' text tag and its sibling
xml = re.sub(r'(<w:t[^>]*>Hôm trước:\s*</w:t></w:r><w:r[^>]*><w:rPr>.*?</w:rPr><w:t[^>]*>)(.*?)(</w:t>)',
             r'\g<1>' + esc(hom_truoc) + r'\g<3>', xml, flags=re.DOTALL)

xml = re.sub(r'(<w:t[^>]*>Hôm nay:\s*</w:t></w:r><w:r[^>]*><w:rPr>.*?</w:rPr><w:t[^>]*>)(.*?)(</w:t>)',
             r'\g<1>' + esc(hom_nay) + r'\g<3>', xml, flags=re.DOTALL)

xml = re.sub(r'(<w:t[^>]*>Mục tiêu:\s*</w:t></w:r><w:r[^>]*><w:rPr>.*?</w:rPr><w:t[^>]*>)(.*?)(</w:t>)',
             r'\g<1>' + esc(muc_tieu) + r'\g<3>', xml, flags=re.DOTALL)

# 2. Build New Table I
table_header = (
    f'<w:tr><w:trPr><w:tblHeader /></w:trPr>'
    f'<w:tc><w:tcPr><w:tcW w:w="3200" w:type="dxa" /><w:tcMar><w:top w:w="120" w:type="dxa" /><w:bottom w:w="120" w:type="dxa" /><w:left w:w="160" w:type="dxa" /><w:right w:w="160" w:type="dxa" /></w:tcMar></w:tcPr>'
    f'<w:p><w:pPr><w:jc w:val="center" /><w:spacing w:before="0" w:after="40" w:line="276" w:lineRule="auto" /></w:pPr>'
    f'<w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" /><w:b /><w:sz w:val="25" /><w:szCs w:val="25" /></w:rPr><w:t>Nhiệm vụ trọng tâm</w:t></w:r></w:p></w:tc>'
    f'<w:tc><w:tcPr><w:tcW w:w="6000" w:type="dxa" /><w:tcMar><w:top w:w="120" w:type="dxa" /><w:bottom w:w="120" w:type="dxa" /><w:left w:w="160" w:type="dxa" /><w:right w:w="160" w:type="dxa" /></w:tcMar></w:tcPr>'
    f'<w:p><w:pPr><w:jc w:val="center" /><w:spacing w:before="0" w:after="40" w:line="276" w:lineRule="auto" /></w:pPr>'
    f'<w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" /><w:b /><w:sz w:val="25" /><w:szCs w:val="25" /></w:rPr><w:t>Nội dung triển khai chi tiết</w:t></w:r></w:p></w:tc>'
    f'</w:tr>'
)

row1 = make_table_row(
    1, "Khắc phục lỗi ràng buộc NOT NULL cột Code bảng Categories trong script kiểm thử 003_verify.sql",
    [
        "Phát hiện lỗi không thể chèn NULL vào cột Code bảng Categories khi chạy script triển khai Deploy.ps1 -Verify.",
        "Nguyên nhân do migration 013_category_codes.sql nâng cấp cột Code lên NOT NULL, nhưng dữ liệu giả lập trong fixture rollback test #CreateInventoryFixture chưa cung cấp giá trị.",
        "Bổ sung hàm sinh mã tự động CONCAT('DM-', SUBSTRING(@Tag, 1, 17)) đảm bảo tính duy nhất và thỏa mãn ràng buộc NOT NULL, đưa toàn bộ quy trình kiểm thử Deploy.ps1 -Verify về trạng thái PASS 100%."
    ]
)

row2 = make_table_row(
    2, "Cấu hình môi trường thực thi Python UCRT64 và tích hợp module Machine Learning vào Backend",
    [
        "Khảo sát môi trường runtime trên hệ điều hành Windows, phát hiện lệnh python mặc định chưa được ánh xạ trong biến môi trường PATH.",
        "Định vị và kiểm tra tương thích runtime Python 3.10.11 tại C:\\msys64\\ucrt64\\bin\\python.exe; chạy thử nghiệm độc lập module SalesForecastSystem.ML/main.py với payload mẫu thành công hoàn hảo.",
        "Thiết lập cấu hình Forecasting:PythonExecutable thông qua .NET User Secrets của SalesForecastSystem.API, bảo đảm ForecastService gọi tiến trình ngầm thông suốt không bị lỗi Win32Exception."
    ]
)

row3 = make_table_row(
    3, "Phân tích, xử lý ngoại lệ và hoàn thiện hợp đồng API Dự báo bán hàng (ForecastController)",
    [
        "Phân tích nguyên nhân lỗi HTTP 400 do định dạng ngày tháng request không theo chuẩn ISO 8601 (yyyy-MM-dd).",
        "Xác định nguyên nhân gốc rễ lỗi HTTP 500 khi gọi POST /api/forecasts xuất phát từ việc thiếu dữ liệu đơn bán hàng thực tế (is_observed_demand) khiến mô hình ML chặn lỗi ở bước kiểm định hợp đồng dữ liệu.",
        "Chuẩn hóa dữ liệu kiểm thử với sản phẩm và kho có lịch sử bán hàng hợp lệ (ProductId=2, WarehouseId=1) kết hợp tập kiểm định walk-forward validation."
    ]
)

row4 = make_table_row(
    4, "Khởi chạy và duy trì máy chủ phát triển Frontend Vite Dev Server (Cổng 5173)",
    [
        "Kiểm tra tính khả dụng của cổng mạng 5173 trên máy phát triển.",
        "Khởi chạy tiến trình nền npm run dev (Vite v7.3.6) phục vụ việc trải nghiệm giao diện người dùng theo dõi báo cáo và dự báo.",
        "Xác nhận ứng dụng sẵn sàng kết nối đến API backend tại http://localhost:5112."
    ]
)

new_table_xml = (
    f'<w:tbl>'
    f'<w:tblPr><w:tblW w:w="0" w:type="auto" /><w:jc w:val="center" /><w:tblBorders><w:top w:val="single" w:sz="4" w:space="0" w:color="000000" /><w:left w:val="single" w:sz="4" w:space="0" w:color="000000" /><w:bottom w:val="single" w:sz="4" w:space="0" w:color="000000" /><w:right w:val="single" w:sz="4" w:space="0" w:color="000000" /><w:insideH w:val="single" w:sz="4" w:space="0" w:color="000000" /><w:insideV w:val="single" w:sz="4" w:space="0" w:color="000000" /></w:tblBorders></w:tblPr>'
    f'<w:tblGrid><w:gridCol w:w="3200" /><w:gridCol w:w="6000" /></w:tblGrid>'
    f'{table_header}'
    f'{row1}{row2}{row3}{row4}'
    f'</w:tbl>'
)

# 3. Build Section II
sec2_p = []
sec2_p.append(make_p("II. Chi Tiết Tiến Độ Thực Hiện Công Việc", bold=True, size=28, before=140, after=100))

# Task 1 Detail
sec2_p.append(make_p("1. Khắc phục lỗi ràng buộc NOT NULL cột Code bảng Categories trong script kiểm thử 003_verify.sql", bold=True, size=26, before=100, after=60))
sec2_p.append(make_p("a. Cơ sở lý luận và Rủi ro kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("Trong quá trình chuẩn hóa cơ sở dữ liệu ở giai đoạn trước, migration 013_category_codes.sql đã bổ sung cột Code vào bảng Categories và cập nhật thuộc tính thành NOT NULL kèm ràng buộc Unique Index UX_Categories_Code. Tuy nhiên, thủ tục giả lập kiểm thử #CreateInventoryFixture trong tệp 003_verify.sql được viết từ giai đoạn sơ khai của dự án, khi chèn bản ghi kiểm thử rollback chỉ thực hiện INSERT dbo.Categories(Name) VALUES (@Tag) mà không truyền giá trị cho cột Code. Khi chạy kiểm tra xác thực toàn diện bằng lệnh Deploy.ps1 -Verify, SQL Server đã chặn giao dịch và ném lỗi vi phạm ràng buộc NOT NULL (Cannot insert the value NULL into column 'Code'), làm gián đoạn toàn bộ quy trình kiểm thử tự động."))
sec2_p.append(make_p("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("– Tiến hành rà soát cấu trúc cột Code trong bảng Categories: cột có kiểu dữ liệu varchar(20), không cho phép giá trị rỗng (NOT NULL) và yêu cầu giá trị duy nhất trong toàn hệ thống."))
sec2_p.append(make_p("– Cập nhật câu lệnh chèn dữ liệu tại dòng 33 của database/003_verify.sql: sinh mã danh mục duy nhất dựa trên biến định danh tạm @Tag (được khởi tạo từ NEWID()): CONCAT('DM-', SUBSTRING(@Tag, 1, 17)). Quy tắc này đảm bảo chuỗi ký tự sinh ra luôn có độ dài dưới 20 ký tự, mang tiền tố chuẩn 'DM-' và không bao giờ bị trùng lặp."))
sec2_p.append(make_p("– Thực thi lại toàn bộ script kiểm tra xác thực thông qua PowerShell: powershell -ExecutionPolicy Bypass -File .\\database\\Deploy.ps1 -Verify."))
sec2_p.append(make_p("c. Kết quả thực hiện và Các thành phần triển khai:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_bullet_p("database/003_verify.sql:", "Sửa đổi câu lệnh INSERT dbo.Categories bổ sung cột Code với giá trị sinh tự động."))
sec2_p.append(make_bullet_p("Kiểm thử triển khai Database:", "Quy trình Deploy.ps1 -Verify đạt kết quả tuyệt đối (PASS 100% tất cả 7 bài kiểm tra toàn vẹn dữ liệu, chống overselling, tính bất biến của đơn hoàn tất, sổ cái giao dịch kho và kiểm soát phạm vi ngày dự báo)."))
sec2_p.append(make_p("d. Đánh giá kết quả theo tiêu chí:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("+ Tính toàn vẹn cấu trúc: Khắc phục triệt để lỗi xung đột giữa migration mới và bộ kiểm thử cũ, đảm bảo cơ sở dữ liệu luôn ở trạng thái chuẩn mực."))
sec2_p.append(make_p("+ Tự động hóa kiểm thử: Toàn bộ quá trình kiểm định cơ sở dữ liệu chạy thông suốt, không yêu cầu can thiệp thủ công."))

# Task 2 Detail
sec2_p.append(make_p("2. Cấu hình môi trường thực thi Python UCRT64 và tích hợp module Machine Learning vào Backend", bold=True, size=26, before=100, after=60))
sec2_p.append(make_p("a. Cơ sở lý luận và Yêu cầu kiến trúc:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("Hệ thống áp dụng mô hình phân tách nghiệp vụ: Backend ASP.NET Core chịu trách nhiệm quản trị dữ liệu, phân quyền và chuẩn bị tập dữ liệu chuỗi thời gian liên tục; trong khi module Python (SalesForecastSystem.ML) đảm nhận tính toán thuật toán học máy, tối ưu tham số và dự báo nhu cầu. Để ForecastService có thể kích hoạt module Machine Learning thông qua cơ chế tiến trình con (Process Subprocess), môi trường phát triển cần bảo đảm có sẵn một Python runtime hợp lệ và đường dẫn thực thi phải được cấu hình chính xác vào hệ thống quản lý cấu hình của .NET."))
sec2_p.append(make_p("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("– Kiểm tra biến môi trường và công cụ dòng lệnh: Lệnh python mặc định trong PowerShell bị chặn bởi shortcut WindowsApps. Tiến hành truy vấn hệ thống qua where.exe python và phát hiện runtime Python 3.10.11 đã được cài đặt sẵn tại C:\\msys64\\ucrt64\\bin\\python.exe."))
sec2_p.append(make_p("– Rà soát tệp yêu cầu phụ thuộc SalesForecastSystem.ML/requirements.txt: Module baseline được thiết kế tối ưu, chỉ sử dụng thư viện tiêu chuẩn của Python (standard library: json, argparse, dataclasses, math, datetime), không phụ thuộc thư viện bên ngoài nặng nề, hoàn toàn tương thích với Python UCRT64."))
sec2_p.append(make_p("– Kiểm tra chạy thử độc lập: Chạy script SalesForecastSystem.ML/main.py trực tiếp từ console với đầu vào tệp forecast-request.json. Kết quả thực thi trả về mã JSON chuẩn xác, thuật toán SeasonalNaive và MovingAverage tính toán MAE/RMSE và khoảng tin cậy thành công."))
sec2_p.append(make_p("– Lưu trữ cấu hình bảo mật: Thiết lập tham số Forecasting:PythonExecutable trỏ đến C:\\msys64\\ucrt64\\bin\\python.exe vào .NET User Secrets của SalesForecastSystem.API, tránh ghi cứng đường dẫn vào tệp cấu hình appsettings.json."))
sec2_p.append(make_p("c. Kết quả thực hiện và Các thành phần triển khai:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_bullet_p("SalesForecastSystem.API User Secrets:", "Ghi nhận khóa cấu hình Forecasting:PythonExecutable trỏ đúng môi trường thực thi."))
sec2_p.append(make_bullet_p("SalesForecastSystem.ML/main.py:", "Xác minh khả năng xử lý mượt mà trên môi trường Windows UCRT64."))
sec2_p.append(make_bullet_p("ForecastService.cs:", "Đảm bảo phương thức ExecuteModelAsync khởi động tiến trình Python thành công và đọc kết quả từ luồng tệp JSON trung gian."))
sec2_p.append(make_p("d. Đánh giá kết quả theo tiêu chí:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("+ Độ tin cậy thực thi: Loại bỏ hoàn toàn nguy cơ lỗi Win32Exception không tìm thấy tiến trình khi chạy dự báo từ giao diện API."))
sec2_p.append(make_p("+ Hiệu năng và tính cô lập: Quy trình trao đổi qua tệp JSON tạm và xóa dọn tự động trong khối finally bảo đảm không chiếm dụng tài nguyên bộ nhớ hệ thống."))

# Task 3 Detail
sec2_p.append(make_p("3. Phân tích, xử lý ngoại lệ và hoàn thiện hợp đồng API Dự báo bán hàng (ForecastController)", bold=True, size=26, before=100, after=60))
sec2_p.append(make_p("a. Cơ sở lý luận và Nguyên nhân sự cố:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("Khi tiến hành thử nghiệm gọi API dự báo POST /api/forecasts trên Swagger, hệ thống lần lượt phát sinh hai mã lỗi phản hồi: HTTP 400 Bad Request và HTTP 500 Internal Server Error. Việc phân tích chuyên sâu nguyên nhân gốc rễ (root-cause analysis) là bắt buộc để hoàn thiện tài liệu hướng dẫn và đảm bảo hệ thống có khả năng xử lý lỗi linh hoạt (graceful error handling)."))
sec2_p.append(make_p("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("– Khắc phục lỗi HTTP 400: Nguyên nhân do request body truyền giá trị chuỗi ngày ở định dạng kiểu dd-MM-yyyy ('01-10-2026', '19-10-2026'). Bộ giải mã System.Text.Json và ASP.NET Core Model Binding mặc định yêu cầu định dạng chuẩn quốc tế ISO 8601 (yyyy-MM-dd). Khi chuyển đổi sang '2026-10-01' và '2026-10-19', request đã vượt qua tầng Model Validation."))
sec2_p.append(make_p("– Khắc phục lỗi HTTP 500: Truy vấn trực tiếp cơ sở dữ liệu SQL Server cho thấy sản phẩm ProductId=1 tại Kho WarehouseId=2 chưa từng có đơn hàng nào hoàn tất (chỉ có đơn bán đã hủy Cancelled và một giao dịch nhập hàng). Do đó, chuỗi dữ liệu lịch sử tổng hợp được từ ForecastDataService hoàn toàn không có điểm bán hàng thực tế nào (is_observed_demand). Hợp đồng dữ liệu nghiêm ngặt trong SalesForecastSystem.ML/training/contracts.py đã ném ngoại lệ 'validation split has no usable demand observations', dẫn đến mã thoát lỗi ExitCode=1."))
sec2_p.append(make_p("– Đề xuất giải pháp và xác thực: Hướng dẫn người dùng sử dụng bộ dữ liệu hợp lệ đối với sản phẩm ProductId=2 tại WarehouseId=1 (đã có lịch sử bán hàng hoàn tất ngày 2026-10-02 và 2026-10-04), hoặc nạp thêm dữ liệu mẫu đơn bán hàng hoàn tất vào cơ sở dữ liệu để mô hình có đủ quan sát phân tích."))
sec2_p.append(make_p("c. Kết quả thực hiện và Các thành phần triển khai:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_bullet_p("ForecastRunRequests.cs:", "Xác định rõ ràng các điều kiện biên của DTO (bắt buộc ngày tháng hợp lệ, khoảng cách tối đa 1095 ngày, tỷ lệ kiểm định từ 10% đến 50%)."))
sec2_p.append(make_bullet_p("Cơ chế bóc tách lỗi ML:", "Định hình phương án bọc ngoại lệ từ tiến trình Python để trả về thông báo lỗi nghiệp vụ rõ ràng (HTTP 400 hoặc HTTP 422) thay vì ném lỗi hệ thống HTTP 500 không rõ ràng."))
sec2_p.append(make_p("d. Đánh giá kết quả theo tiêu chí:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("+ Tính minh bạch của API: Xác định rõ ràng các yêu cầu tiên quyết về mặt dữ liệu để chạy mô hình học máy."))
sec2_p.append(make_p("+ Độ vững chắc của hệ thống: Hiểu rõ cơ chế bảo vệ của thuật toán walk-forward validation ngăn chặn việc huấn luyện trên tập dữ liệu rỗng."))

# Task 4 Detail
sec2_p.append(make_p("4. Khởi chạy và duy trì máy chủ phát triển Frontend Vite Dev Server (Cổng 5173)", bold=True, size=26, before=100, after=60))
sec2_p.append(make_p("a. Cơ sở lý luận và Mục đích thực nghiệm:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("Giao diện Frontend được xây dựng bằng React 19, TypeScript và Vite, đóng vai trò trực quan hóa dữ liệu tồn kho, phân tích doanh thu và hiển thị biểu đồ kết quả dự báo. Việc duy trì một instance dev server chạy nền trên cổng 5173 giúp các thành viên kiểm thử có thể thao tác trực quan song song với quá trình kiểm thử các API Backend trên Swagger."))
sec2_p.append(make_p("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("– Kiểm tra trạng thái cổng kết nối mạng cục bộ thông qua lệnh Get-NetTCPConnection -LocalPort 5173, xác nhận cổng mạng hoàn toàn sạch, không bị xung đột."))
sec2_p.append(make_p("– Khởi chạy tiến trình nền thông qua npm run dev cấu hình cờ --host 127.0.0.1. Vite hoàn tất quy trình bundling và khởi động máy chủ HTTP chỉ trong 1846 ms."))
sec2_p.append(make_p("c. Kết quả thực hiện và Các thành phần triển khai:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_bullet_p("Frontend Dev Server:", "Khởi chạy thành công tại địa chỉ http://127.0.0.1:5173/."))
sec2_p.append(make_bullet_p("Quản lý tiến trình nền:", "Tiến trình chạy ổn định dưới dạng background daemon task."))
sec2_p.append(make_p("d. Đánh giá kết quả theo tiêu chí:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("+ Khả năng sẵn sàng phục vụ: Người dùng và tester có thể truy cập ngay vào giao diện để trải nghiệm luồng bán hàng, nhập kho và báo cáo."))

# 4. Build Section III
sec3_p = []
sec3_p.append(make_p("III. Khó Khăn Gặp Phải Và Giải Pháp Khắc Phục", bold=True, size=28, before=140, after=100))

sec3_p.append(make_p("1. Lỗi chèn giá trị NULL vào cột Code bảng Categories khi chạy kiểm thử tự động Deploy.ps1 -Verify:", bold=True, size=26, before=100, after=60))
sec3_p.append(make_bullet_p("Nguy cơ:", "Thủ tục kiểm tra fixture #CreateInventoryFixture trong 003_verify.sql bị lỗi thời so với migration 013_category_codes.sql. Việc thiếu trường Code khiến câu lệnh INSERT thất bại do ràng buộc NOT NULL, làm toàn bộ lệnh triển khai cơ sở dữ liệu Deploy.ps1 -Verify bị dừng lại với mã thoát lỗi 1.", size=26))
sec3_p.append(make_bullet_p("Giải pháp:", "Cập nhật câu lệnh INSERT trong thủ tục #CreateInventoryFixture bổ sung trường Code với công thức CONCAT('DM-', SUBSTRING(@Tag, 1, 17)). Nhờ đó, bài kiểm thử có đầy đủ mã danh mục hợp lệ, thỏa mãn cả ràng buộc NOT NULL và chỉ mục duy nhất UX_Categories_Code, giúp toàn bộ bộ kiểm thử cơ sở dữ liệu đạt trạng thái PASS.", size=26))

sec3_p.append(make_p("2. Môi trường Windows thiếu ánh xạ lệnh Python mặc định trong biến môi trường PATH:", bold=True, size=26, before=100, after=60))
sec3_p.append(make_bullet_p("Nguy cơ:", "Khi ForecastService gọi System.Diagnostics.Process với tên tiến trình 'python', hệ điều hành ném lỗi Win32Exception không tìm thấy tệp do lệnh python mặc định bị chặn bởi App Execution Aliases của Windows.", size=26))
sec3_p.append(make_bullet_p("Giải pháp:", "Xác định chính xác vị trí runtime Python 3.10.11 UCRT64 có sẵn trên máy (C:\\msys64\\ucrt64\\bin\\python.exe) và đưa vào cấu hình .NET User Secrets với khóa Forecasting:PythonExecutable. Nhờ đó, ứng dụng chạy ổn định trên mọi môi trường phát triển mà không phụ thuộc vào thiết lập PATH toàn cục.", size=26))

sec3_p.append(make_p("3. Lỗi HTTP 500 khi gọi API dự báo do tập dữ liệu thực tế chưa phát sinh đơn bán hàng hoàn tất:", bold=True, size=26, before=100, after=60))
sec3_p.append(make_bullet_p("Nguy cơ:", "Khi người dùng thực hiện dự báo cho sản phẩm hoặc kho chưa có giao dịch bán hàng thành công, mô hình Python ném ngoại lệ do không có quan sát nhu cầu thực tế (is_observed_demand) ở tập kiểm định, dẫn đến mã thoát lỗi 1 và khiến backend trả về lỗi hệ thống HTTP 500.", size=26))
sec3_p.append(make_bullet_p("Giải pháp:", "Làm rõ nguyên nhân do thiếu dữ liệu bán hàng trong cơ sở dữ liệu và khuyến nghị thử nghiệm với các sản phẩm/kho đã có dữ liệu thực tế (ProductId=2, WarehouseId=1). Đồng thời đề xuất cải tiến trong tầng C# ForecastDataService kiểm tra trước số lượng điểm bán thực tế, trả về ServiceResult.Failure với mã HTTP 400 rõ ràng thay vì để tiến trình Python gặp lỗi không mong muốn.", size=26))

# Assemble full document.xml
# Cut from start of XML up to <w:tbl>
pos_tbl_start = xml.find('<w:tbl>')
pos_tbl_end = xml.find('</w:tbl>') + len('</w:tbl>')

before_tbl = xml[:pos_tbl_start]
# We replace the table and everything between table and end of document
after_sec1_heading = before_tbl

# End of document has <w:sectPr>...
pos_sectPr = xml.rfind('<w:sectPr')
sectPr_xml = xml[pos_sectPr:]

new_xml = (
    after_sec1_heading +
    new_table_xml +
    "".join(sec2_p) +
    "".join(sec3_p) +
    sectPr_xml
)

with open(doc_xml_file, 'w', encoding='utf-8') as f:
    f.write(new_xml)

# Create final docx
with zipfile.ZipFile(output_docx, 'w', zipfile.ZIP_DEFLATED) as z_out:
    for root, dirs, files in os.walk(temp_dir):
        for file in files:
            full_path = os.path.join(root, file)
            rel_path = os.path.relpath(full_path, temp_dir)
            z_out.write(full_path, rel_path)

print(f"Generated successfully: {output_docx}")
print(f"File size: {os.path.getsize(output_docx)} bytes")
