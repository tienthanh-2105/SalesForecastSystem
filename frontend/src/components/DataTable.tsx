import type {
  Category,
  Customer,
  Order,
  Product,
  Section,
  UserRow,
  Warehouse,
} from "../types";
import { code, date, money } from "../lib/format";
import { roleLabels } from "../app/config";
import { StatusBadge } from "./StatusBadge";
export type Row = Category | Product | Warehouse | Order | UserRow | Customer;
export function rowId(section: Section, row: Row): number {
  return Number(
    section === "categories"
      ? (row as Category).categoryId
      : section === "products"
        ? (row as Product).productId
        : section === "warehouses"
          ? (row as Warehouse).warehouseId
          : section === "orders"
            ? (row as Order).salesOrderId
            : section === "customers"
              ? (row as Customer).customerId
              : (row as UserRow).userId,
  );
}
export function DataTable({
  section,
  rows,
  categories,
  action,
}: {
  section: Section;
  rows: Row[];
  categories: Category[];
  action: (row: Row, kind: string) => void;
}) {
  const headers =
    section === "categories"
      ? ["Mã danh mục", "Tên danh mục", "Loại danh mục", "Mô tả", "Trạng thái"]
      : section === "products"
        ? [
            "Mã hệ thống",
            "SKU",
            "Sản phẩm",
            "Danh mục",
            "Đơn vị",
            "Giá bán",
            "Tồn kho",
            "Trạng thái",
          ]
        : section === "warehouses"
          ? ["Mã kho", "Tên kho", "Địa chỉ", "Trạng thái"]
          : section === "orders"
            ? [
                "Mã đơn",
                "Khách hàng",
                "Ngày đặt",
                "Giao hàng",
                "Tổng tiền",
                "Trạng thái",
              ]
            : section === "customers"
              ? ["Mã khách hàng", "Họ tên", "Số điện thoại", "Email", "Địa chỉ"]
              : [
                "Mã người dùng",
                "Tên người dùng",
                "Email",
                "Số điện thoại",
                "Vai trò",
                "Trạng thái",
                "Ngày tạo",
              ];
  return (
    <div className="table-responsive">
      <table className="table admin-table align-middle mb-0">
        <thead>
          <tr>
            {headers.map((h) => (
              <th key={h}>{h}</th>
            ))}
            <th>Thao tác</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={rowId(section, row)}>
              <Cells section={section} row={row} categories={categories} />
              {(
                <td className="text-end text-nowrap">
                  <div className="table-actions">
                    {section === "orders" ? (
                      <OrderActions
                        row={row as Order}
                        action={(kind) => action(row, kind)}
                      />
                    ) : (
                      <>
                        <Action
                          label="Sửa"
                          icon="pencil"
                          onClick={() => action(row, "edit")}
                        />
                        {section === "customers" && <Action label="Lịch sử đơn hàng" icon="clock-history" onClick={() => action(row, "history")} />}
                        {section !== "customers" && (section !== "users" || (row as UserRow).roleName !== "Admin") && (section !== "warehouses" ||
                          (row as Warehouse).isActive) && (
                          <Action
                            label={
                              section === "warehouses"
                                ? "Ngừng hoạt động"
                                : section === "products"
                                  ? "Ngừng kinh doanh"
                                  : "Xóa"
                            }
                            icon={section === "warehouses" ? "power" : "trash"}
                            onClick={() => action(row, "delete")}
                          />
                        )}
                      </>
                    )}
                  </div>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
function Cells({
  section,
  row,
  categories,
}: {
  section: Section;
  row: Row;
  categories: Category[];
}) {
  if (section === "categories") {
    const c = row as Category;
    return (
      <>
        <td>
          <span className="id-badge">{c.code || code("DM", c.categoryId)}</span>
        </td>
        <td>
          {c.parentCategoryId ? "↳ " : "▱ "}
          {c.name}
        </td>
        <td>
          {categories.find((p) => p.categoryId === c.parentCategoryId)?.name ||
            "Danh mục gốc"}
        </td>
        <td>{c.description}</td>
        <td>
          <StatusBadge active={c.isActive} />
        </td>
      </>
    );
  }
  if (section === "products") {
    const p = row as Product;
    return (
      <>
        <td>
          <span className="id-badge">{code("SP", p.productId)}</span>
        </td>
        <td>{p.sku}</td>
        <td>
          <div className="product-cell product-list-cell">
            <div className="product-list-thumb">
              {p.imageUrl ? <img src={p.imageUrl} alt={p.name} /> : <i aria-hidden="true" className="bi bi-image" />}
            </div>
            <div>
              <span className="table-primary-text">{p.name}</span>
              <small className="table-secondary-text">Tối thiểu: {p.minimumStockLevel}</small>
            </div>
          </div>
        </td>
        <td>
          {p.categoryName ||
            categories.find((c) => c.categoryId === p.categoryId)?.name}
        </td>
        <td>{p.unit}</td>
        <td>{money(p.salePrice)}</td>
        <td>{p.quantityOnHand ?? 0}</td>
        <td>
          <span
            className={`status-badge status-${!p.isActive || p.stockStatus === "OutOfStock" ? "inactive" : p.stockStatus === "Low" ? "warning" : "active"}`}
          >
            {!p.isActive
              ? "Ngừng kinh doanh"
              : p.stockStatus === "OutOfStock"
                ? "Hết hàng"
                : p.stockStatus === "Low"
                  ? "Sắp hết"
                  : "Còn hàng"}
          </span>
        </td>
      </>
    );
  }
  if (section === "warehouses") {
    const w = row as Warehouse;
    return (
      <>
        <td>{code("KHO", w.warehouseId)}</td>
        <td>{w.name}</td>
        <td>{w.address}</td>
        <td>
          <StatusBadge active={w.isActive} />
        </td>
      </>
    );
  }
  if (section === "orders") {
    const o = row as Order;
    return (
      <>
        <td>
          {o.orderNumber}
          <small className="table-secondary-text">
            {code("DH", o.salesOrderId)}
          </small>
        </td>
        <td>
          {o.customerName || "Khách lẻ"}
          <small className="d-block">{o.customerPhone}</small>
        </td>
        <td>{date(o.orderDate)}</td>
        <td>{o.shippingAddress || "Nhận tại cửa hàng"}</td>
        <td>{money(o.totalAmount)}</td>
        <td>
          <StatusBadge status={o.status} />
        </td>
      </>
    );
  }
  if (section === "customers") {
    const c = row as Customer;
    return <><td>{code("KH", c.customerId)}</td><td>{c.fullName}</td><td>{c.phoneNumber || "—"}</td><td>{c.email || "—"}</td><td>{c.address || "—"}</td></>;
  }
  const u = row as UserRow;
  return (
    <>
      <td><span className="id-badge">{u.code || code("ND", u.userId)}</span></td>
      <td>{u.fullName}</td>
      <td>{u.email}</td>
      <td>{u.phoneNumber || "—"}</td>
      <td>{roleLabels[u.roleName] || u.roleName}</td>
      <td>
        <span
          className={`status-badge status-${u.status === "Active" ? "active" : "inactive"}`}
        >
          {u.status === "Active" ? "Đang hoạt động" : "Đã khóa"}
        </span>
      </td>
      <td>{date(u.createdAt)}</td>
    </>
  );
}
function Action({
  label,
  icon,
  onClick,
  disabled = false,
  reason,
}: {
  label: string;
  icon: string;
  onClick: () => void;
  disabled?: boolean;
  reason?: string;
}) {
  return (
    <button
      type="button"
      className="table-action"
      title={reason || label}
      aria-label={label}
      disabled={disabled}
      onClick={onClick}
    >
      <i className={`bi bi-${icon}`} />
    </button>
  );
}
function OrderActions({
  row,
  action,
}: {
  row: Order;
  action: (kind: string) => void;
}) {
  return (
    <>
      <Action label="Xem chi tiết" icon="eye" onClick={() => action("view")} />
      <Action label="Sửa" icon="pencil" onClick={() => action("edit")} disabled={row.status !== "Draft"} reason={row.status !== "Draft" ? "Chỉ được sửa đơn hàng ở trạng thái Nháp." : undefined} />
      <Action label="Xóa" icon="trash" onClick={() => action("delete")} disabled={row.status !== "Draft"} reason={row.status !== "Draft" ? "Chỉ được xóa đơn hàng ở trạng thái Nháp; đơn đã xác nhận dùng thao tác Hủy đơn." : undefined} />
      {row.status === "Draft" && (
        <>
          <Action
            label="Xác nhận đơn"
            icon="send-check"
            onClick={() => action("submit")}
          />
        </>
      )}
      {row.status === "Pending" && (
        <Action
          label="Bắt đầu giao"
          icon="truck"
          onClick={() => action("dispatch")}
        />
      )}{" "}
      {row.status === "Delivering" && (
        <Action
          label="Xác nhận đã giao"
          icon="check2-circle"
          onClick={() => action("complete")}
        />
      )}{" "}
      {["Draft", "Pending", "Delivering"].includes(row.status) && (
        <Action
          label="Hủy đơn"
          icon="x-circle"
          onClick={() => action("cancel")}
        />
      )}
    </>
  );
}
