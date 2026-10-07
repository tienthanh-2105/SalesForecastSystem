import { describe, expect, it, vi, afterEach } from "vitest";
import ExcelJS from "exceljs";
import { buildWorkbook, readWorkbook } from "../features/excel/workbook";
import { emptyReferences, schemas, validateRows, type InputRow, type References } from "../features/excel/schema";
import { boundedPages, exportRows } from "../features/excel/service";
import { configureApi } from "../lib/api";

const refs: References = {
  ...emptyReferences(),
  categories: [{ categoryId: 1, code: "DM-0001", name: "Laptop", description: null, parentCategoryId: null, isActive: true }],
  warehouses: [{ warehouseId: 1, name: "Kho 1", address: null, isActive: true }],
  products: [{ productId: 1, sku: "LAP-01", name: "Laptop", unit: "Máy", categoryId: 1, description: "Mô tả thật", imageUrl: null, salePrice: 150000, minimumStockLevel: 0, isActive: true, rowVersion: "x" }],
};
const productRow = (rowNumber = 2, sku = "NEW-01"): InputRow => ({ rowNumber, errors: [], values: { sku, name: "Mới", categoryCode: "DM-0001", unit: "Máy", salePrice: 150000 } });
const orderRow = (rowNumber = 2, sku = "LAP-01"): InputRow => ({ rowNumber, errors: [], values: { orderNumber: "DH-NEW", orderDate: "2026-10-05", warehouseCode: "KHO-0001", customerName: "Khách", customerPhone: "0901234567", shippingAddress: "Địa chỉ", sku, quantity: 2, unitPrice: 100, discount: 10 } });
afterEach(() => { vi.unstubAllGlobals(); configureApi(""); });

describe("Excel input validation", () => {
  it("normalizes duplicate SKU and marks both rows, including existing records", () => {
    const result = validateRows("products", [productRow(2, " lap-01 "), productRow(3, "LAP-01")], refs);
    expect(result.every(r => r.duplicate && r.errors.some(e => e.includes("trùng trong file")) && r.errors.some(e => e.includes("đã tồn tại")))).toBe(true);
  });
  it("rejects invalid numeric data, inactive or parent categories, preserves input", () => {
    const row = productRow(); row.values.salePrice = "1,500"; row.values.minimumStockLevel = -1;
    const result = validateRows("products", [row], { ...refs, categories: [{ ...refs.categories[0], isActive: false }] });
    expect(result[0].errors.join(" ")).toMatch(/Giá bán.*Tồn kho tối thiểu/s);
    expect(result[0].errors.join(" ")).toContain("Mã danh mục");
    expect(row.values.isActive).toBeUndefined();
    expect(validateRows("products", [productRow()], refs)[0].errors).toEqual([]);
  });
  it("groups distinct order products and rejects inconsistent header data on every line", () => {
    const secondProduct = { ...refs.products[0], productId: 2, sku: "LAP-02" };
    const rows = [orderRow(), orderRow(3, "LAP-02")];
    const lookup = { ...refs, products: [...refs.products, secondProduct] };
    expect(validateRows("orders", rows, lookup).every(r => r.errors.length === 0)).toBe(true);
    rows[1].values.shippingAddress = "Địa chỉ khác";
    expect(validateRows("orders", rows, lookup).every(r => r.errors.some(e => e.includes("không thống nhất")))).toBe(true);
  });
  it("blocks repeated products, old orders and statuses that would replay stock", () => {
    const rows = [orderRow(), orderRow(3)]; rows[0].values.status = "Completed"; rows[1].values.customerPhone = 901234567;
    const result = validateRows("orders", rows, { ...refs, orders: [{ orderNumber: "DH-NEW" } as References["orders"][number]] });
    expect(result.every(r => r.duplicate)).toBe(true);
    expect(result[0].errors.join(" ")).toContain("chỉ nhận Draft");
    expect(result[1].errors.join(" ")).toContain("phải là văn bản");
  });
  it("rejects nonexistent dates, money beyond the line total and fractional quantities", () => {
    const row = orderRow(); row.values.orderDate = "2026-02-30"; row.values.quantity = 1.5; row.values.discount = 200;
    const errors = validateRows("orders", [row], refs)[0].errors.join(" ");
    expect(errors).toContain("Ngày đặt"); expect(errors).toContain("Số lượng"); expect(errors).toContain("vượt giá trị dòng");
  });
});

