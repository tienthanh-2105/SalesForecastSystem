import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { api, send } from "../lib/api";
import type {
  Category,
  Order,
  Page,
  Product,
  Section,
  Warehouse,
} from "../types";
import { sections } from "../app/config";
import { DataTable, rowId, type Row } from "../components/DataTable";
import { Toolbar } from "../components/Toolbar";
import { Modal } from "../components/Modal";
import { useToast } from "../components/Toast";
import { CategoryForm } from "./categories/CategoryForm";
import { WarehouseForm } from "./warehouses/WarehouseForm";
import { ProductForm } from "./products/ProductForm";
import { OrderForm } from "./orders/OrderForm";
interface Editor {
  entity: Row | null;
  view: boolean;
}
export function ManagementPage({ section }: { section: Section }) {
  const [params, setParams] = useSearchParams();
  const client = useQueryClient();
  const toast = useToast();
  const [editor, setEditor] = useState<Editor | null>(null);
  const [confirmation, setConfirmation] = useState<{
    row: Row;
    kind: string;
  } | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [debounced, setDebounced] = useState(params.get("search") || "");
  const search = params.get("search") || "";
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(search), 300);
    return () => clearTimeout(timer);
  }, [search]);
  const categories = useQuery({
    queryKey: ["categories"],
    queryFn: () => api<Category[]>("/api/categories"),
    enabled: section === "categories" || section === "products",
  });
  const page = Math.max(1, Number(params.get("page")) || 1);
  const query = new URLSearchParams();
  query.set("page", String(page));
  query.set("pageSize", "10");
  if (debounced) query.set("search", debounced);
  const allowedFilters =
    section === "products"
      ? ["categoryId", "stockStatus"]
      : section === "orders"
        ? ["status", "fromDate", "toDate"]
        : section === "warehouses"
          ? ["isActive"]
          : [];
  allowedFilters.forEach((key) => {
    const value = params.get(key);
    if (value) query.set(key, value);
  });
  const data = useQuery({
    queryKey: ["list", section, query.toString()],
    queryFn: ({ signal }) =>
      api<Page<Row>>(
        `/api/${section === "orders" ? "sales" : section}?${query}`,
        { signal },
      ),
    enabled: section !== "categories",
  });
  let rows = data.data?.items || [];
  if (section === "categories") {
    const all = categories.data || [];
    const seen = new Set<number>();
    const ordered: Category[] = [];
    const visit = (c: Category) => {
      if (seen.has(c.categoryId)) return;
      seen.add(c.categoryId);
      ordered.push(c);
      all
        .filter((child) => child.parentCategoryId === c.categoryId)
        .forEach(visit);
    };
    all.filter((c) => !c.parentCategoryId).forEach(visit);
    all.forEach(visit);
    rows = ordered.filter(
      (c) =>
        (!debounced ||
          `${c.code} ${c.name} ${c.description || ""}`
            .toLocaleLowerCase("vi")
            .includes(debounced.toLocaleLowerCase("vi"))) &&
        (!params.get("isActive") ||
          c.isActive === (params.get("isActive") === "true")),
    );
  }
  const loading =
    section === "categories" ? categories.isPending : data.isPending;
  const failure = section === "categories" ? categories.error : data.error;
  const total =
    section === "categories" ? rows.length : data.data?.totalItems || 0;
  const pages =
    section === "categories" ? 1 : Math.max(1, data.data?.totalPages || 1);
  const change = (key: string, value: string) => {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value);
    else next.delete(key);
    if (key !== "page") next.delete("page");
    setParams(next, { replace: key === "search" });
  };
  const refresh = () => {
    void client.invalidateQueries({ queryKey: ["list", section] });
    if (section === "categories" || section === "products")
      void client.invalidateQueries({ queryKey: ["categories"] });
  };
  const saved = () => {
    setEditor(null);
    toast("Đã lưu thành công.");
    void client.invalidateQueries();
  };
  const action = async (row: Row, kind: string) => {
    setError("");
    if (kind === "edit" || kind === "view") {
      setBusy(true);
      try {
        const entity =
          section === "categories"
            ? row
            : await api<Row>(
                `/api/${section === "orders" ? "sales" : section}/${rowId(section, row)}`,
              );
        setEditor({ entity, view: kind === "view" });
      } catch (e) {
        setError((e as Error).message);
      } finally {
        setBusy(false);
      }
    } else setConfirmation({ row, kind });
  };
  return (
    <section>
      <div className="page-heading">
        <span className="overline text-brand fs-3">
          {sections[section].heading}
        </span>
      </div>
      <Toolbar
        section={section}
        params={params}
        change={change}
        categories={categories.data || []}
        refresh={refresh}
        busy={busy || data.isFetching || categories.isFetching}
        add={() => {
          setError("");
          setEditor({ entity: null, view: false });
        }}
      />
      {error && (
        <div role="alert" className="alert alert-danger">
          {error}
        </div>
      )}
      <div className="data-card">
        {loading ? (
          <div className="data-state" role="status">
            Đang tải dữ liệu...
          </div>
        ) : failure ? (
          <div className="data-state data-error" role="alert">
            {failure.message}
            <button className="btn btn-outline-danger" onClick={refresh}>
              Thử lại
            </button>
          </div>
        ) : !rows.length ? (
          <div className="data-state">Không tìm thấy dữ liệu phù hợp.</div>
        ) : (
          <DataTable
            section={section}
            rows={rows}
            categories={categories.data || []}
            action={(row, kind) => {
              if (!busy) void action(row, kind);
            }}
          />
        )}
        <footer className="d-flex justify-content-between p-3">
          <small>
            {total} kết quả · Trang {section === "categories" ? 1 : page}/
            {pages}
          </small>
          <div>
            <button
              className="btn btn-sm btn-light border"
              aria-label="Trang trước"
              disabled={page <= 1 || section === "categories"}
              onClick={() => change("page", String(page - 1))}
            >
              ‹
            </button>
            <button
              className="btn btn-sm btn-light border"
              aria-label="Trang sau"
              disabled={page >= pages}
              onClick={() => change("page", String(page + 1))}
            >
              ›
            </button>
          </div>
        </footer>
      </div>
      {editor &&
        (section === "categories" ? (
          <CategoryForm
            entity={editor.entity as Category | null}
            onClose={() => setEditor(null)}
            onSaved={saved}
          />
        ) : section === "warehouses" ? (
          <WarehouseForm
            entity={editor.entity as Warehouse | null}
            onClose={() => setEditor(null)}
            onSaved={saved}
          />
        ) : section === "products" ? (
          <ProductForm
            entity={editor.entity as Product | null}
            onClose={() => setEditor(null)}
            onSaved={saved}
          />
        ) : section === "orders" ? (
          <OrderForm
            entity={editor.entity as Order | null}
            viewOnly={editor.view}
            onClose={() => setEditor(null)}
            onSaved={saved}
          />
        ) : null)}
      {confirmation && (
        <Modal
          title="Xác nhận thao tác"
          onClose={() => {
            setConfirmation(null);
            setError("");
          }}
          busy={busy}
        >
          <p>
            {confirmation.kind === "delete"
              ? section === "warehouses"
                ? "Kho sẽ ngừng hoạt động và không còn xuất hiện khi tạo đơn mới. Bạn có chắc muốn tiếp tục?"
                : section === "products"
                  ? "Sản phẩm sẽ chuyển sang ngừng kinh doanh. Bạn có chắc muốn tiếp tục?"
                  : "Bạn có chắc muốn xóa dữ liệu này?"
              : `Bạn có chắc muốn ${({ submit: "xác nhận đơn", dispatch: "bắt đầu giao", complete: "xác nhận đã giao", cancel: "hủy đơn" } as Record<string, string>)[confirmation.kind]}?`}
          </p>
          {error && (
            <div className="alert alert-danger" role="alert">
              {error}
            </div>
          )}
          <button
            className={`btn ${["delete", "cancel"].includes(confirmation.kind) ? "btn-danger" : "btn-primary"}`}
            disabled={busy}
            onClick={async () => {
              setBusy(true);
              setError("");
              try {
                const resource = section === "orders" ? "sales" : section;
                const id = rowId(section, confirmation.row);
                await send(
                  `/api/${resource}/${id}${confirmation.kind === "delete" ? "" : `/${confirmation.kind}`}`,
                  confirmation.kind === "delete" ? "DELETE" : "POST",
                );
                setConfirmation(null);
                toast("Thao tác thành công.");
                void client.invalidateQueries();
              } catch (e) {
                setError((e as Error).message);
              } finally {
                setBusy(false);
              }
            }}
          >
            {busy ? "Đang xử lý..." : "Xác nhận"}
          </button>
        </Modal>
      )}
    </section>
  );
}
