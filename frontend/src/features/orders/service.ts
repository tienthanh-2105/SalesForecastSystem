import { api, send } from "../../lib/api";
import type { Order, OrderItem } from "../../types";
export const lineTotal = (item: OrderItem) =>
  Math.max(0, item.quantity * item.unitPrice - item.discount);
export function validateItems(items: OrderItem[]) {
  if (!items.length) return "Đơn hàng phải có ít nhất một sản phẩm.";
  if (new Set(items.map((i) => i.productId)).size !== items.length)
    return "Không được chọn trùng sản phẩm.";
  if (
    items.some(
      (i) =>
        !Number.isInteger(i.productId) ||
        i.productId <= 0 ||
        !Number.isInteger(i.quantity) ||
        i.quantity < 1 ||
        !Number.isFinite(i.unitPrice) ||
        i.unitPrice < 0 ||
        !Number.isFinite(i.discount) ||
        i.discount < 0 ||
        i.discount > i.quantity * i.unitPrice,
    )
  )
    return "Số lượng, giá hoặc giảm giá không hợp lệ.";
  return "";
}
export class PartialOrderError extends Error {
  constructor(
    public orderId: number,
    public causeMessage: string,
  ) {
    super(`Đơn ${orderId} đã được lưu một phần. ${causeMessage}`);
  }
}
export async function saveOrder(
  existing: Order | null,
  header: unknown,
  items: OrderItem[],
): Promise<Order> {
  const error = validateItems(items);
  if (error) throw new Error(error);
  const id = existing?.salesOrderId;
  const saved = await send<Order>(
    `/api/sales${id ? `/${id}` : ""}`,
    id ? "PUT" : "POST",
    header,
  );
  try {
    const retained = new Set(items.map((i) => i.salesOrderItemId));
    for (const old of existing?.items || [])
      if (!retained.has(old.salesOrderItemId))
        await send(
          `/api/sales/${saved.salesOrderId}/items/${old.salesOrderItemId}`,
          "DELETE",
        );
    for (const item of items)
      await send(
        `/api/sales/${saved.salesOrderId}/items${item.salesOrderItemId ? `/${item.salesOrderItemId}` : ""}`,
        item.salesOrderItemId ? "PUT" : "POST",
        {
          productId: item.productId,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          discount: item.discount,
        },
      );
    return await api<Order>(`/api/sales/${saved.salesOrderId}`);
  } catch (e) {
    throw new PartialOrderError(saved.salesOrderId, (e as Error).message);
  }
}
