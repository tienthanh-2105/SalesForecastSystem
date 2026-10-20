from __future__ import annotations

import hashlib
import shutil
import zipfile
from pathlib import Path

from lxml import etree


ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "output/docx/Phạm Minh Tài_Báo cáo ngày 13-10-2026.docx"
TARGET = ROOT / "output/docx/Phạm Minh Tài_Báo cáo ngày 14-10-2026.docx"
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
    0: [(0, "BÁO CÁO NGÀY 14/10/2026")],
    3: [(1, "Cải thiện phân tích nhóm, bảng báo cáo, xác nhận sản phẩm và dữ liệu mẫu.")],
    4: [(1, "Tích hợp mô hình dự báo cơ sở vào API và lưu kết quả theo lần chạy.")],
    6: [(1, "Hoàn thiện API chạy dự báo baseline, lưu kết quả và hướng dẫn triển khai.")],
    7: [(0, "API dự báo: "), (1, "Thêm API tạo lần chạy, gọi các mô hình moving average và seasonal naive.")],
    8: [(0, "Lưu kết quả: "), (1, "Lưu mô hình được chọn, MAE/RMSE và các giá trị dự báo theo ngày.")],
    9: [(0, "Database và deploy: "), (1, "Bổ sung API đọc lại kết quả và migration lưu chẩn đoán theo lần chạy.")],
    10: [(0, "Tích hợp Python: "), (1, "Đóng gói mã Python cùng API và cập nhật hướng dẫn cấu hình, sử dụng.")],
    15: [(0, "API chạy dự báo: "), (1, "Thêm POST /api/forecasts; yêu cầu sản phẩm, kho, khoảng dữ liệu và kỳ dự báo.")],
    16: [(0, "Chuỗi và đánh giá: "), (1, "Tạo chuỗi ngày từ đơn Completed, chia Training/Validation và gọi module Python để đánh giá moving average cùng seasonal naive bằng walk-forward validation.")],
    17: [(0, "Chọn baseline: "), (1, "Chọn mô hình theo RMSE rồi MAE; trả thông tin ứng viên, MAE/RMSE và dự báo có cận dưới, cận trên.")],
    18: [(0, "Lưu dữ liệu: "), (1, "Lưu ForecastModel, ForecastRun và ForecastResults trong một transaction; doanh thu dự báo tính theo giá bán sản phẩm.")],
    19: [(0, "API kết quả: "), (1, "Thêm GET /api/forecasts/{runId} để đọc thông tin lần chạy, chẩn đoán đánh giá và kết quả đã lưu.")],
    20: [(0, "Migration database: "), (1, "Migration 015 thêm ModelDiagnostics dạng JSON cho từng ForecastRun; cập nhật Deploy.ps1 và nâng schema version lên 12.")],
    21: [(0, "Chạy module Python: "), (1, "Cấu hình dự án để build/publish API kèm các tệp Python. Cho phép cấu hình Forecasting:PythonExecutable; tiến trình trao đổi đầu vào và kết quả qua JSON tạm.")],
    22: [(0, "Tài liệu API: "), (1, "Cập nhật HuongDanChay.md với payload mẫu, giới hạn kỳ dự báo 1–366 ngày và yêu cầu cài Python 3.")],
    23: [(0, "Kết quả: "), (1, "Đã nối luồng chuẩn bị chuỗi, chạy baseline, đánh giá, lưu và truy vấn kết quả dự báo qua API.")],
    24: [(0, "Ghi chú: "), (1, "Chưa chạy build hoặc kiểm thử tự động. Cần áp dụng migration 015 và có Python 3 trước khi vận hành API.")],
}

table_text = {
    (1, 0): "API dự báo",
    (1, 1): [
        "Thêm POST /api/forecasts để chạy mô hình.",
        "Kỳ dự báo mặc định 7 ngày, cho phép từ 1 đến 366 ngày.",
        "So sánh moving average và seasonal naive trên tập kiểm định.",
        "Trả MAE/RMSE, ứng viên và dự báo có khoảng giá trị.",
    ],
    (2, 0): "Lưu trữ và triển khai",
    (2, 1): [
        "Lưu mô hình, lần chạy và kết quả trong transaction.",
        "Thêm ModelDiagnostics và migration schema version 12.",
        "Đăng ký migration 015 trong database/Deploy.ps1.",
        "Đóng gói mã Python và hỗ trợ cấu hình lệnh chạy.",
    ],
    (3, 0): "Tài liệu và trạng thái",
    (3, 1): [
        "Cập nhật hướng dẫn API và payload mẫu.",
        "Tài liệu hóa yêu cầu Python 3 và cấu hình executable.",
        "Ghi rõ cần áp dụng migration trước khi chạy API.",
        "Chưa chạy build và kiểm thử.",
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
    TARGET.parent.mkdir(parents=True, exist_ok=True)
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
    shutil.copystat(SOURCE, TARGET)
    print(TARGET)


if __name__ == "__main__":
    main()
