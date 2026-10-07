import type { Category, Order, Product, Warehouse } from "../../types";

export type ExcelSection = "products" | "orders";
export type Scalar = string | number | boolean | null;
export interface ExcelColumn { key: string; label: string; required: boolean; help: string; width: number; text?: boolean }
export const MAX_ROWS = 2000;
export const MAX_FILE_SIZE = 5 * 1024 * 1024;
export const schemas: Record<ExcelSection, { sheet: string; label: string; columns: ExcelColumn[] }> = {
  products: {
    sheet: "SanPham", label: "sản phẩm", columns: [
      { key: "sku", label: "SKU", required: true, text: true, width: 23, help: "Mã sản phẩm duy nhất, tối đa 50 ký tự ASCII. Không dùng mã hệ thống D hoặc SP." },
      { key: "name", label: "Tên sản phẩm", required: true, width: 32, help: "Tối đa 200 ký tự." },
      { key: "categoryCode", label: "Mã danh mục", required: true, text: true, width: 20, help: "Mã danh mục đang hoạt động và không có danh mục con, xem sheet ThamChieu." },
      { key: "unit", label: "Đơn vị", required: true, width: 15, help: "Tối đa 30 ký tự, ví dụ Cái hoặc Máy." },
      { key: "salePrice", label: "Giá bán", required: true, width: 18, help: "Số không âm, tối đa 2 chữ số thập phân; không nhập dấu phân cách hàng nghìn." },
      { key: "minimumStockLevel", label: "Tồn kho tối thiểu", required: false, width: 23, help: "Số nguyên không âm, mặc định 0. Đây là ngưỡng cảnh báo, không phải tồn kho hiện tại." },
      { key: "isActive", label: "Đang kinh doanh", required: false, width: 23, help: "TRUE hoặc FALSE, mặc định TRUE." },
      { key: "description", label: "Mô tả", required: false, width: 40, help: "Tối đa 1000 ký tự." },
    ],
  },
  orders: {
    sheet: "DonHang", label: "đơn hàng", columns: [
      { key: "orderNumber", label: "Mã đơn hàng", required: true, text: true, width: 28, help: "Mã duy nhất, tối đa 50 ký tự. Lặp lại cùng mã cho các sản phẩm của một đơn." },
      { key: "orderDate", label: "Ngày đặt", required: true, width: 18, help: "Ngày dạng YYYY-MM-DD hoặc ô ngày Excel." },
      { key: "warehouseCode", label: "Mã kho", required: true, text: true, width: 18, help: "Mã kho đang hoạt động, xem sheet ThamChieu." },
      { key: "customerName", label: "Tên khách hàng", required: true, width: 26, help: "Tối đa 100 ký tự, giống nhau ở mọi dòng của cùng một đơn." },
      { key: "customerPhone", label: "Số điện thoại", required: true, text: true, width: 21, help: "8 đến 15 chữ số, giữ dạng văn bản để không mất số 0 đầu." },
      { key: "shippingAddress", label: "Địa chỉ giao hàng", required: true, width: 38, help: "Tối đa 255 ký tự." },
      { key: "sku", label: "SKU", required: true, text: true, width: 23, help: "SKU của sản phẩm đang kinh doanh; không lặp sản phẩm trong cùng một đơn." },
      { key: "quantity", label: "Số lượng", required: true, width: 16, help: "Số nguyên dương, tối đa 2147483647." },
      { key: "unitPrice", label: "Đơn giá", required: true, width: 18, help: "Số không âm, tối đa 2 chữ số thập phân." },
      { key: "discount", label: "Giảm giá (tiền)", required: false, width: 22, help: "Số tiền giảm của toàn dòng, mặc định 0; không vượt số lượng nhân đơn giá. Đây là số tiền, không phải phần trăm." },
      { key: "status", label: "Trạng thái", required: false, text: true, width: 18, help: "Đơn mới chỉ nhận Draft (Nháp), mặc định Draft. File xuất giữ trạng thái thực tế để tra cứu." },
      { key: "notes", label: "Ghi chú", required: false, width: 35, help: "Tối đa 500 ký tự, giống nhau ở mọi dòng của một đơn." },
    ],
  },
};
export interface InputRow { rowNumber: number; values: Record<string, Scalar>; errors: string[] }
export interface CheckedRow extends InputRow { duplicate: boolean }
export interface References { categories: Category[]; products: Product[]; warehouses: Warehouse[]; orders: Order[] }
export const emptyReferences = (): References => ({ categories: [], products: [], warehouses: [], orders: [] });
export const normalizeKey = (value: Scalar | undefined) => String(value ?? "").trim().toUpperCase();
const text = (v: Scalar | undefined) => String(v ?? "").trim();
const absent = (v: Scalar | undefined) => v === null || v === undefined || text(v) === "";
function decimal(v: Scalar | undefined, min: number, max: number) {
  if (absent(v) || typeof v === "boolean") return false;
  if (!/^\d+(?:\.\d{1,2})?$/.test(text(v))) return false;
  const n = Number(v);
  return Number.isFinite(n) && n >= min && n <= max;
}
function integer(v: Scalar | undefined, min: number) {
  return !absent(v) && typeof v !== "boolean" && /^\d+$/.test(text(v)) && Number.isSafeInteger(Number(v)) && Number(v) >= min && Number(v) <= 2147483647;
}
export function validDate(v: Scalar | undefined) {
  const s = text(v);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(s)) return false;
  const d = new Date(`${s}T00:00:00Z`);
  return Number.isFinite(d.getTime()) && d.toISOString().slice(0, 10) === s && s >= "1753-01-01";
}

