from pathlib import Path
from zipfile import ZipFile
from copy import deepcopy
from lxml import etree
root = Path(__file__).resolve().parents[3]
source = root / 'output/docx/Phạm Minh Tài_Báo cáo ngày 02-10-2026.docx'
target = root / 'output/docx/Phạm Minh Tài_Báo cáo ngày 02-10-2026_Chi tiết.docx'
ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
groups = [
('Hoàn thiện chức năng quản lý người dùng', [
('Biểu mẫu thêm người dùng', 'Bổ sung hộp thoại nhập họ tên, email, số điện thoại, vai trò, mật khẩu và xác nhận mật khẩu. Các trường bắt buộc có dấu sao đỏ; hai ô mật khẩu có cùng kích thước. Có lựa chọn hiển thị mật khẩu và kiểm tra mật khẩu xác nhận phải khớp.'),
('Vai trò và thao tác', 'Khi thêm mới chỉ cho chọn Quản lý kho hoặc Nhân viên bán hàng. Loại bỏ dòng thông báo trạng thái mặc định khỏi biểu mẫu. Bổ sung thao tác sửa thông tin và xóa tài khoản trong danh sách người dùng.'),
('Bảo vệ dữ liệu', 'Không cho xóa quản trị viên hoặc tài khoản đang đăng nhập. Người dùng có lịch sử đơn bán hàng, phiếu nhập hay dự báo không được xóa để tránh mất liên kết nghiệp vụ. Xử lý dữ liệu phiên đăng nhập khi xóa tài khoản được phép.')]),
('Chuẩn hóa mã và dữ liệu tài khoản', [
('Mã người dùng tự động', 'Tách mã hiển thị ND khỏi UserId nội bộ. Bổ sung số thứ tự và bộ cấp số trong SQL Server, sinh mã ND-0001, ND-0002, ND-0003 cho ba tài khoản hiện có. Mã tiếp theo tại thời điểm kiểm tra là ND-0004; người dùng không được tự nhập mã.'),
('Giữ liên kết dữ liệu', 'Không thay đổi khóa chính UserId và các khóa ngoại đã liên kết với đơn hàng. Cập nhật API danh sách, chi tiết và mã gợi ý; frontend hiển thị mã lưu trong database thay vì ghép theo ID nội bộ.'),
('Bộ tài khoản thực tập', 'Chuẩn hóa ba tài khoản theo tên riêng Nguyễn Văn Khánh, Trần Minh Huy và Lê Hoàng Nam, tương ứng quản trị viên, quản lý kho và nhân viên bán hàng. Cập nhật mật khẩu hai tài khoản nghiệp vụ theo yêu cầu, lưu bằng BCrypt và thu hồi các phiên cũ; giữ nguyên mật khẩu quản trị viên.')]),
('Bổ sung ràng buộc mật khẩu và email', [
('Mật khẩu trên API', 'Áp dụng cho thêm người dùng và đặt lại mật khẩu: tối thiểu 12 ký tự, có ít nhất một chữ cái hoa hoặc thường và một chữ số; tối đa 72 byte UTF-8. Không bắt buộc đồng thời có cả chữ hoa và chữ thường. Kiểm tra tương tự khi khởi tạo quản trị viên mới.'),
('Email và tính duy nhất', 'Email bắt buộc, đúng định dạng có tên miền, tối đa 100 ký tự. Khi lưu, chuẩn hóa email về chữ thường và bỏ khoảng trắng ở hai đầu. Giữ kiểm tra email trùng và ràng buộc duy nhất trong database khi thêm hoặc sửa tài khoản.'),
('Đồng bộ giao diện', 'Bổ sung kiểm tra chữ cái, chữ số và độ dài mật khẩu trên form; hiển thị hướng dẫn nhập và lỗi tại trường tương ứng. Email sai định dạng được báo trước khi gửi. API vẫn kiểm tra độc lập, không phụ thuộc kiểm tra ở trình duyệt.')]),
('Hoàn thiện giao diện và luồng nhập hàng', [
('Phiếu nhập và lưu dữ liệu', 'Hoàn thiện màn hình danh sách và hộp thoại phiếu nhập trong frontend React: chọn kho, nhà cung cấp, ngày nhập, ghi chú và nhiều dòng sản phẩm. Mỗi dòng có mã SP, tên tự hiển thị, ảnh, số lượng, giá nhập và thành tiền; tổng tiền tính từ các dòng.'),
('Lưu nháp và xác nhận', 'Giữ quy trình lưu thông tin phiếu rồi lưu các dòng hàng. Khi lưu một phần thất bại, tải lại phiếu đã tạo theo ID để tiếp tục, không tạo thêm phiếu mới. Phiếu nháp chưa tăng tồn kho; chỉ thao tác xác nhận nhập kho mới ghi nhận hàng vào kho.'),
('Căn chỉnh và sửa lỗi', 'Cho mã và tên sản phẩm hiển thị cùng hàng; căn ô số lượng, giá nhập và thành tiền ngang ô mã. Giới hạn kích thước ảnh, xử lý kích thước phần tử trong CSS Grid và ẩn phần tràn để ảnh không đè chữ của dòng tiếp theo. Nút thêm dòng vẫn nằm bên dưới bảng.')]),
('Hoàn thiện nhà cung cấp và tra cứu mã số thuế', [
('Thông tin nhà cung cấp', 'Hoàn thiện modal thêm nhà cung cấp gồm tên, mã số thuế, email, điện thoại và địa chỉ. Tên và mã số thuế là trường bắt buộc. Phạm vi kiểm tra mã số thuế được giới hạn cho nhà cung cấp là doanh nghiệp.'),
('Ràng buộc và tra cứu', 'API và frontend nhận mã 10 chữ số hoặc dạng 10 chữ số, dấu gạch ngang, 3 chữ số. Kiểm tra mã trùng khi thêm và sửa; giữ unique index để bảo vệ khi có yêu cầu đồng thời. Thêm nút mở cổng tra cứu Cục Thuế và lưu ý rằng đúng định dạng chưa đồng nghĩa doanh nghiệp đã được xác minh.'),
('Toolbar và dữ liệu tham khảo', 'Mở rộng riêng ô chọn nhà cung cấp lên 360px; ô trạng thái và kho giữ kích thước nhỏ. Đổi nút Thêm nhà cung cấp sang màu xanh giống Thêm phiếu nhập. Tổng hợp tên pháp nhân, mã số thuế và thông tin liên hệ công khai của Digiworld, Synnex FPT và PSD để tham khảo, không tự nhập các doanh nghiệp này vào database.')]),
('Điều chỉnh topbar và thao tác đơn hàng', [
('Thông tin người đăng nhập', 'Hiển thị họ tên phía trên email trên topbar, căn phải theo cùng nhóm thông tin tài khoản. Giữ nhãn vai trò và nút Đăng xuất ở bên cạnh để nhận biết người đang sử dụng hệ thống.'),
('Sửa và xóa đơn hàng', 'Cho hai nút Sửa, Xóa luôn xuất hiện ở cột thao tác. Chỉ đơn ở trạng thái Nháp được sử dụng hai nút; các trạng thái khác khóa nút và có chú thích. Giữ quy tắc API hiện có, không mở quyền sửa hoặc xóa đơn đã xác nhận.'),
('Tồn kho và giá nhập', 'Làm rõ tồn kho ở trang Sản phẩm được tính từ giao dịch nhập, xuất, không nhập trực tiếp khi sửa thông tin sản phẩm. Chuẩn bị giá nhập giả lập cho năm sản phẩm đang dùng trong bài; các mức giá này phục vụ kiểm thử, không phải báo giá thật của nhà cung cấp.')]),
('Kiểm thử và kết quả thực hiện', [
('Kiểm tra ràng buộc', 'Thực hiện 10 trường hợp kiểm tra thêm người dùng và đặt lại mật khẩu: mật khẩu ngắn, chỉ có chữ, chỉ có số, email không đúng định dạng và dữ liệu hợp lệ. Thực hiện 7 trường hợp mã số thuế: thiếu mã, sai độ dài, thiếu dấu phân tách và hai định dạng hợp lệ. Các trường hợp đều cho kết quả đúng mong đợi.'),
('Kiểm thử luồng nhập hàng', 'Bài kiểm thử trình duyệt dùng API giả lập chạy thành công: thêm nhà cung cấp, lưu phiếu một phần, tiếp tục theo phiếu đã tạo, xác nhận tăng số lượng kho mô phỏng, lọc trạng thái và khôi phục bộ lọc sau tải lại. Đây không phải kiểm thử giao dịch thật trên dữ liệu đang sử dụng.'),
('Kiểm tra kỹ thuật và giới hạn', 'TypeScript, ESLint và build backend Release thành công trong các lượt kiểm tra đã thực hiện; backend được khởi động lại để nạp thay đổi. Kiểm tra database có unique index mã số thuế. Việc tra cứu doanh nghiệp vẫn do người dùng đối chiếu trên cổng chính thức, chưa có tích hợp API xác minh tự động.')]),
]
def set_text(p, value):
    nodes = p.xpath('.//w:t', namespaces=ns)
    assert nodes
    for n in nodes: n.set('{http://www.w3.org/XML/1998/namespace}space', 'preserve')
    if ': ' in value and len(nodes) > 1:
        a, b = value.split(': ', 1)
        nodes[0].text, nodes[1].text = a + ': ', b
        for n in nodes[2:]: n.text = ''
    else:
        nodes[0].text = value
        for n in nodes[1:]: n.text = ''
