import { api } from "../../lib/api";
import type { Category, Order, Page, Product, Warehouse } from "../../types";
import { MAX_ROWS, emptyReferences, type ExcelSection, type References, type Scalar } from "./schema";

// A limit must fail explicitly instead of silently exporting a partial file.
export async function boundedPages<T>(resource: string, signal?: AbortSignal): Promise<T[]> {
  const result: T[] = [];
  for (let page = 1; page <= MAX_ROWS / 100 + 1; page++) {
    const data = await api<Page<T>>(`${resource}${resource.includes("?") ? "&" : "?"}page=${page}&pageSize=100`, { signal });
    if (!Array.isArray(data.items) || !Number.isFinite(data.totalPages) || !Number.isFinite(data.totalItems)) throw new Error("Không đọc được danh sách dữ liệu. Vui lòng thử lại.");
    if (data.totalItems > MAX_ROWS || result.length + data.items.length > MAX_ROWS) throw new Error(`Có hơn ${MAX_ROWS} bản ghi. Hãy thu hẹp bộ lọc trước khi xuất hoặc kiểm tra file.`);
    result.push(...data.items);
    if (page >= data.totalPages) return result;
    if (!data.items.length) throw new Error("Danh sách thay đổi trong lúc tải. Vui lòng thử lại.");
  }
  throw new Error("Danh sách vượt giới hạn tải. Hãy thu hẹp bộ lọc.");
}

export async function loadReferences(section: ExcelSection, checkDuplicates: boolean, signal?: AbortSignal): Promise<References> {
  const refs = emptyReferences();
  if (section === "products") {
    const [categories, products] = await Promise.all([
      api<Category[]>("/api/categories", { signal }),
      checkDuplicates ? boundedPages<Product>("/api/products", signal) : Promise.resolve([]),
    ]);
    refs.categories = categories; refs.products = products;
  } else {
    const [warehouses, products, orders] = await Promise.all([
      boundedPages<Warehouse>("/api/warehouses", signal),
      boundedPages<Product>("/api/products", signal),
      checkDuplicates ? boundedPages<Order>("/api/sales", signal) : Promise.resolve([]),
    ]);
    refs.warehouses = warehouses; refs.products = products; refs.orders = orders;
  }
  return refs;
}

export function exportResource(section: ExcelSection, params: URLSearchParams) {
  const query = new URLSearchParams();
  for (const key of section === "products" ? ["search", "categoryId", "stockStatus"] : ["search", "status", "fromDate", "toDate"]) {
    const value = params.get(key);
    if (value) query.set(key, value);
  }
  return `/api/${section === "products" ? "products" : "sales"}?${query}`;
}

async function mapDetails<T, R>(source: T[], get: (record: T) => Promise<R>): Promise<R[]> {
  const results: R[] = new Array(source.length);
  let index = 0;
  await Promise.all(Array.from({ length: Math.min(4, source.length) }, async () => {
    while (index < source.length) { const current = index++; results[current] = await get(source[current]); }
  }));
  return results;
}

export async function exportRows(section: ExcelSection, params: URLSearchParams, refs: References, signal?: AbortSignal): Promise<Record<string, Scalar>[]> {
  if (section === "products") {
    const list = await boundedPages<Product>(exportResource(section, params), signal);
    const products = await mapDetails(list, p => api<Product>(`/api/products/${p.productId}`, { signal }));
    return products.map(p => ({ sku: p.sku, name: p.name, categoryCode: refs.categories.find(c => c.categoryId === p.categoryId)?.code || "", unit: p.unit, salePrice: p.salePrice, minimumStockLevel: p.minimumStockLevel, isActive: p.isActive, description: p.description || "" }));
  }
  const orders = await boundedPages<Order>(exportResource(section, params), signal);
  const details = await mapDetails(orders, o => api<Order>(`/api/sales/${o.salesOrderId}`, { signal }));
  const rows: Record<string, Scalar>[] = [];
  for (const order of details) {
    if (!Array.isArray(order.items) || !order.items.length) throw new Error(`Đơn ${order.orderNumber} chưa có chi tiết sản phẩm; không thể xuất theo mẫu. Hãy hoàn thiện đơn hoặc thu hẹp bộ lọc.`);
    for (const item of order.items) {
      const product = refs.products.find(p => p.productId === item.productId);
      if (!product) throw new Error(`Không tìm thấy sản phẩm của đơn ${order.orderNumber}. Vui lòng thử lại.`);
      rows.push({ orderNumber: order.orderNumber, orderDate: order.orderDate.slice(0, 10), warehouseCode: `KHO-${String(order.warehouseId).padStart(4, "0")}`, customerName: order.customerName || "", customerPhone: order.customerPhone || "", shippingAddress: order.shippingAddress || "", sku: product.sku, quantity: item.quantity, unitPrice: item.unitPrice, discount: item.discount, status: order.status, notes: order.notes || "" });
      if (rows.length > MAX_ROWS) throw new Error(`Đơn hàng có hơn ${MAX_ROWS} dòng sản phẩm. Hãy thu hẹp bộ lọc trước khi xuất.`);
    }
  }
  return rows;
}