export function validateRows(section: ExcelSection, input: InputRow[], refs: References): CheckedRow[] {
  const rows = input.map(r => ({ ...r, values: { ...r.values }, errors: [...r.errors], duplicate: false }));
  const counts = new Map<string, number>();
  for (const r of rows) {
    const key = section === "products" ? normalizeKey(r.values.sku) : `${normalizeKey(r.values.orderNumber)}\u0000${normalizeKey(r.values.sku)}`;
    counts.set(key, (counts.get(key) || 0) + 1);
  }
  const existing = new Set((section === "products" ? refs.products.map(p => p.sku) : refs.orders.map(o => o.orderNumber)).map(normalizeKey));
  const groups = new Map<string, CheckedRow[]>();
  for (const row of rows) {
    const v = row.values;
    const add = (s: string) => row.errors.push(s);
    for (const c of schemas[section].columns) {
      if (c.required && absent(v[c.key])) add(`${c.label}: bắt buộc.`);
      if (c.text && typeof v[c.key] === "number") add(`${c.label}: phải là văn bản để giữ nguyên mã và số 0 đầu.`);
    }
    const length = (key: string, label: string, max: number) => {
      if (text(v[key]).length > max) add(`${label}: tối đa ${max} ký tự.`);
    };
    length("sku", "SKU", 50);
    if (!absent(v.sku) && !/^[\x20-\x7E]+$/.test(text(v.sku))) add("SKU: chỉ nhận ký tự ASCII in được.");
    const key = section === "products" ? normalizeKey(v.sku) : `${normalizeKey(v.orderNumber)}\u0000${normalizeKey(v.sku)}`;
    if ((counts.get(key) || 0) > 1) { row.duplicate = true; add(section === "products" ? "SKU bị trùng trong file." : "Sản phẩm bị lặp trong cùng đơn hàng."); }
    const id = section === "products" ? normalizeKey(v.sku) : normalizeKey(v.orderNumber);
    if (id && existing.has(id)) { row.duplicate = true; add(section === "products" ? "SKU đã tồn tại trong hệ thống." : "Mã đơn hàng đã tồn tại trong hệ thống."); }
    if (section === "products") {
      length("name", "Tên sản phẩm", 200); length("unit", "Đơn vị", 30); length("description", "Mô tả", 1000);
      const category = refs.categories.find(c => normalizeKey(c.code) === normalizeKey(v.categoryCode));
      if (!category || !category.isActive || refs.categories.some(c => c.parentCategoryId === category.categoryId)) add("Mã danh mục: phải thuộc danh mục hoạt động, không có danh mục con.");
      // JS cannot represent all decimal(18,2) values exactly; reject unsafe amounts.
      if (!decimal(v.salePrice, 0, 9999999999999.99)) add("Giá bán: số từ 0 đến 9999999999999.99, tối đa 2 chữ số thập phân.");
      if (absent(v.minimumStockLevel)) v.minimumStockLevel = 0;
      if (!integer(v.minimumStockLevel, 0)) add("Tồn kho tối thiểu: số nguyên không âm.");
      if (absent(v.isActive)) v.isActive = true;
      if (!["TRUE", "FALSE"].includes(normalizeKey(v.isActive))) add("Đang kinh doanh: chỉ nhận TRUE hoặc FALSE.");
    } else {
      length("orderNumber", "Mã đơn hàng", 50); length("customerName", "Tên khách hàng", 100); length("shippingAddress", "Địa chỉ giao hàng", 255); length("notes", "Ghi chú", 500);
      if (!validDate(v.orderDate)) add("Ngày đặt: ngày hợp lệ theo YYYY-MM-DD, từ 1753-01-01.");
      if (!/^\d{8,15}$/.test(text(v.customerPhone))) add("Số điện thoại: phải gồm 8 đến 15 chữ số.");
      if (!refs.warehouses.some(w => w.isActive && normalizeKey(`KHO-${String(w.warehouseId).padStart(4, "0")}`) === normalizeKey(v.warehouseCode))) add("Mã kho: không tồn tại hoặc đã ngừng hoạt động.");
      if (!refs.products.some(p => p.isActive && normalizeKey(p.sku) === normalizeKey(v.sku))) add("SKU: không tồn tại hoặc đã ngừng kinh doanh.");
      if (!integer(v.quantity, 1)) add("Số lượng: số nguyên dương, tối đa 2147483647.");
      if (!decimal(v.unitPrice, 0, 9999999999999.99)) add("Đơn giá: số không âm, tối đa 2 chữ số thập phân và 9999999999999.99.");
      if (absent(v.discount)) v.discount = 0;
      if (!decimal(v.discount, 0, 9999999999999.99)) add("Giảm giá (tiền): số không âm, tối đa 2 chữ số thập phân.");
      if (Number(v.discount) > Number(v.quantity) * Number(v.unitPrice)) add("Giảm giá (tiền): không được vượt giá trị dòng hàng.");
      if (absent(v.status)) v.status = "Draft";
      if (v.status !== "Draft") add("Trạng thái: đơn mới chỉ nhận Draft (Nháp), không tự ghi nhận doanh thu hoặc xuất kho.");
      const group = groups.get(id) || []; group.push(row); groups.set(id, group);
    }
  }
  for (const group of groups.values()) {
    const common = ["orderDate", "warehouseCode", "customerName", "customerPhone", "shippingAddress", "status", "notes"];
    if (common.some(k => new Set(group.map(r => k === "warehouseCode" ? normalizeKey(r.values[k]) : text(r.values[k]))).size > 1)) {
      group.forEach(r => r.errors.push("Thông tin chung không thống nhất giữa các dòng của cùng đơn hàng."));
    }
  }
  return rows;
}
