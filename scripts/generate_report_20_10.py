# -*- coding: utf-8 -*-
import os
import shutil
import zipfile
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

template_docx = r'output/docx/Phạm Minh Tài_Báo cáo ngày 19-10-2026.docx'
if not os.path.exists(template_docx):
    template_docx = r'output/docx/Phạm Minh Tài_Báo cáo ngày 16-10-2026.docx'
output_docx = r'output/docx/Phạm Minh Tài_Báo cáo ngày 20-10-2026.docx'
temp_dir = r'tmp/docx_builder_20_10'

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
xml = re.sub(r'B[ÁA]O C[ÁA]O NG[ÀA]Y \[\d{2}/\d{2}/\d{4}\]', 'BÁO CÁO NGÀY [20/10/2026]', xml)

hom_truoc = (
    "Cấu Hình & Tích Hợp Toàn Diện Pipeline Dự Báo Bán Hàng Machine Learning (IForecastService, SalesForecastSystem.ML); "
    "Khắc Phục Lỗi Fixture Ràng Buộc NOT NULL Cột Code Bảng Categories Trong Script 003_verify.sql Giúp Deploy.ps1 -Verify Đạt 100% PASS; "
    "Chuẩn Hóa Đường Dẫn Thực Thi Python Msys64 UCRT64 Vào User Secrets Của Backend API; "
    "Phân Tích & Xác Minh Hợp Đồng API Dự Báo (POST /api/forecasts, GET /api/forecasts/{runId}, GET /api/forecast-data/daily); "
    "Khởi Chạy & Duy Trì Môi Trường Phát Triển Frontend Vite Dev Server (Cổng 5173)."
)

hom_nay = (
    "Nâng Cấp Cơ Chế Pre-validation & Xử Lý Ngoại Lệ Cho Pipeline Dự Báo Machine Learning (ForecastService, ForecastDataService); "
    "Khắc Phục Lỗi Định Tuyến CreatedAtAction Trong ForecastController Bằng CreatedAtRoute('GetForecastRunById'); "
    "Xây Dựng Script Nạp Dữ Liệu Chuỗi Thời Gian 79 Ngày database/seed-timeseries-sales.sql Với Quy Luật Chu Kỳ Bán Hàng Tuần Thỏa Mãn Toàn Bộ Triggers; "
    "Tích Hợp Kiểm Thử Tự Động Toàn Diện Cho API Dự Báo Vào IntegrationChecks, Đạt Tuyệt Đối 383/383 HTTP Checks PASS và 140/140 Excel Checks PASS; "
    "Kiểm Chứng Độ Chính Xác Thuật Toán Walk-Forward Validation (SeasonalNaive & MovingAverage) Đạt 6/6 Python Unit Tests PASS."
)

muc_tieu = (
    "Hoàn thiện thông suốt và vững chắc toàn bộ luồng huấn luyện, đánh giá và dự báo nhu cầu bán hàng ứng dụng học máy (Machine Learning demand forecasting), "
    "chuẩn hóa dữ liệu lịch sử chuỗi thời gian thực tế, loại trừ triệt để lỗi ngoại lệ runtime và đảm bảo tính sẵn sàng 100% trước khi tích hợp giao diện người dùng Frontend."
)

xml = re.sub(r'(<w:t[^>]*>Hôm tr[^\:]*:\s*</w:t></w:r><w:r[^>]*><w:rPr>.*?</w:rPr><w:t[^>]*>)(.*?)(</w:t>)',
             r'\g<1>' + esc(hom_truoc) + r'\g<3>', xml, flags=re.DOTALL)

xml = re.sub(r'(<w:t[^>]*>Hôm nay:\s*</w:t></w:r><w:r[^>]*><w:rPr>.*?</w:rPr><w:t[^>]*>)(.*?)(</w:t>)',
             r'\g<1>' + esc(hom_nay) + r'\g<3>', xml, flags=re.DOTALL)

xml = re.sub(r'(<w:t[^>]*>Mục ti[^\:]*:\s*</w:t></w:r><w:r[^>]*><w:rPr>.*?</w:rPr><w:t[^>]*>)(.*?)(</w:t>)',
             r'\g<1>' + esc(muc_tieu) + r'\g<3>', xml, flags=re.DOTALL)

