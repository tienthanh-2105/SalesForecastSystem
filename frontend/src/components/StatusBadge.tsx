import type { OrderStatus } from "../types";
export const orderLabels: Record<OrderStatus, string> = {
  Draft: "Nháp",
  Pending: "Chờ xử lý",
  Delivering: "Đang giao",
  Completed: "Đã giao",
  Cancelled: "Đã hủy",
};
export function StatusBadge({
  active,
  status,
}: {
  active?: boolean;
  status?: string;
}) {
  return (
    <span
      className={`status-badge status-${status === "Cancelled" || active === false ? "inactive" : status === "Draft" ? "info" : status === "Pending" ? "warning" : status === "Delivering" ? "delivering" : "active"}`}
    >
      {status
        ? orderLabels[status as OrderStatus] || status
        : active
          ? "Đang hoạt động"
          : "Ngừng hoạt động"}
    </span>
  );
}
