import { api, send } from "../../lib/api";
import type { Purchase, PurchaseItem } from "../../types";
export class PartialPurchaseError extends Error {
  constructor(public id: number, message: string) { super(`Phiếu ${id} đã lưu một phần. ${message}`); }
}
export async function savePurchase(existing: Purchase | null, header: unknown, items: PurchaseItem[]) {
  if (!items.length) throw new Error("Phiếu nhập phải có ít nhất một sản phẩm.");
  if (new Set(items.map(item => item.productId)).size !== items.length) throw new Error("Không chọn trùng sản phẩm.");
  if (items.some(item => !Number.isInteger(item.productId) || item.productId <= 0 || !Number.isInteger(item.quantity) || item.quantity <= 0 || !Number.isFinite(item.unitPrice) || item.unitPrice < 0)) throw new Error("Sản phẩm, số lượng hoặc giá nhập không hợp lệ.");
  const saved = await send<Purchase>(`/api/purchases${existing ? `/${existing.purchaseOrderId}` : ""}`, existing ? "PUT" : "POST", header);
  try {
    const retained = new Set(items.map(item => item.purchaseOrderItemId));
    for (const old of existing?.items || []) if (!retained.has(old.purchaseOrderItemId)) await send(`/api/purchases/${saved.purchaseOrderId}/items/${old.purchaseOrderItemId}`, "DELETE");
    for (const item of items) await send(`/api/purchases/${saved.purchaseOrderId}/items${item.purchaseOrderItemId ? `/${item.purchaseOrderItemId}` : ""}`, item.purchaseOrderItemId ? "PUT" : "POST", { productId: item.productId, quantity: item.quantity, unitPrice: item.unitPrice });
    return await api<Purchase>(`/api/purchases/${saved.purchaseOrderId}`);
  } catch (e) { throw new PartialPurchaseError(saved.purchaseOrderId, (e as Error).message); }
}
