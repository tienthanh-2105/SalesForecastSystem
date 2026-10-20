from __future__ import annotations

import hashlib
import sys
import zipfile
from pathlib import Path

from lxml import etree


ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "output/docx/Phạm Minh Tài_Báo cáo ngày 14-10-2026.docx"
TARGET = ROOT / "output/docx/Phạm Minh Tài_Báo cáo ngày 15-10-2026.docx"
NS = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
W = f"{{{NS['w']}}}"


def set_paragraph_run(paragraph: etree._Element, run_index: int, value: str) -> None:
    runs = paragraph.xpath("./w:r", namespaces=NS)
    run = runs[run_index]
    text_nodes = run.xpath(".//w:t", namespaces=NS)
    if not text_nodes:
        text = etree.SubElement(run, W + "t")
        text.text = value
        return
    text_nodes[0].text = value
    if value.startswith(" ") or value.endswith(" "):
        text_nodes[0].set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    for node in text_nodes[1:]:
        node.text = ""


def set_paragraph_text(paragraph: etree._Element, value: str) -> None:
    set_paragraph_run(paragraph, 0, value)
    for run in paragraph.xpath("./w:r", namespaces=NS)[1:]:
        for node in run.xpath(".//w:t", namespaces=NS):
            node.text = ""


paragraph_values = {
    0: [(0, "BÁO CÁO NGÀY 15/10/2026")],
    3: [(1, "Tích hợp API dự báo baseline, lưu kết quả và cập nhật hướng dẫn chạy.")],
    4: [(1, "Rà soát tồn kho đa kho và chuyển trạng thái đơn hàng, người dùng sang enum.")],
    6: [(1, "Chuẩn hóa kiểu trạng thái backend, giữ tương thích database và JSON API.")],
    7: [(0, "Enum nghiệp vụ: "), (1, "Tạo enum cho đơn bán, đơn nhập và người dùng trong Core.")],
    8: [(0, "Luồng xử lý: "), (1, "Cập nhật entity, DTO, service, xác thực tài khoản, báo cáo và dự báo.")],
    9: [(0, "Tương thích: "), (1, "EF Core lưu tên trạng thái; API trả chuỗi và từ chối giá trị số.")],
    10: [(0, "Tồn kho đa kho: "), (1, "Xác nhận tồn gắn với sản phẩm và kho; màn hình sản phẩm đang hiển thị tổng tồn.")],
    15: [(0, "Enum dùng chung: "), (1, "Thêm SalesOrderStatus, PurchaseOrderStatus và UserStatus trong SalesForecastSystem.Core/Enums.")],
    16: [(0, "Entity và EF Core: "), (1, "Đổi Status của SalesOrder, PurchaseOrder, User sang enum; cấu hình HasConversion<string>() và mặc định enum.")],
    17: [(0, "Hợp đồng API: "), (1, "Đổi request/response/filter DTO sang enum; kiểm tra Enum.IsDefined và giữ JSON dạng tên trạng thái, không nhận số.")],
    18: [(0, "Nghiệp vụ: "), (1, "Đổi điều kiện chuyển trạng thái trong sales/purchase/user service, xác thực, session, báo cáo, dự báo, lịch sử khách hàng và import Excel.")],
    19: [(0, "Rà soát kho: "), (1, "Sản phẩm là dữ liệu chung; tồn được tính theo sản phẩm–kho. Phiếu nhập và đơn bán chọn kho; màn hình sản phẩm đang cộng tồn các kho.")],
    20: [(0, "Tương thích database: "), (1, "Giữ cột trạng thái dạng chuỗi và lưu tên enum; không cần migration đổi kiểu cột.")],
    21: [(0, "Build: "), (1, "Core và Infrastructure build thành công, 0 cảnh báo và 0 lỗi. Bổ sung framework reference ASP.NET Core để Infrastructure biên dịch ForecastService.")],
    22: [(0, "Giới hạn xác minh: "), (1, "Build API/solution dừng ở bước MSBuild khôi phục hoặc tìm project reference, không trả lỗi cụ thể; chưa chạy kiểm thử tích hợp.")],
    23: [(0, "Kết quả: "), (1, "Ba nhóm trạng thái đã dùng enum xuyên entity, DTO và service; API/database vẫn trao đổi tên trạng thái.")],
    24: [(0, "Việc tiếp theo: "), (1, "Xác nhận build toàn solution trong Visual Studio và chạy kiểm thử tích hợp trên database kiểm thử riêng.")],
}

table_text = {
    (1, 0): "Enum và luồng nghiệp vụ",
    (1, 1): [
        "Thêm ba enum trạng thái trong SalesForecastSystem.Core.",
        "Đổi entity, DTO, bộ lọc và so sánh trạng thái trong service.",
        "Cập nhật xác thực tài khoản, báo cáo, dự báo và import Excel.",
        "Giữ nguyên tên trạng thái trong dữ liệu SQL và JSON API.",
    ],
    (2, 0): "Rà soát kho và tương thích",
    (2, 1): [
        "Tồn kho được tính riêng theo sản phẩm và kho.",
        "Phiếu nhập và đơn bán gắn với kho được chọn.",
        "Màn hình sản phẩm hiển thị tổng tồn từ các kho.",
        "Không cần migration đổi kiểu cột trạng thái.",
    ],
    (3, 0): "Build và xác minh",
    (3, 1): [
        "Core và Infrastructure build đạt, không cảnh báo/lỗi.",
        "Build API/solution bị dừng ở restore/reference của MSBuild.",
        "Chưa chạy kiểm thử tích hợp.",
        "Cần xác nhận build và test trong môi trường phù hợp.",
    ],
}


def patch_document(xml: bytes) -> bytes:
    tree = etree.fromstring(xml)
    body = tree.find("w:body", namespaces=NS)
    paragraphs = body.findall("w:p", namespaces=NS)
    for index, edits in paragraph_values.items():
        for run_index, value in edits:
            set_paragraph_run(paragraphs[index], run_index, value)

    table = body.find("w:tbl", namespaces=NS)
    rows = table.findall("w:tr", namespaces=NS)
    for (row_index, cell_index), value in table_text.items():
        cell = rows[row_index].findall("w:tc", namespaces=NS)[cell_index]
        cell_paragraphs = cell.findall("w:p", namespaces=NS)
        if cell_index == 0:
            set_paragraph_text(cell_paragraphs[0], value)
            continue
        for paragraph, bullet in zip(cell_paragraphs, value, strict=True):
            set_paragraph_text(paragraph, bullet)
    return etree.tostring(tree, xml_declaration=True, encoding="UTF-8", standalone=True)


def main() -> None:
    original_parts: dict[str, bytes] = {}
    with zipfile.ZipFile(SOURCE, "r") as source:
        with zipfile.ZipFile(TARGET, "w") as target:
            for item in source.infolist():
                content = source.read(item.filename)
                original_parts[item.filename] = content
                if item.filename == "word/document.xml":
                    content = patch_document(content)
                target.writestr(item, content)

    with zipfile.ZipFile(TARGET, "r") as final:
        for item in final.infolist():
            if item.filename != "word/document.xml":
                assert hashlib.sha256(final.read(item.filename)).digest() == hashlib.sha256(original_parts[item.filename]).digest()
    print(TARGET)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
