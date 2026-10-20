from pathlib import Path
from zipfile import ZipFile
from lxml import etree
import hashlib
root = Path(__file__).resolve().parents[3]
source = root / 'output/docx/Phạm Minh Tài_Báo cáo ngày 01-10-2026.docx'
target = root / 'output/docx/Phạm Minh Tài_Báo cáo ngày 02-10-2026.docx'
original_hash = hashlib.sha256(source.read_bytes()).hexdigest()
updates = {
0: 'BÁO CÁO NGÀY [02/10/2026]',
3: 'Hôm trước: Xây dựng frontend quản lý khách hàng, kết nối API và bổ sung lịch sử đơn hàng.',
4: 'Hôm nay: Hoàn thiện quản lý người dùng, nhập hàng, nhà cung cấp và các thao tác trên đơn hàng.',
6: 'Mục tiêu: Điều chỉnh giao diện React, bổ sung ràng buộc API và chuẩn bị dữ liệu tham khảo cho nghiệp vụ nhập hàng, cụ thể gồm:',
7: 'Quản lý người dùng: Cấp mã ND tự tăng, chỉnh thông tin tài khoản và hiển thị họ tên trên topbar.',
8: 'Ràng buộc tài khoản: Kiểm tra mật khẩu, định dạng email và email trùng khi thêm hoặc sửa.',
9: 'Nhập hàng và nhà cung cấp: Kiểm tra mã số thuế, chỉnh bộ lọc và căn các trường sản phẩm.',
10: 'Đơn hàng và kiểm thử: Hiển thị Sửa, Xóa theo trạng thái; kiểm tra giao diện và API.',
15: 'Hoàn thiện Quản lý Người dùng',
16: 'Tách mã hiển thị ND khỏi ID nội bộ, bắt đầu ND-0001 và tự tăng.',
17: 'Bổ sung thêm, sửa, xóa người dùng với giới hạn bảo vệ tài khoản.',
18: 'Cập nhật tên riêng, tài khoản mẫu và hiển thị họ tên trên email.',
19: 'Đồng bộ ràng buộc mật khẩu và email giữa API với giao diện.',
20: 'Điều chỉnh Nhập hàng và Nhà cung cấp',
21: 'Bắt buộc mã số thuế doanh nghiệp và kiểm tra định dạng, trùng mã.',
22: 'Thêm nút tra cứu Cục Thuế; mở rộng riêng ô chọn nhà cung cấp.',
23: 'Căn các ô nhập hàng theo mã sản phẩm.',
24: 'Sửa ảnh tràn khung, tránh đè chữ của dòng sản phẩm tiếp theo.',
25: 'Đơn hàng và Kiểm tra Kết quả',
26: 'Hiển thị nút Sửa, Xóa; chỉ cho thao tác với đơn Nháp.',
27: 'Tra cứu Digiworld, Synnex FPT và PSD làm dữ liệu tham khảo.',
28: 'Chuẩn bị giá nhập giả lập và làm rõ cách cập nhật tồn kho.',
29: 'Kiểm tra TypeScript, ESLint, build backend và luồng nhập hàng.',
32: 'Hoàn thiện Người dùng và Ràng buộc Tài khoản: Điều chỉnh dữ liệu và biểu mẫu quản lý:',
33: 'Mã người dùng: Cấp ND-0001, ND-0002, ND-0003 cho dữ liệu hiện có; mã tiếp theo ND-0004. Giữ nguyên ID và liên kết dữ liệu.',
34: 'Biểu mẫu và topbar: Mã chỉ đọc; thêm mới chọn Quản lý kho hoặc Nhân viên bán hàng. Có Sửa, Xóa và họ tên phía trên email.',
35: 'Kiểm tra tài khoản: Mật khẩu tối thiểu 12 ký tự, có chữ cái và chữ số, tối đa 72 byte UTF-8; email đúng định dạng, không trùng.',
36: 'Hoàn thiện Nhập hàng và Nhà cung cấp: Điều chỉnh giao diện và kiểm tra dữ liệu khi lưu:',
37: 'Mã số thuế: Bắt buộc 10 số hoặc dạng 10 số-3 số; chặn trùng và thêm liên kết tra cứu Cục Thuế. Không tự xác minh doanh nghiệp.',
38: 'Bố cục sản phẩm: Căn số lượng, giá nhập, thành tiền theo ô mã; giữ ảnh trong khung. Ô lọc nhà cung cấp rộng 360px, nút thêm màu xanh.',
39: 'Thao tác Đơn hàng và Kết quả Kiểm thử: Hoàn thiện hiển thị và kiểm tra các thay đổi:',
40: 'Quy tắc nghiệp vụ: Sửa, Xóa chỉ dùng cho đơn Nháp; trạng thái khác khóa nút. Tồn kho tăng khi xác nhận nhập, không sửa trong Sản phẩm.',
41: 'Kết quả kiểm tra: 10 trường hợp ràng buộc tài khoản và 7 trường hợp mã số thuế đạt; luồng nhập hàng với API giả lập đạt. TypeScript, ESLint và build backend thành công.',
}
ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
with ZipFile(source) as z:
    xml = etree.fromstring(z.read('word/document.xml'))
    paragraphs = xml.xpath('//w:body//w:p', namespaces=ns)
    for index, value in updates.items():
        nodes = paragraphs[index].xpath('.//w:t', namespaces=ns)
        assert nodes
        for node in nodes:
            node.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
        if len(nodes) > 1 and ': ' in value:
            lead, body = value.split(': ', 1)
            nodes[0].text, nodes[1].text = lead + ': ', body
            for node in nodes[2:]: node.text = ''
        else:
            nodes[0].text = value
            for node in nodes[1:]: node.text = ''
    with ZipFile(target, 'w') as out:
        for info in z.infolist():
            out.writestr(info, etree.tostring(xml, xml_declaration=True, encoding='UTF-8', standalone=True) if info.filename == 'word/document.xml' else z.read(info.filename))
with ZipFile(source) as a, ZipFile(target) as b:
    assert a.namelist() == b.namelist()
    for part in a.namelist():
        if part != 'word/document.xml': assert a.read(part) == b.read(part), part
assert hashlib.sha256(source.read_bytes()).hexdigest() == original_hash
print('Created report; template and preserved package parts unchanged.')