# 2. Build New Table I
table_header = (
    f'<w:tr><w:trPr><w:tblHeader /></w:trPr>'
    f'<w:tc><w:tcPr><w:tcW w:w="3200" w:type="dxa" /><w:shd w:fill="F2F2F2" /><w:tcMar><w:top w:w="120" w:type="dxa" /><w:bottom w:w="120" w:type="dxa" /><w:left w:w="160" w:type="dxa" /><w:right w:w="160" w:type="dxa" /></w:tcMar></w:tcPr>'
    f'<w:p><w:pPr><w:jc w:val="center" /><w:spacing w:before="0" w:after="40" w:line="276" w:lineRule="auto" /></w:pPr>'
    f'<w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" /><w:b /><w:sz w:val="24" /><w:szCs w:val="24" /></w:rPr><w:t>Nhiệm vụ trọng tâm</w:t></w:r>'
    f'</w:p></w:tc>'
    f'<w:tc><w:tcPr><w:tcW w:w="6000" w:type="dxa" /><w:shd w:fill="F2F2F2" /><w:tcMar><w:top w:w="120" w:type="dxa" /><w:bottom w:w="120" w:type="dxa" /><w:left w:w="160" w:type="dxa" /><w:right w:w="160" w:type="dxa" /></w:tcMar></w:tcPr>'
    f'<w:p><w:pPr><w:jc w:val="center" /><w:spacing w:before="0" w:after="40" w:line="276" w:lineRule="auto" /></w:pPr>'
    f'<w:r><w:rPr><w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman" /><w:b /><w:sz w:val="24" /><w:szCs w:val="24" /></w:rPr><w:t>Nội dung triển khai chi tiết</w:t></w:r>'
    f'</w:p></w:tc>'
    f'</w:tr>'
)

row1 = make_table_row(
    1, "Nâng cấp cơ chế tiền kiểm định (Pre-validation) và bóc tách ngoại lệ cho luồng Machine Learning Backend",
    [
        "Xây dựng cơ chế kiểm tra tiền điều kiện trước khi khởi chạy tiến trình Python trong ForecastService: kiểm tra tổng lượng bán thực tế (totalQuantitySold > 0), tính hợp lệ của tập Training và Validation.",
        "Phát hiện và ngăn chặn từ sớm trường hợp dữ liệu rỗng hoặc thiếu điểm quan sát nhu cầu (is_observed_demand), phản hồi mã lỗi HTTP 400 (Validation Problem) kèm thông báo tiếng Việt cụ thể thay vì ném ngoại lệ HTTP 500.",
        "Cải tiến cơ chế bóc tách thông điệp lỗi JSON từ stderr của tiến trình Python trong ExecuteModelAsync, giúp API phản hồi chính xác thông tin chẩn đoán lỗi."
    ]
)

row2 = make_table_row(
    2, "Khắc phục lỗi định tuyến CreatedAtAction trong ForecastController bằng định danh CreatedAtRoute",
    [
        "Phát hiện lỗi System.InvalidOperationException: No route matches the supplied values khi gọi POST /api/forecasts xuất phát từ việc ASP.NET Core tự động loại bỏ hậu tố 'Async' trong tên action khi sinh route.",
        "Gán định danh route rõ ràng Name = 'GetForecastRunById' cho action GetAsync trong ForecastController.",
        "Chuyển đổi phương thức phản hồi thành CreatedAtRoute('GetForecastRunById', new { runId = value.ForecastRunId }, value), chuẩn hóa 100% đồng bộ với kiến trúc toàn bộ các Controller trong hệ thống."
    ]
)

row3 = make_table_row(
    3, "Xây dựng kịch bản nạp dữ liệu chuỗi thời gian 79 ngày phục vụ huấn luyện và dự báo (seed-timeseries-sales.sql)",
    [
        "Thiết kế script SQL tự động nạp phiếu nhập kho đầu kỳ 1000 đơn vị (iPhone 15) và 2000 đơn vị (Pin Anker) đảm bảo tồn kho dương cho Kho 1.",
        "Sinh chuỗi 79 ngày bán hàng liên tục (từ 01/08/2026 đến 18/10/2026) với 92 đơn bán hàng hoàn tất (Completed), mô phỏng sát thực tế quy luật chu kỳ 7 ngày (cuối tuần bán cao hơn ngày thường, rải rác ngày NoSale).",
        "Tuân thủ nghiêm ngặt các quy tắc ràng buộc của SQL Server: chèn đơn ở trạng thái Draft trước khi chuyển Completed, thỏa mãn trigger TR_InventoryTransactions_NonNegativeBalance và TR_SalesOrders_Workflow."
    ]
)

