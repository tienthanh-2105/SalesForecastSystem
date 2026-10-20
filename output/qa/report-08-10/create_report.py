from pathlib import Path
from zipfile import ZipFile
from lxml import etree
import hashlib
ref=next(Path('output/docx').glob('*07-10-2026.docx'))
out=ref.with_name(ref.name.replace('07-10','08-10'))
ns={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
slots={
0:'BÁO CÁO NGÀY 08/10/2026',
3:'Hôm trước: Hoàn thiện giao diện Excel, báo cáo lợi nhuận và doanh số nhóm nhân viên.',
4:'Hôm nay: Xây dựng Backend cảnh báo tồn kho thấp, ưu tiên sản phẩm bán chạy.',
6:'Mục tiêu: Xây dựng API cảnh báo sản phẩm sắp hết kho, cụ thể gồm:',7:'',
8:'Tồn kho: Đối chiếu tổng tồn thực tế với ngưỡng tối thiểu của sản phẩm.',
9:'Bán chạy: Xác định top 10 sản phẩm theo số lượng bán trong 30 ngày gần nhất.',
10:'Phân quyền: Cho phép ba vai trò xem cảnh báo sau khi xác thực JWT.',
11:'Kiểm thử: Kiểm tra nghiệp vụ và quyền truy cập trên database SQL Server tạm.',
16:'Xây dựng API cảnh báo',
17:'Bổ sung GET /api/reports/stock-alerts, yêu cầu JWT.',
18:'Cho phép Admin, Quản lý kho và Nhân viên bán hàng xem.',
19:'Trả tồn kho, ngưỡng tối thiểu, lượng bán và trạng thái cảnh báo.',
20:'Chỉ xét sản phẩm đang kinh doanh, không ghi dữ liệu nghiệp vụ.',
21:'Xử lý tồn kho và bán chạy',
22:'Cảnh báo khi tổng tồn tất cả kho bằng hoặc thấp hơn ngưỡng.',
23:'Phân biệt hết hàng và tồn kho thấp; chưa có giao dịch tính là 0.',
24:'Xếp top 10 theo lượng bán từ đơn hoàn tất trong 30 ngày.',
25:'Ưu tiên sản phẩm bán chạy trong danh sách cảnh báo.',
26:'Kiểm thử Backend',
27:'Build Backend thành công, 0 warning và 0 error.',
28:'140/140 kiểm tra HTTP và SQL đạt trên database tạm.',
29:'Kiểm tra phân quyền, ngưỡng tồn, hết hàng và ngừng kinh doanh.',
32:'API cảnh báo tồn kho: Bổ sung endpoint và chuẩn hóa dữ liệu trả về:',
33:'Xác thực và phân quyền: API yêu cầu JWT; cả ba vai trò được xem cảnh báo. Request chưa đăng nhập trả HTTP 401.',
34:'Dữ liệu trả về: Bao gồm sản phẩm, mã SKU, tồn kho, ngưỡng tối thiểu, lượng bán, nhãn bán chạy, trạng thái hết hàng và thời điểm kiểm tra.',
35:'Điều kiện cảnh báo: Chỉ xét sản phẩm đang kinh doanh. Cảnh báo được loại khỏi kết quả khi tồn vượt ngưỡng hoặc sản phẩm ngừng kinh doanh.',
36:'Nghiệp vụ tồn kho và bán chạy: Xử lý dữ liệu từ kho và đơn hàng:',
37:'Tồn kho thực tế: Tổng hợp từ sổ kho trên tất cả kho; chưa có giao dịch tính là 0. Tồn bằng 0 là hết hàng. Đọc tồn và doanh số trong cùng transaction.',
38:'Sản phẩm bán chạy: Top 10 theo lượng bán của đơn Completed trong 30 ngày gồm hôm nay, tính theo ngày đặt đơn và giờ Việt Nam; đồng hạng ưu tiên ID nhỏ hơn.',
39:'Kiểm thử và kế hoạch tiếp theo: Xác nhận Backend và chuẩn bị tích hợp:',
40:'Kết quả kiểm thử: Build Backend đạt; bộ HTTP/SQL đạt 140/140 kiểm tra, gồm phân quyền, tồn bằng ngưỡng, tổng nhiều kho, hết hàng và sản phẩm ngừng kinh doanh.',
41:'Kế hoạch ngày mai: Tích hợp và hoàn thiện giao diện cảnh báo từ API, tự cập nhật dữ liệu và kiểm tra hiển thị trên máy tính, điện thoại.'}
with ZipFile(ref) as z:
 root=etree.fromstring(z.read('word/document.xml'))
 ps=root.xpath('//w:p',namespaces=ns)
 evidence=['Reference: '+str(ref.resolve()),'SHA256: '+hashlib.sha256(ref.read_bytes()).hexdigest(),'Preserve: all package parts except word/document.xml; section geometry, paragraph and run properties, table structure unchanged.','Slots: title, metadata, four objectives, three table rows, three detail groups; replace text only. Remove text in continuation paragraph 7.','Section: '+etree.tostring(root.find('.//w:sectPr',ns)).decode()]
 for i,p in enumerate(ps):
  evidence.append(f'Paragraph {i}: '+etree.tostring(p.find('w:pPr',ns)).decode() if p.find('w:pPr',ns) is not None else f'Paragraph {i}: inherited formatting')
 Path('output/qa/report-08-10/artifact.md').write_text('\n'.join(evidence),encoding='utf-8')
 for i,text in slots.items():
  ts=ps[i].xpath('.//w:t',namespaces=ns)
  if ts:
   ts[0].text=text
   for t in ts[1:]: t.text=''
   if len(ts)>1 and ':' in text:
    label,body=text.split(':',1)
    ts[0].text=label+':'
    ts[1].text=body
    ts[1].set('{http://www.w3.org/XML/1998/namespace}space','preserve')
 with ZipFile(out,'w') as dst:
  for part in z.infolist(): dst.writestr(part,etree.tostring(root,xml_declaration=True,encoding='UTF-8',standalone=True) if part.filename=='word/document.xml' else z.read(part.filename))
with ZipFile(ref) as a,ZipFile(out) as b:
 assert all(a.read(n)==b.read(n) for n in a.namelist() if n!='word/document.xml')
print(out.resolve())