with ZipFile(source) as original:
    xml = etree.fromstring(original.read('word/document.xml'))
    ps = xml.xpath('//w:body//w:p', namespaces=ns)
    heading, child = deepcopy(ps[32]), deepcopy(ps[33])
    body = ps[31].getparent()
    for p in ps[32:42]: body.remove(p)
    for p in ps[42:]:
        if p.getparent() is body and not ''.join(p.xpath('.//w:t/text()', namespaces=ns)).strip():
            body.remove(p)
    anchor = ps[31]
    for label, entries in groups:
        p = deepcopy(heading); set_text(p, label)
        props = p.find('w:pPr', ns)
        for tag in ('keepNext', 'keepLines'):
            if props.find('w:' + tag, ns) is None: etree.SubElement(props, '{%s}%s' % (ns['w'], tag))
        anchor.addnext(p); anchor = p
        for lead, text in entries:
            p = deepcopy(child); set_text(p, lead + ': ' + text)
            props = p.find('w:pPr', ns)
            # Allow long body text to flow naturally across pages.
            for e in props.findall('w:keepNext', ns): props.remove(e)
            anchor.addnext(p); anchor = p
    # Avoid forcing a largely empty middle page from the shorter draft.
    for e in ps[31].xpath('./w:pPr/w:pageBreakBefore', namespaces=ns): e.getparent().remove(e)
    with ZipFile(target, 'w') as out:
        for info in original.infolist(): out.writestr(info, etree.tostring(xml, xml_declaration=True, encoding='UTF-8', standalone=True) if info.filename == 'word/document.xml' else original.read(info.filename))
with ZipFile(source) as a, ZipFile(target) as b:
    for name in a.namelist():
        if name != 'word/document.xml': assert a.read(name) == b.read(name), name
print('Expanded report created; all non-body package parts preserved.')
