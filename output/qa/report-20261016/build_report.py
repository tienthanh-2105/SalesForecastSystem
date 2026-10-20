from __future__ import annotations

import hashlib
import sys
import zipfile
from pathlib import Path
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "output/docx/Phạm Minh Tài_Báo cáo ngày 15-10-2026.docx"
TARGET = ROOT / "output/docx/Phạm Minh Tài_Báo cáo ngày 16-10-2026.docx"
NS = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
W = f"{{{NS['w']}}}"


def set_paragraph_run(paragraph: ET.Element, run_index: int, value: str) -> None:
    runs = paragraph.findall("w:r", namespaces=NS)
    run = runs[run_index]
    text_nodes = run.findall(".//w:t", namespaces=NS)
    if not text_nodes:
        text = ET.SubElement(run, W + "t")
        text.text = value
        return
    text_nodes[0].text = value
    if value.startswith(" ") or value.endswith(" "):
        text_nodes[0].set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    for node in text_nodes[1:]:
        node.text = ""


def set_paragraph_text(paragraph: ET.Element, value: str) -> None:
    set_paragraph_run(paragraph, 0, value)
    for run in paragraph.findall("w:r", namespaces=NS)[1:]:
        for node in run.findall(".//w:t", namespaces=NS):
            node.text = ""


paragraph_values = {
    0: [(0, "BÁO CÁO NGÀY 16/10/2026")],
    3: [(1, "Rà soát tồn kho đa kho và chuyển trạng thái đơn hàng, người dùng sang enum.")],
    4: [(1, "Hoàn tất build solution và bộ kiểm thử, đồng bộ luồng xử lý và xuất báo cáo ngày.")],
    6: [(1, "Nghiệm thu kiểm thử toàn diện backend/frontend, chuẩn hóa xử lý ngoại lệ DB.")],
    7: [(0, "Enum nghiệp vụ: "), (1, "Đồng bộ SalesOrderStatus, PurchaseOrderStatus, UserStatus xuyên suốt entity, DTO, service.")],
    8: [(0, "Kiểm thử hệ thống: "), (1, "Toàn bộ solution và test project .NET 8 build đạt 100%; toàn bộ 33 frontend test chạy xanh.")],
    9: [(0, "Xử lý đồng thời: "), (1, "Khắc phục bắt lỗi SqlException (deadlock, duplicate key) qua GetBaseException() trong import Excel và các service.")],
    10: [(0, "Tổng kết tuần: "), (1, "Kiểm tra toàn vẹn luồng đơn hàng, kho và hoàn thiện báo cáo công việc tuần 07.")],
    15: [(0, "Kiểm thử Frontend: "), (1, "Điều chỉnh selector ambiguous trong day5-day6.test.tsx; toàn bộ 6 test suite (33 test) và build Vite/TS đều đạt 100%.")],
    16: [(0, "Build Backend: "), (1, "Biên dịch thành công SalesForecastSystem.sln, ExcelChecks và IntegrationChecks với .NET SDK (0 cảnh báo, 0 lỗi).")],
    17: [(0, "Xử lý ngoại lệ DB: "), (1, "Cải tiến hàm IsDatabaseConflict và DatabaseError duyệt cây GetBaseException() để bắt đúng mã lỗi SqlException bị bọc lồng.")],
    18: [(0, "Kiểm thử tích hợp: "), (1, "Chạy bộ kiểm thử ExcelChecks (140 assertions) đạt tuyệt đối, xác nhận an toàn dữ liệu và cơ chế chống deadlock.")],
    19: [(0, "Hợp đồng API & Regex: "), (1, "Hỗ trợ khoảng trắng đầu cuối trong AccountValidation.EmailPattern giúp chuẩn hóa email tài khoản người dùng.")],
    20: [(0, "Tương thích database: "), (1, "Giữ nguyên cấu trúc chuỗi cho cột trạng thái trong SQL Server, tương thích hoàn toàn stored procedure và trigger.")],
    21: [(0, "Trạng thái mã nguồn: "), (1, "Code base gọn gàng, hoàn thành toàn bộ mục tiêu chuyển đổi enum và tích hợp dự báo baseline.")],
    22: [(0, "Giới hạn xác minh: "), (1, "Kiểm thử tích hợp phụ thuộc môi trường SQL Server Express nội bộ; đã xác minh các luồng nghiệp vụ chính.")],
    23: [(0, "Kết quả: "), (1, "Hệ thống hoạt động ổn định từ Core, Infrastructure, API đến Frontend; sẵn sàng cho báo cáo tuần.")],
    24: [(0, "Việc tiếp theo: "), (1, "Nộp báo cáo tuần 07, chuẩn bị kế hoạch tuần tiếp theo về tối ưu hóa mô hình ML dự báo bán hàng.")],
}

table_text = {
    (1, 0): "Enum và kiểm thử hệ thống",
    (1, 1): [
        "Hoàn tất chuyển đổi ba enum trạng thái trong Core.",
        "Toàn bộ solution .NET 8 và các dự án test build đạt.",
        "Khắc phục test frontend, 33/33 test Vitest chạy xanh.",
        "Build production frontend Vite hoàn tất không lỗi.",
    ],
    (2, 0): "Xử lý lỗi DB và tương thích",
    (2, 1): [
        "Xử lý bóc tách SqlException bị EF Core bọc lồng.",
        "Vượt qua kiểm thử 140 assertions của ExcelChecks.",
        "Chuẩn hóa regex email cho phép cắt khoảng trắng.",
        "Đảm bảo an toàn giao dịch và chống deadlock khi import.",
    ],
    (3, 0): "Nghiệm thu và bàn giao tuần",
    (3, 1): [
        "Hệ thống đạt tính toàn vẹn và độ tin cậy cao.",
        "Hoàn thiện báo cáo ngày 16/10/2026 đúng hạn.",
        "Cập nhật tài liệu kỹ thuật và kế hoạch phát triển.",
        "Chuẩn bị nội dung báo cáo tổng kết tuần 07.",
    ],
}


def patch_document(xml: bytes) -> bytes:
    ET.register_namespace("w", NS["w"])
    tree = ET.fromstring(xml)
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
    return ET.tostring(tree, encoding="utf-8", xml_declaration=True)


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
    print("SUCCESS:", TARGET)


if __name__ == "__main__":
    main()
