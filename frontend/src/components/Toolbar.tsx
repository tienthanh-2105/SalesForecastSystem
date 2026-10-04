import type { Category, Section } from "../types";
import { sections } from "../app/config";
import { orderLabels } from "./StatusBadge";
export function Toolbar({
  section,
  params,
  change,
  categories,
  refresh,
  add,
  busy,
}: {
  section: Section;
  params: URLSearchParams;
  change: (key: string, value: string) => void;
  categories: Category[];
  refresh: () => void;
  add: () => void;
  busy: boolean;
}) {
  const select = (key: string, label: string, options: [string, string][]) => (
    <select
      className="form-select"
      aria-label={label}
      value={params.get(key) || ""}
      onChange={(e) => change(key, e.target.value)}
    >
      <option value="">{label}</option>
      {options.map(([value, text]) => (
        <option key={value} value={value}>
          {text}
        </option>
      ))}
    </select>
  );
  return (
    <div className="management-toolbar">
      <div className="management-search">
        <i className="bi bi-search" />
        <input
          className="form-control"
          aria-label="Tìm kiếm"
          placeholder={section === "customers" ? "Tìm tên, điện thoại, email khách hàng..." : `Tìm kiếm ${sections[section].label.toLowerCase()}...`}
          type="search"
          maxLength={200}
          value={params.get("search") || ""}
          onChange={(e) => change("search", e.target.value)}
        />
      </div>
      <div className="management-filters">
        {(section === "categories" || section === "warehouses") &&
          select("isActive", "Tất cả trạng thái", [
            ["true", "Đang hoạt động"],
            ["false", "Ngừng hoạt động"],
          ])}
        {section === "products" && (
          <>
            {select(
              "categoryId",
              "Tất cả danh mục",
              categories.map((c) => [String(c.categoryId), c.name]),
            )}
            {select("stockStatus", "Tất cả tồn kho", [
              ["Available", "Còn hàng"],
              ["Low", "Sắp hết hàng"],
              ["OutOfStock", "Hết hàng"],
            ])}
          </>
        )}
        {section === "orders" && (
          <>
            {select("status", "Tất cả trạng thái", Object.entries(orderLabels))}
            <input
              className="form-control"
              aria-label="Từ ngày"
              type="date"
              value={params.get("fromDate") || ""}
              onChange={(e) => change("fromDate", e.target.value)}
            />
            <input
              className="form-control"
              aria-label="Đến ngày"
              type="date"
              value={params.get("toDate") || ""}
              onChange={(e) => change("toDate", e.target.value)}
            />
          </>
        )}
      </div>
      <div className="page-actions toolbar-actions">
        <button
          className="btn btn-light border"
          disabled={busy}
          onClick={refresh}
        >
          <i className="bi bi-arrow-clockwise me-2" />
          Làm mới
        </button>
        {section !== "customers" && (
          <button className="btn btn-brand" onClick={add}>
            <i className="bi bi-plus-lg" />
            Thêm{" "}
            {section === "orders"
              ? "đơn hàng"
              : sections[section].label.toLowerCase()}
          </button>
        )}
      </div>
    </div>
  );
}
