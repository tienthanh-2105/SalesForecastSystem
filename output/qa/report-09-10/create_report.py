from pathlib import Path
from zipfile import ZipFile
from lxml import etree
import hashlib

root_dir = Path.cwd()
ref = root_dir / 'output/docx/Phạm Minh Tài_Báo cáo ngày 08-10-2026.docx'
out = ref.with_name(ref.name.replace('08-10', '09-10'))
ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
slots = {
    0: 'BÁO CÁO NGÀY 09/10/2026',
    3: 'Hôm trước: Hoàn thiện API cảnh báo tồn kho và kiểm thử nghiệp vụ trên database SQL Server tạm.',
    4: 'Hôm nay: Hoàn thiện hộp thoại cảnh báo tự động, thời gian nhắc lại và bố cục theo mẫu danh mục.',
    6: 'Mục tiêu: Giúp người dùng nhận biết kịp thời sản phẩm cần bổ sung hàng:',
    7: 'Tự động hiển thị hộp thoại khi API phát hiện sản phẩm tồn thấp hoặc hết hàng.',
    8: 'Tự đóng sau 15 giây; nhắc lại sau 30 phút nếu cảnh báo vẫn còn.',
    9: 'Mở rộng hộp thoại theo mẫu danh mục, giữ bảng chi tiết dễ đọc.',
    10: 'Tiếp tục kiểm tra dữ liệu tồn kho mỗi 60 giây khi ứng dụng đang mở.',
    15: 'Hạng mục cảnh báo tồn kho',
    16: 'Tự mở hộp thoại khi API có cảnh báo tồn kho.',
    17: 'Hiển thị tồn kho, ngưỡng và lượng bán 30 ngày.',
    18: 'Cho phép đóng hoặc mở lại hộp thoại.',
    19: 'Mở rộng chiều ngang để dễ đọc bảng.',
    20: 'Tự đóng và nhắc lại',
    21: 'Tự đóng sau 15 giây nếu chưa thao tác.',
    22: 'Nhắc lại sau 30 phút nếu cảnh báo vẫn còn.',
    23: 'Không hiện lại nếu cảnh báo đã được xử lý.',
    24: 'Kiểm tra tồn kho tự động mỗi 60 giây.',
    25: 'Build và kiểm tra giao diện',
    26: 'Build TypeScript/Vite thành công.',
    27: 'Giao diện theo mẫu hộp thoại danh mục.',
    30: 'Hộp thoại cảnh báo tồn kho: Hiển thị thông tin cần xử lý ngay khi phát hiện:',
    31: 'Tự động hiển thị: Khi API trả về sản phẩm tồn thấp hoặc hết hàng, giao diện tự mở hộp thoại; người dùng không cần bấm nút để xem lần đầu.',
    32: 'Nội dung và bố cục: Giữ các cột sản phẩm, tồn kho, ngưỡng tối thiểu, lượng bán 30 ngày và trạng thái. Hộp thoại dùng kiểu header, nút đóng như form danh mục và có chiều ngang lớn hơn.',
    33: 'Mở lại: Sau khi đóng, nút Xem cảnh báo tồn kho cho phép mở lại danh sách hiện tại.',
    34: 'Thời gian hiển thị và làm mới: Hạn chế gián đoạn nhưng vẫn cập nhật dữ liệu:',
    35: 'Tự đóng: Bộ đếm hiển thị thời gian còn lại; hộp thoại tự đóng sau 15 giây nếu người dùng chưa đóng trước.',
    36: 'Nhắc lại: Sau khi đóng, chờ 30 phút rồi kiểm tra kết quả mới; chỉ hiện lại nếu cảnh báo vẫn còn. Kiểm tra thủ công có thể mở hộp thoại ngay.',
    37: 'Build và bàn giao: Hoàn thiện frontend và xác nhận biên dịch:',
    38: 'Kết quả kiểm tra: Build TypeScript/Vite thành công sau khi chỉnh giao diện. Kiểm thử tự động cho chu kỳ nhắc lại 30 phút chưa đạt; cần đồng bộ bộ đếm nhắc với chu kỳ truy vấn API rồi chạy lại.',
    39: 'Bước tiếp theo: Kiểm tra trực tiếp luồng tự hiện, tự đóng, đóng thủ công, mở lại và nhắc sau 30 phút trên trình duyệt.'
}

with ZipFile(ref) as src:
    doc = etree.fromstring(src.read('word/document.xml'))
    paragraphs = doc.xpath('//w:p', namespaces=ns)
    for index, value in slots.items():
        nodes = paragraphs[index].xpath('.//w:t', namespaces=ns)
        if not nodes:
            continue
        for node in nodes:
            node.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
        if len(nodes) > 1 and ': ' in value:
            label, body = value.split(': ', 1)
            nodes[0].text = label + ': '
            nodes[1].text = body
            for node in nodes[2:]:
                node.text = ''
        else:
            nodes[0].text = value
            for node in nodes[1:]:
                node.text = ''
    with ZipFile(out, 'w') as dst:
        for part in src.infolist():
            content = etree.tostring(doc, xml_declaration=True, encoding='UTF-8', standalone=True) if part.filename == 'word/document.xml' else src.read(part.filename)
            dst.writestr(part, content)

with ZipFile(ref) as original, ZipFile(out) as updated:
    assert all(original.read(name) == updated.read(name) for name in original.namelist() if name != 'word/document.xml')
print(out.resolve())
