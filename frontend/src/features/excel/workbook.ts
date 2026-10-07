import type { CellValue, Workbook, Worksheet } from "exceljs";
import { MAX_FILE_SIZE, MAX_ROWS, schemas, type ExcelSection, type InputRow, type References, type Scalar } from "./schema";

async function newWorkbook(): Promise<Workbook> {
  const { default: ExcelJS } = await import("exceljs");
  return new ExcelJS.Workbook();
}
function valueOf(value: CellValue): { value: Scalar; error?: string } {
  if (value === null || value === undefined) return { value: null };
  if (value instanceof Date) return { value: Number.isFinite(value.getTime()) ? value.toISOString().slice(0, 10) : "", error: Number.isFinite(value.getTime()) ? undefined : "Ngày Excel không hợp lệ." };
  if (typeof value !== "object") return { value };
  if ("richText" in value) return { value: value.richText.map(r => r.text).join("") };
  if ("hyperlink" in value) return { value: value.text, error: "Không nhận ô chứa liên kết; thay bằng giá trị văn bản." };
  return { value: "[Công thức hoặc lỗi Excel]", error: "Không nhận công thức hoặc ô lỗi; thay bằng giá trị cụ thể." };
}
export async function readWorkbook(section: ExcelSection, buffer: ArrayBuffer): Promise<InputRow[]> {
  if (buffer.byteLength > MAX_FILE_SIZE) throw new Error("File vượt quá 5 MB.");
  const workbook = await newWorkbook();
  try { await workbook.xlsx.load(buffer); } catch { throw new Error("Không đọc được file Excel. Hãy dùng file .xlsx hợp lệ, không đặt mật khẩu."); }
  const schema = schemas[section];
  const sheet = workbook.getWorksheet(schema.sheet);
  if (!sheet) throw new Error(`Thiếu sheet ${schema.sheet}. Hãy tải và dùng file mẫu.`);
  if (sheet.rowCount > MAX_ROWS + 1) throw new Error(`Chỉ hỗ trợ tối đa ${MAX_ROWS} dòng dữ liệu. Hãy chia file nhỏ hơn và xóa các dòng trống thừa.`);
  if (sheet.columnCount > 32) throw new Error("File có quá nhiều cột. Hãy dùng đúng file mẫu.");
  const positions = new Map<string, number>();
  sheet.getRow(1).eachCell((cell, index) => {
    const name = cell.text.trim().replace(/\s*\*$/, "");
    if (positions.has(name)) throw new Error(`Cột ${name} bị lặp.`);
    if (!schema.columns.some(c => c.label === name)) throw new Error(`Cột không được hỗ trợ: ${name}. Hãy dùng đúng file mẫu.`);
    if (cell.isMerged) throw new Error("Không gộp ô ở dòng tiêu đề.");
    positions.set(name, index);
  });
  const missing = schema.columns.filter(c => c.required && !positions.has(c.label));
  if (missing.length) throw new Error(`Thiếu cột bắt buộc: ${missing.map(c => c.label).join(", ")}.`);
  const rows: InputRow[] = [];
  for (let i = 2; i <= sheet.rowCount; i++) {
    const source = sheet.getRow(i);
    const values: Record<string, Scalar> = {}, errors: string[] = [];
    for (const c of schema.columns) {
      const index = positions.get(c.label);
      const cell = index ? source.getCell(index) : undefined;
      const converted = valueOf(cell?.value ?? null);
      values[c.key] = converted.value;
      if (converted.error) errors.push(`${c.label}: ${converted.error}`);
      if (cell?.isMerged) errors.push(`${c.label}: không gộp ô dữ liệu.`);
      if (["discount", "unitPrice", "salePrice"].includes(c.key) && cell?.numFmt?.includes("%")) errors.push(`${c.label}: nhập số tiền, không dùng định dạng Percentage.`);
    }
    if (Object.values(values).every(v => v === null || String(v).trim() === "") && !errors.length) continue;
    rows.push({ rowNumber: i, values, errors });
  }
  if (!rows.length) throw new Error("File chưa có dòng dữ liệu. Điền dữ liệu từ dòng 2 trong sheet " + schema.sheet + ".");
  return rows;
}

function styleSheet(sheet: Worksheet) {
  sheet.views = [{ state: "frozen", ySplit: 1 }];
  const header = sheet.getRow(1);
  header.height = 30;
  header.eachCell(c => {
    c.font = { bold: true, color: { argb: "FFFFFFFF" } };
    c.fill = { type: "pattern", pattern: "solid", fgColor: { argb: "FF1976A5" } };
    c.alignment = { vertical: "middle", wrapText: true };
  });
  sheet.autoFilter = { from: { row: 1, column: 1 }, to: { row: Math.max(1, sheet.rowCount), column: sheet.columnCount } };
}