row4 = make_table_row(
    4, "Bổ sung kiểm thử tích hợp tự động cho API Dự báo và nghiệm thu toàn diện hệ thống",
    [
        "Bổ sung kịch bản kiểm thử API POST /api/forecasts (trường hợp từ chối dữ liệu rỗng HTTP 400 và trường hợp thành công HTTP 201) cùng GET /api/forecasts/{runId} HTTP 200 vào SalesForecastSystem.IntegrationChecks.",
        "Cấu hình tự động nhận diện runtime Python UCRT64 qua biến môi trường Forecasting__PythonExecutable trong môi trường kiểm thử cô lập.",
        "Chạy quy trình kiểm thử tự động trên cơ sở dữ liệu tạm disposable database: 383/383 HTTP Checks PASS, 140/140 Excel Checks PASS, và 6/6 Python ML Unit Tests PASS."
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
sec2_p.append(make_p("1. Nâng cấp cơ chế tiền kiểm định (Pre-validation) và bóc tách ngoại lệ cho luồng Machine Learning Backend", bold=True, size=26, before=100, after=60))
sec2_p.append(make_p("a. Cơ sở lý luận và Rủi ro kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("Mô hình dự báo nhu cầu ứng dụng học máy hoạt động dựa trên kỹ thuật Walk-Forward Validation, yêu cầu tập dữ liệu lịch sử phải có đủ điểm quan sát nhu cầu thực tế (is_observed_demand) ở cả hai tập Training và Validation. Nếu người dùng chọn sản phẩm hoặc kho hàng chưa từng phát sinh đơn hàng bán thành công, hoặc trong khoảng thời gian kiểm định hàng hóa bị hết hàng hoàn toàn (StockOut), thuật toán Python sẽ không thể tính toán các chỉ số sai số (MAE, RMSE) và ném ngoại lệ ValueError. Trước đây, lỗi này khiến tiến trình Python thoát với mã lỗi 1 và làm API Backend ném lỗi hệ thống HTTP 500 không rõ ràng."))
sec2_p.append(make_p("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("• Bổ sung tầng kiểm tra tiền điều kiện ngay sau khi ForecastDataService trích xuất dữ liệu chuỗi thời gian:"))
sec2_p.append(make_p("  + Kiểm tra tổng sản lượng bán: Nếu dataset.TotalQuantitySold <= 0, lập tức trả về ServiceResult.Failure với mã lỗi Validation, thông báo sản phẩm chưa có phát sinh đơn bán hàng hoàn tất (Completed)."))
sec2_p.append(make_p("  + Kiểm tra tính sẵn sàng của tập Training: Kiểm tra có ít nhất một ngày có trạng thái Sold hoặc NoSale (có tồn kho sẵn sàng bán)."))
sec2_p.append(make_p("  + Kiểm tra tính sẵn sàng của tập Validation: Đảm bảo tập kiểm định có đủ quan sát để đánh giá sai số mô hình cuốn chiếu."))
sec2_p.append(make_p("• Cải tiến phương thức ExecuteModelAsync: Khi tiến trình Python kết thúc không thành công, tiến hành đọc stderr và trích xuất trường 'error' từ payload JSON để trả về nguyên nhân cốt lõi cho Client thay vì bọc chuỗi JSON thô trong ngoại lệ."))
sec2_p.append(make_p("c. Kết quả thực hiện và Các thành phần triển khai:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_bullet_p("ForecastService.cs:", "Bổ sung đầy đủ 3 khối kiểm tra tiền điều kiện và cơ chế bóc tách thông điệp lỗi từ Python."))
sec2_p.append(make_bullet_p("Chuẩn hóa phản hồi API:", "Toàn bộ các vi phạm về dữ liệu đầu vào đều phản hồi mã chuẩn HTTP 400 Bad Request kèm chi tiết trường lỗi (FieldName)."))
sec2_p.append(make_p("d. Đánh giá kết quả theo tiêu chí:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("+ Độ vững chắc (Robustness): Triệt tiêu hoàn toàn rủi ro văng lỗi HTTP 500 khi người dùng yêu cầu dự báo cho sản phẩm mới."))
sec2_p.append(make_p("+ Trải nghiệm lập trình viên & Client: Thông báo lỗi rõ nghĩa, giúp giao diện người dùng hiển thị hướng dẫn trực quan."))

# Task 2 Detail
sec2_p.append(make_p("2. Khắc phục lỗi định tuyến CreatedAtAction trong ForecastController bằng CreatedAtRoute", bold=True, size=26, before=100, after=60))
sec2_p.append(make_p("a. Cơ sở lý luận và Rủi ro kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("Trong chuẩn thiết kế RESTful API, phương thức tạo mới POST thành công phải trả về mã HTTP 201 Created kèm Header Location trỏ đến URI lấy thông tin chi tiết của tài nguyên vừa tạo. Ban đầu, ForecastController sử dụng CreatedAtAction(nameof(GetAsync), new { runId = ... }). Tuy nhiên trong ASP.NET Core, cơ chế định tuyến mặc định tự động cắt bỏ hậu tố 'Async' (Async suffix trimming) khỏi tên action, khiến ActionContext không thể khớp được route và ném ngoại lệ InvalidOperationException: No route matches the supplied values."))
sec2_p.append(make_p("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("• Khảo sát quy chuẩn toàn dự án: Nhận thấy tất cả các Controller khác (ProductController, CategoryController, SalesController, PurchaseController) đều sử dụng thuộc tính Name trên HttpGet và trả về thông qua CreatedAtRoute."))
sec2_p.append(make_p("• Cập nhật ForecastController: Bổ sung Name = 'GetForecastRunById' vào endpoint [HttpGet(\"{runId:long}\")]."))
sec2_p.append(make_p("• Chuyển đổi lệnh trả về thành CreatedAtRoute('GetForecastRunById', new { runId = value.ForecastRunId }, value)."))
sec2_p.append(make_p("c. Kết quả thực hiện và Các thành phần triển khai:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_bullet_p("ForecastDataController.cs:", "Chuẩn hóa hoàn toàn endpoint ForecastController với định danh route rõ ràng."))
sec2_p.append(make_bullet_p("Location Header:", "Phản hồi HTTP 201 trả về chính xác Header Location: /api/forecasts/{runId} kèm đầy đủ đối tượng ForecastRunResponse."))
sec2_p.append(make_p("d. Đánh giá kết quả theo tiêu chí:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("+ Tính nhất quán kiến trúc: Đảm bảo 100% Controller trong dự án tuân theo cùng một mẫu thiết kế định tuyến RESTful chuẩn."))

# Task 3 Detail
sec2_p.append(make_p("3. Xây dựng kịch bản nạp dữ liệu chuỗi thời gian 79 ngày phục vụ huấn luyện và dự báo", bold=True, size=26, before=100, after=60))
sec2_p.append(make_p("a. Cơ sở lý luận và Yêu cầu dữ liệu bài toán ML:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("Mô hình học máy chuỗi thời gian không thể dự báo chính xác nếu dữ liệu chỉ gồm một vài đơn hàng rải rác. Để thuật toán Seasonal Naive (chu kỳ 7 ngày) và Moving Average (cửa sổ 3, 7, 14 ngày) có thể học được tính quy luật, hệ thống cần một chuỗi dữ liệu lịch sử liên tục, bao gồm cả ngày thường và cuối tuần với đặc trưng phân phối doanh số thực tế."))
sec2_p.append(make_p("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("• Viết kịch bản database/seed-timeseries-sales.sql:"))
sec2_p.append(make_p("  + Phiếu nhập hàng đầu kỳ (PO-INIT-20260801-01): Nhập 1000 đơn vị iPhone 15 và 2000 đơn vị Pin dự phòng Anker vào Kho 1 vào ngày 01/08/2026, ghi sổ kho an toàn."))
sec2_p.append(make_p("  + Vòng lặp sinh đơn hàng hàng ngày: Chạy liên tục từ 01/08/2026 đến 18/10/2026 (79 ngày liên tiếp)."))
sec2_p.append(make_p("  + Mô phỏng chu kỳ tuần: Sử dụng DATEPART(dw, @Date), thiết lập ngày Thứ Bảy và Chủ Nhật có sản lượng bán cao hơn ngày thường từ 2 đến 3 lần; thứ Sáu tăng nhẹ; xen kẽ một vài ngày không bán hàng (NoSale)."))
sec2_p.append(make_p("  + Tuân thủ toàn vẹn CSDL: Tạo đơn hàng với trạng thái Draft, chèn chi tiết đơn SalesOrderItems, sau đó cập nhật Completed và ghi nhận xuất kho vào InventoryTransactions, thỏa mãn tuyệt đối tất cả các trigger kiểm soát kho."))
sec2_p.append(make_p("c. Kết quả thực hiện và Các thành phần triển khai:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_bullet_p("database/seed-timeseries-sales.sql:", "Tạo mới kịch bản nạp chuỗi thời gian tự động."))
sec2_p.append(make_bullet_p("Dữ liệu thực tế trong SalesForecastingDB:", "Nâng tổng số đơn hoàn tất từ 15 lên 92 đơn và tổng số giao dịch kho lên 207 bản ghi, tạo nền tảng dữ liệu phong phú."))
sec2_p.append(make_p("d. Đánh giá kết quả theo tiêu chí:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("+ Độ tin cậy của thuật toán ML: Cung cấp đầy đủ dữ liệu cho các mô hình tự động so sánh, tính toán MAE/RMSE và dự báo chuẩn xác."))

# Task 4 Detail
sec2_p.append(make_p("4. Bổ sung kiểm thử tích hợp tự động cho API Dự báo và nghiệm thu toàn diện hệ thống", bold=True, size=26, before=100, after=60))
sec2_p.append(make_p("a. Cơ sở lý luận và Mục đích kiểm thử:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("Để đảm bảo luồng dự báo bán hàng hoạt động trơn tru trong mọi quy trình CI/CD và không phụ thuộc vào dữ liệu cố định của máy phát triển, bộ kiểm thử tích hợp độc lập trên cơ sở dữ liệu tạm (disposable database) cần bao phủ toàn bộ các trường hợp nghiệp vụ của API dự báo."))
sec2_p.append(make_p("b. Phân tích thiết kế và Luồng xử lý kỹ thuật:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("• Cập nhật SalesForecastSystem.IntegrationChecks/Program.cs:"))
sec2_p.append(make_p("  + Cấu hình biến môi trường Forecasting__PythonExecutable tự động trỏ đến C:\\msys64\\ucrt64\\bin\\python.exe khi khởi chạy tiến trình API."))
sec2_p.append(make_p("  + Thêm bài kiểm thử từ chối chạy dự báo trên sản phẩm không có đơn bán hàng: POST /api/forecasts nhận mã 400."))
sec2_p.append(make_p("  + Thêm bài kiểm thử chạy dự báo thành công: POST /api/forecasts nhận mã 201, kiểm tra status='Completed', modelName, và 7 ngày dự báo."))
sec2_p.append(make_p("  + Thêm bài kiểm thử đọc lại kết quả: GET /api/forecasts/{runId} nhận mã 200."))
sec2_p.append(make_p("• Thực thi toàn bộ quy trình kiểm định tự động:"))
sec2_p.append(make_p("  + powershell -ExecutionPolicy Bypass -File tests/RunDisposableIntegration.ps1"))
sec2_p.append(make_p("  + powershell -ExecutionPolicy Bypass -File tests/RunDisposableIntegration.ps1 -ExcelOnly"))
sec2_p.append(make_p("  + python -m unittest discover -s SalesForecastSystem.ML/tests"))
sec2_p.append(make_p("c. Kết quả thực hiện và Các thành phần triển khai:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_bullet_p("IntegrationChecks (Full flow):", "Đạt 383/383 HTTP Checks PASS 100%."))
sec2_p.append(make_bullet_p("ExcelChecks (Reports & FIFO):", "Đạt 140/140 Assertions PASS 100%."))
sec2_p.append(make_bullet_p("SalesForecastSystem.ML Unit Tests:", "Đạt 6/6 Unit Tests PASS 100%."))
sec2_p.append(make_p("d. Đánh giá kết quả theo tiêu chí:", bold=True, italic=True, size=26, before=60, after=40))
sec2_p.append(make_p("+ Chất lượng mã nguồn: Toàn bộ hệ thống Backend, Database, Machine Learning và Tests đều ở trạng thái xanh tuyệt đối."))

# 4. Build Section III
sec3_p = []
sec3_p.append(make_p("III. Khó Khăn Gặp Phải Và Giải Pháp Khắc Phục", bold=True, size=28, before=140, after=100))

sec3_p.append(make_p("1. Lỗi 'No route matches the supplied values' khi phản hồi kết quả CreatedAtAction trong ForecastController:", bold=True, size=26, before=100, after=60))
sec3_p.append(make_bullet_p("Nguy cơ:", "Khi người dùng gọi POST /api/forecasts, dù dữ liệu đã được tính toán và lưu thành công vào CSDL, API vẫn ném lỗi hệ thống HTTP 500 do ASP.NET Core không tìm thấy route tương ứng với tên action có hậu tố Async.", size=26))
sec3_p.append(make_bullet_p("Giải pháp:", "Bổ sung Name = 'GetForecastRunById' vào endpoint [HttpGet] và chuyển sang sử dụng CreatedAtRoute('GetForecastRunById', new { runId = value.ForecastRunId }, value). Nhờ đó, API phản hồi mã HTTP 201 Created hoàn hảo kèm Header Location chính xác.", size=26))

sec3_p.append(make_p("2. Ràng buộc trigger SQL Server yêu cầu đơn hàng và phiếu nhập mới phải ở trạng thái Draft:", bold=True, size=26, before=100, after=60))
sec3_p.append(make_bullet_p("Nguy cơ:", "Khi chạy script nạp chuỗi thời gian, câu lệnh INSERT trực tiếp đơn hàng với trạng thái 'Completed' hoặc phiếu nhập 'Posted' bị chặn bởi các trigger TR_SalesOrders_RequireDraftOnInsert và TR_PurchaseOrders_RequireDraftOnInsert.", size=26))
sec3_p.append(make_bullet_p("Giải pháp:", "Điều chỉnh luồng xử lý trong script SQL: luôn chèn bản ghi ở trạng thái Draft, sau đó chèn các chi tiết mặt hàng, rồi mới thực hiện UPDATE chuyển sang Posted/Completed và ghi sổ InventoryTransactions, tuân thủ đúng quy trình nghiệp vụ hệ thống.", size=26))

sec3_p.append(make_p("3. Thiếu dữ liệu bán hàng lịch sử đủ dài để đánh giá tính chu kỳ của mô hình Machine Learning:", bold=True, size=26, before=100, after=60))
sec3_p.append(make_bullet_p("Nguy cơ:", "Dữ liệu ban đầu chỉ có một vài đơn hàng phát sinh trong tháng 10, khiến thuật toán Seasonal Naive không thể tìm thấy quan sát cách đó 7 ngày và thuật toán Walk-Forward Validation bị thiếu điểm kiểm định.", size=26))
sec3_p.append(make_bullet_p("Giải pháp:", "Xây dựng kịch bản database/seed-timeseries-sales.sql sinh tự động 79 ngày liên tiếp với 92 đơn hoàn tất, thể hiện rõ quy luật doanh số cuối tuần tăng cao, giúp mô hình Machine Learning đánh giá chính xác chỉ số MAE/RMSE và chọn được mô hình tối ưu.", size=26))

# Assemble full document.xml
pos_tbl_start = xml.find('<w:tbl>')
pos_tbl_end = xml.find('</w:tbl>') + len('</w:tbl>')

before_tbl = xml[:pos_tbl_start]
after_sec1_heading = before_tbl

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

with zipfile.ZipFile(output_docx, 'w', zipfile.ZIP_DEFLATED) as z_out:
    for root, dirs, files in os.walk(temp_dir):
        for file in files:
            full_path = os.path.join(root, file)
            rel_path = os.path.relpath(full_path, temp_dir)
            z_out.write(full_path, rel_path)

print(f"Generated successfully: {output_docx}")
print(f"File size: {os.path.getsize(output_docx)} bytes")