describe("real XLSX files", () => {
  it("round-trips Vietnamese, phone leading zeros and exact discounts", async () => {
    const buffer = await buildWorkbook("orders", [orderRow().values], refs);
    const result = await readWorkbook("orders", buffer);
    expect(result[0].values).toMatchObject({ customerPhone: "0901234567", customerName: "Khách", discount: 10 });
    expect(validateRows("orders", result, refs)[0].errors).toEqual([]);
  });
  it("keeps examples out of the import sheet and includes live reference codes", async () => {
    const buffer = await buildWorkbook("products", [], refs, true);
    const workbook = new ExcelJS.Workbook(); await workbook.xlsx.load(buffer);
    expect(workbook.getWorksheet("SanPham")!.actualRowCount).toBe(1);
    expect(workbook.getWorksheet("ViDu")!.actualRowCount).toBe(2);
    expect(workbook.getWorksheet("ThamChieu")!.getCell("B2").value).toBe("DM-0001");
    await expect(readWorkbook("products", buffer)).rejects.toThrow("chưa có dòng dữ liệu");
  });
  it("reports formula cells without evaluating their cached value", async () => {
    const workbook = new ExcelJS.Workbook(), sheet = workbook.addWorksheet("SanPham");
    sheet.columns = schemas.products.columns.map(c => ({ header: c.label, key: c.key }));
    sheet.addRow(productRow().values); sheet.getCell("E2").value = { formula: "1+1", result: 2 };
    const buffer = await workbook.xlsx.writeBuffer();
    const rows = await readWorkbook("products", new Uint8Array(buffer).slice().buffer);
    expect(rows[0].errors.join(" ")).toContain("Không nhận công thức");
  });
  it("rejects missing headers and corrupted workbooks", async () => {
    const workbook = new ExcelJS.Workbook(); workbook.addWorksheet("SanPham").addRow(["Tên sản phẩm"]);
    await expect(readWorkbook("products", new Uint8Array(await workbook.xlsx.writeBuffer()).slice().buffer)).rejects.toThrow("Thiếu cột bắt buộc");
    await expect(readWorkbook("products", new TextEncoder().encode("not xlsx").buffer)).rejects.toThrow("Không đọc được");
  });
});

describe("read-only exports", () => {
  it("fetches every filtered page and product detail to retain descriptions", async () => {
    const urls: string[] = [];
    vi.stubGlobal("fetch", vi.fn(async (url: string, init: RequestInit) => {
      expect(init.method || "GET").toBe("GET"); urls.push(url);
      const body = url.startsWith("/api/products/1") ? refs.products[0] : { items: [refs.products[0]], totalPages: 1, totalItems: 1 };
      return new Response(JSON.stringify(body));
    }));
    const rows = await exportRows("products", new URLSearchParams("search=LAP&categoryId=1&page=3"), refs);
    expect(urls[0]).toContain("search=LAP&categoryId=1&page=1&pageSize=100");
    expect(rows[0]).toMatchObject({ description: "Mô tả thật", categoryCode: "DM-0001" });
  });
  it("paginates until the final page and fails instead of truncating oversized exports", async () => {
    let calls = 0;
    vi.stubGlobal("fetch", vi.fn(async () => { calls++; return new Response(JSON.stringify({ items: [calls], totalPages: 2, totalItems: 2 })); }));
    expect(await boundedPages<number>("/api/sales?status=Draft")).toEqual([1, 2]);
    vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify({ items: [], totalPages: 21, totalItems: 2001 }))));
    await expect(boundedPages("/api/sales")).rejects.toThrow("hơn 2000");
  });
});