export async function buildWorkbook(section: ExcelSection, rows: Record<string, Scalar>[], refs: References, template = false): Promise<ArrayBuffer> {
  const workbook = await newWorkbook();
  workbook.creator = "SalesForecastSystem";
  const schema = schemas[section], sheet = workbook.addWorksheet(schema.sheet);
  sheet.columns = schema.columns.map(c => ({ header: c.label, key: c.key, width: c.width, style: c.text ? { numFmt: "@" } : {} }));
  for (const row of rows) sheet.addRow(row);
  styleSheet(sheet);
  for (const c of schema.columns) sheet.getRow(1).getCell(c.key).note = (c.required ? "Bắt buộc. " : "Không bắt buộc. ") + c.help;
  if (template) {
    // Only the data sheet is imported; examples live separately to avoid accidental imports.
    const example = workbook.addWorksheet("ViDu");
    example.columns = sheet.columns.map(c => ({ header: c.header, key: c.key, width: c.width, style: c.style }));
    const leaf = refs.categories.find(c => c.isActive && !refs.categories.some(child => child.parentCategoryId === c.categoryId));
    const warehouse = refs.warehouses.find(w => w.isActive);
    const activeProducts = refs.products.filter(p => p.isActive);
    if (section === "products") example.addRow({ sku: "SKU-MOI-001", name: "Sản phẩm mẫu", categoryCode: leaf?.code || "DM-0001", unit: "Cái", salePrice: 150000, minimumStockLevel: 0, isActive: true, description: "Ví dụ, thay bằng dữ liệu của bạn." });
    else {
      const common = { orderNumber: "DH-MOI-001", orderDate: "2026-10-05", warehouseCode: warehouse ? `KHO-${String(warehouse.warehouseId).padStart(4, "0")}` : "KHO-0001", customerName: "Khách hàng mẫu", customerPhone: "0901234567", shippingAddress: "Địa chỉ mẫu", status: "Draft", notes: "Một đơn có thể có nhiều dòng sản phẩm." };
      example.addRow({ ...common, sku: activeProducts[0]?.sku || "SKU-DANG-BAN-001", quantity: 1, unitPrice: 150000, discount: 0 });
      example.addRow({ ...common, sku: activeProducts[1]?.sku || "SKU-DANG-BAN-002", quantity: 2, unitPrice: 100000, discount: 20000 });
    }
    styleSheet(example);
  }
  const reference = workbook.addWorksheet("ThamChieu");
  reference.columns = [{ header: "Loại", key: "kind", width: 18 }, { header: "Mã", key: "code", width: 28 }, { header: "Tên", key: "name", width: 42 }];
  if (section === "products") refs.categories.filter(c => c.isActive && !refs.categories.some(child => child.parentCategoryId === c.categoryId)).forEach(c => reference.addRow({ kind: "Danh mục", code: c.code, name: c.name }));
  else {
    refs.warehouses.filter(w => w.isActive).forEach(w => reference.addRow({ kind: "Kho", code: `KHO-${String(w.warehouseId).padStart(4, "0")}`, name: w.name }));
    refs.products.filter(p => p.isActive).forEach(p => reference.addRow({ kind: "Sản phẩm", code: p.sku, name: p.name }));
  }
  reference.getColumn("code").numFmt = "@";
  styleSheet(reference);
  const guide = workbook.addWorksheet("HuongDan");
  guide.columns = [{ header: "Cột", key: "column", width: 25 }, { header: "Bắt buộc", key: "required", width: 14 }, { header: "Quy định", key: "help", width: 100 }];
  schema.columns.forEach(c => guide.addRow({ column: c.label, required: c.required ? "Có" : "Không", help: c.help }));
  guide.addRow({ column: "Cách điền", help: `Chỉ đọc sheet ${schema.sheet}, tiêu đề dòng 1 và dữ liệu từ dòng 2. Giữ tên cột. Tối đa ${MAX_ROWS} dòng, 5 MB. Sheet ViDu chỉ để tham khảo.` });
  guide.addRow({ column: "Mã trùng", help: "Không ghi đè dữ liệu đã có. SKU hoặc mã đơn đã tồn tại sẽ bị đánh dấu lỗi. Mã hệ thống không được nhập từ Excel." });
  guide.addRow({ column: "Tồn kho", help: "Không nhập tồn kho hiện tại. Đơn mới chỉ tạo Nháp; việc xác nhận nghiệp vụ nhập/xuất kho thực hiện riêng." });
  guide.addRow({ column: "File xuất", help: "Dữ liệu xuất dùng để tra cứu. Nhập lại bị chặn bởi kiểm tra mã trùng; đơn đã hoàn thành không được tự ghi nhận lần nữa." });
  guide.getColumn("help").alignment = { wrapText: true, vertical: "top" };
  for (let i = 2; i <= guide.rowCount; i++) guide.getRow(i).height = 45;
  styleSheet(guide);
  const buffer = await workbook.xlsx.writeBuffer();
  return new Uint8Array(buffer).slice().buffer;
}

export function downloadWorkbook(buffer: ArrayBuffer, name: string) {
  const url = URL.createObjectURL(new Blob([buffer], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }));
  const a = document.createElement("a"); a.href = url; a.download = name;
  document.body.appendChild(a); a.click(); a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 30000);
}
