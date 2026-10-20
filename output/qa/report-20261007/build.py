from pathlib import Path
from zipfile import ZipFile
from lxml import etree
import hashlib
root=Path.cwd()
source=root/'output/docx/Phạm Minh Tài_Báo cáo ngày 02-10-2026.docx'
target=root/'output/docx/Phạm Minh Tài_Báo cáo ngày 07-10-2026.docx'
updates={
0:'BÁO CÁO NGÀY [07/10/2026]',
3:'Hôm trước: Backend đã có API Excel ngày 05/10 và báo cáo lợi nhuận, doanh số nhóm nhân viên ngày 06/10.',
4:'Hôm nay: Triển khai frontend cho các API trên, kiểm thử giao diện và khởi động dự án.',
6:'Mục tiêu: Kết nối giao diện React với chức năng backend đã triển khai ngày 05 và 06/10, cụ thể gồm:',
7:'Excel: Tải mẫu, xuất theo bộ lọc, kiểm tra file và nhập dữ liệu qua API.',
8:'Lợi nhuận: Hiển thị doanh thu, giá vốn FIFO, lợi nhuận gộp và chi tiết theo kỳ.',
9:'Nhóm nhân viên: Tổng hợp doanh số theo các thành viên được chọn và bộ lọc chung.',
10:'Kiểm thử: Kiểm tra build, lint, unit test, giao diện và khả năng khởi động ứng dụng.',
15:'Kết nối chức năng Excel',
16:'Chuyển tải mẫu và xuất Excel sang API backend, kèm JWT.',
17:'Gửi file multipart/form-data để kiểm tra trước khi nhập.',
18:'Hiển thị lỗi theo dòng, cột; chỉ bật nhập khi file hợp lệ.',
19:'Chặn gửi lặp và tải lại danh sách sau khi nhập thành công.',
20:'Triển khai trang Báo cáo',
21:'Thêm mục Báo cáo tại /reports cho cả ba vai trò.',
22:'Lọc ngày, chu kỳ, kho, sản phẩm, danh mục và nhân viên.',
23:'Hiển thị lợi nhuận theo kỳ và cảnh báo thiếu giá vốn.',
24:'Chọn nhóm nhân viên, xem tổng và chi tiết từng người.',
25:'Kiểm thử và Chạy dự án',
26:'Sửa điều hướng sau đăng nhập để trở lại trang /reports.',
27:'Build, lint và 29/29 unit test đạt.',
28:'17/17 kiểm thử giao diện trên Microsoft Edge đạt.',
29:'Khởi động API, React; trang frontend và Swagger trả HTTP 200.',
32:'Tích hợp Excel với Backend: Hoàn thiện luồng kiểm tra và lưu dữ liệu từ giao diện:',
33:'Tải và xuất file: Gọi API template/export bằng JWT; xuất toàn bộ dữ liệu khớp bộ lọc danh sách sản phẩm hoặc đơn hàng.',
34:'Kiểm tra và lưu: Nhận file .xlsx tối đa 5 MB, 2000 dòng; gọi import/preview, hiển thị lỗi dòng/cột rồi cho phép import khi hợp lệ.',
35:'Xử lý trạng thái: Đổi file hoặc kiểm tra lại xóa kết quả cũ; chặn gửi lặp, hiển thị lỗi API và làm mới danh sách sau khi lưu.',
36:'Báo cáo Lợi nhuận và Nhóm Nhân viên: Bổ sung trang báo cáo và quyền truy cập theo API:',
37:'Lợi nhuận gộp: Hiển thị doanh thu, giá vốn FIFO, lợi nhuận, số đơn và số lượng theo kỳ. Thiếu giá vốn hiển thị Chưa xác định; chưa trừ chi phí vận hành, thuế.',
38:'Nhóm nhân viên: Nhập tên nhóm và mã thành viên; loại mã lặp, tổng hợp doanh số và chi tiết từng người. Nhóm chỉ dùng cho lần báo cáo hiện tại.',
39:'Kiểm thử và Khởi động Ứng dụng: Kiểm tra thay đổi, cập nhật tài liệu và chạy dự án:',
40:'Kết quả kiểm thử: Build TypeScript/Vite và ESLint đạt; 29/29 unit test, 17/17 kiểm thử trình duyệt Edge đạt. Các kiểm thử frontend sử dụng API mô phỏng.',
41:'Vận hành và bước tiếp theo: API cổng 5112 kết nối database; React cổng 5173 và Swagger trả HTTP 200. Tiếp tục nghiệm thu Excel, báo cáo trên database kiểm thử thật.',
}
ns={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
original_hash=hashlib.sha256(source.read_bytes()).hexdigest()
with ZipFile(source) as z:
 x=etree.fromstring(z.read('word/document.xml'))
 paragraphs=x.xpath('//w:body//w:p',namespaces=ns)
 for index,value in updates.items():
  nodes=paragraphs[index].xpath('.//w:t',namespaces=ns)
  assert nodes
  for node in nodes: node.set('{http://www.w3.org/XML/1998/namespace}space','preserve')
  if len(nodes)>1 and ': ' in value:
   lead,body=value.split(': ',1);nodes[0].text=lead+': ';nodes[1].text=body
   for node in nodes[2:]:node.text=''
  else:
   nodes[0].text=value
   for node in nodes[1:]:node.text=''
 for node in paragraphs[31].xpath('./w:pPr/w:pageBreakBefore',namespaces=ns): node.getparent().remove(node)
 with ZipFile(target,'w') as out:
  for info in z.infolist():out.writestr(info,etree.tostring(x,xml_declaration=True,encoding='UTF-8',standalone=True) if info.filename=='word/document.xml' else z.read(info.filename))
with ZipFile(source) as a,ZipFile(target) as b:
 for part in a.namelist():
  if part!='word/document.xml':assert a.read(part)==b.read(part),part
assert hashlib.sha256(source.read_bytes()).hexdigest()==original_hash
contract=f'''# Daily report template contract
Reference: {source}; SHA256 {original_hash}. Three reference page PNGs in output/qa/report-20261002/final-1.png through final-3.png inspected.
Preserve the reference package except document.xml. A4 portrait section, margins, Times New Roman, title, table, list styles and all paragraph/run formatting unchanged.
Slots: paragraph 0 date; 3 previous work; 4 today; 6 purpose; 7–10 objectives; 15–29 three table groups; 32–41 detail groups. Preserve paragraphs 2, 12–14, 31 and blank slots.
Coverage: frontend Excel API integration, profit and staff group reports, 29 unit tests, 17 mocked browser tests, actual startup and HTTP 200. Do not claim live database business integration testing.
Renderer fallback: packaged render_docx.py cannot find soffice.exe. Use Word COM read-only PDF export and invoke packaged rasterize with the exported PDF, without saving reference or target from Word.
'''
(root/'output/qa/report-20261007/artifact.md').write_text(contract,encoding='utf-8')
print(target)

