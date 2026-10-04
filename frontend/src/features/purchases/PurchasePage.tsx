import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { allPages, api, send } from "../../lib/api";
import { code, date } from "../../lib/format";
import { Modal } from "../../components/Modal";
import { useToast } from "../../components/Toast";
import type { Page, Purchase, Supplier, Warehouse } from "../../types";
import { PurchaseForm } from "./PurchaseForm";
const labels = { Draft: "Nháp", Posted: "Đã nhập kho", Cancelled: "Đã hủy" };
export function PurchasePage() {
  const [params, setParams] = useSearchParams();
  const client = useQueryClient(), toast = useToast();
  const [editor, setEditor] = useState<{ entity: Purchase | null; view: boolean } | null>(null);
  const [supplier, setSupplier] = useState(false);
  const [confirmation, setConfirmation] = useState<{ row: Purchase; kind: string } | null>(null);
  const [busy, setBusy] = useState(false), [error, setError] = useState("");
  const page = Math.max(1, Number(params.get("page")) || 1);
  const query = new URLSearchParams({ page: String(page), pageSize: "10" });
  ["status", "fromDate", "toDate", "warehouseId", "supplierId"].forEach(key => { if (params.get(key)) query.set(key, params.get(key)!); });
  const list = useQuery({ queryKey: ["list", "purchases", query.toString()], queryFn: ({ signal }) => api<Page<Purchase>>(`/api/purchases?${query}`, { signal }) });
  const references = useQuery({ queryKey: ["purchase-references"], queryFn: async () => {
    const [warehouses, suppliers] = await Promise.all([allPages<Warehouse>("/api/warehouses"), allPages<Supplier>("/api/suppliers")]); return { warehouses, suppliers };
  } });
  const change = (key: string, value: string) => { const next = new URLSearchParams(params); if (value) next.set(key, value); else next.delete(key); if (key !== "page") next.delete("page"); setParams(next); };
  const saved = () => { setEditor(null); setSupplier(false); void client.invalidateQueries(); toast("Đã lưu thành công."); };
  const open = async (row: Purchase, view: boolean) => {
    setBusy(true); setError(""); try { setEditor({ entity: await api<Purchase>(`/api/purchases/${row.purchaseOrderId}`), view }); } catch (e) { setError((e as Error).message); } finally { setBusy(false); }
  };
  return <section><div className="page-heading"><span className="overline text-brand fs-3">Phiếu nhập hàng</span></div>
    <div className="management-toolbar">
      <div className="management-filters purchase-filters">
        <select className="form-select" aria-label="Trạng thái phiếu" value={params.get("status") || ""} onChange={e => change("status", e.target.value)}><option value="">Tất cả trạng thái</option>{Object.entries(labels).map(([key, text]) => <option key={key} value={key}>{text}</option>)}</select>
        <select className="form-select" aria-label="Lọc kho" value={params.get("warehouseId") || ""} onChange={e => change("warehouseId", e.target.value)}><option value="">Tất cả kho</option>{references.data?.warehouses.map(w => <option key={w.warehouseId} value={w.warehouseId}>{w.name}</option>)}</select>
        <select className="form-select purchase-supplier-filter" aria-label="Lọc nhà cung cấp" value={params.get("supplierId") || ""} onChange={e => change("supplierId", e.target.value)}><option value="">Tất cả nhà cung cấp</option>{references.data?.suppliers.map(s => <option key={s.supplierId} value={s.supplierId}>{s.name}</option>)}</select>
        <input className="form-control" type="date" aria-label="Từ ngày" value={params.get("fromDate") || ""} onChange={e => change("fromDate", e.target.value)} />
        <input className="form-control" type="date" aria-label="Đến ngày" value={params.get("toDate") || ""} onChange={e => change("toDate", e.target.value)} />
      </div>
      <div className="page-actions toolbar-actions"><button className="btn btn-light border" disabled={busy || list.isFetching} onClick={() => void client.invalidateQueries()}>Làm mới</button><button className="btn btn-brand" onClick={() => setSupplier(true)}>Thêm nhà cung cấp</button><button className="btn btn-brand" onClick={() => { setError(""); setEditor({ entity: null, view: false }); }}>Thêm phiếu nhập</button></div>
    </div>
    {error && <div className="alert alert-danger" role="alert">{error}</div>}
    {references.error && <div className="alert alert-warning" role="alert">Không tải được tên kho hoặc nhà cung cấp. <button onClick={() => void references.refetch()}>Thử lại</button></div>}
    <div className="data-card">{list.isPending ? <div className="data-state" role="status">Đang tải phiếu nhập...</div> : list.error ? <div className="data-state" role="alert">{list.error.message}<button onClick={() => void list.refetch()}>Thử lại</button></div> : !list.data?.items.length ? <div className="data-state">Chưa có phiếu nhập phù hợp.</div> : <div className="table-responsive"><table className="table admin-table"><thead><tr>{["Mã phiếu", "Ngày nhập", "Kho", "Nhà cung cấp", "Trạng thái", "Thao tác"].map(h => <th key={h}>{h}</th>)}</tr></thead><tbody>{list.data.items.map(row => <tr key={row.purchaseOrderId}><td>{row.orderNumber}</td><td>{date(row.orderDate)}</td><td>{references.data?.warehouses.find(w => w.warehouseId === row.warehouseId)?.name || code("KHO", row.warehouseId)}</td><td>{references.data?.suppliers.find(s => s.supplierId === row.supplierId)?.name || code("NCC", row.supplierId)}</td><td><span className={`status-badge status-${row.status === "Posted" ? "active" : row.status === "Cancelled" ? "inactive" : "warning"}`}>{labels[row.status]}</span></td><td><div className="d-flex gap-2 flex-wrap"><button className="btn btn-sm btn-light border" disabled={busy} onClick={() => void open(row, true)}>Xem</button>{row.status === "Draft" && <><button className="btn btn-sm btn-light border" disabled={busy} onClick={() => void open(row, false)}>Sửa</button>{[ ["post", "Xác nhận nhập kho"], ["cancel", "Hủy"], ["delete", "Xóa"] ].map(([kind, text]) => <button key={kind} className="btn btn-sm btn-light border" disabled={busy} onClick={() => { setError(""); setConfirmation({ row, kind }); }}>{text}</button>)}</>}</div></td></tr>)}</tbody></table></div>}
      <footer className="d-flex justify-content-between p-3"><small>{list.data?.totalItems || 0} kết quả · Trang {page}/{Math.max(1, list.data?.totalPages || 1)}</small><div><button className="btn btn-light border" aria-label="Trang trước" disabled={page <= 1} onClick={() => change("page", String(page - 1))}>‹</button><button className="btn btn-light border" aria-label="Trang sau" disabled={page >= (list.data?.totalPages || 1)} onClick={() => change("page", String(page + 1))}>›</button></div></footer>
    </div>
    {editor && <PurchaseForm entity={editor.entity} view={editor.view} onClose={() => setEditor(null)} onSaved={saved} />}
    {supplier && <SupplierForm onClose={() => setSupplier(false)} onSaved={saved} />}
    {confirmation && <Modal title="Xác nhận thao tác" onClose={() => { setConfirmation(null); setError(""); }} busy={busy}><p>{confirmation.kind === "post" ? "Xác nhận đã nhận đủ hàng? Thao tác này tăng tồn kho và phiếu không còn được sửa hoặc xóa." : confirmation.kind === "cancel" ? "Bạn muốn hủy phiếu nháp? Tồn kho không thay đổi." : "Bạn muốn xóa phiếu nháp này?"}</p>{error && <div className="alert alert-danger" role="alert">{error}</div>}<button className="btn btn-brand" disabled={busy} onClick={async () => { setBusy(true); setError(""); try { await send(`/api/purchases/${confirmation.row.purchaseOrderId}${confirmation.kind === "delete" ? "" : `/${confirmation.kind}`}`, confirmation.kind === "delete" ? "DELETE" : "POST"); setConfirmation(null); void client.invalidateQueries(); toast("Thao tác thành công."); } catch (e) { setError((e as Error).message); } finally { setBusy(false); } }}>Xác nhận</button></Modal>}
  </section>;
}
function SupplierForm({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const [error, setError] = useState("");
  const { register, handleSubmit, formState: { isSubmitting, errors } } = useForm({ defaultValues: { name: "", taxCode: "", email: "", phoneNumber: "", address: "" } });
  return <Modal title="Thêm nhà cung cấp" onClose={onClose} busy={isSubmitting}>
    <form onSubmit={handleSubmit(async values => {
      setError("");
      try {
        await send("/api/suppliers", "POST", { name: values.name.trim(), taxCode: values.taxCode, email: values.email.trim() || null, phoneNumber: values.phoneNumber.trim() || null, address: values.address.trim() || null, isActive: true });
        onSaved();
      } catch (e) { setError((e as Error).message); }
    })}>
      {error && <div className="alert alert-danger" role="alert">{error}</div>}
      <label>Tên nhà cung cấp <span className="text-danger">*</span><input className="form-control" maxLength={200} required {...register("name", { required: true, validate: value => !!value.trim() })} /></label>
      <label>Mã số thuế <span className="text-danger">*</span>
        <input className="form-control" maxLength={14} required placeholder="10 số hoặc 10 số-3 số" {...register("taxCode", {
          required: "Vui lòng nhập mã số thuế doanh nghiệp.",
          pattern: { value: /^[0-9]{10}(-[0-9]{3})?$/, message: "Mã số thuế phải gồm 10 chữ số hoặc có dạng 0123456789-001." },
        })} />
        {errors.taxCode && <small className="text-danger" role="alert">{errors.taxCode.message}</small>}
      </label>
      <a className="btn btn-outline-primary btn-sm align-self-start" href="https://tracuunnt.gdt.gov.vn" target="_blank" rel="noopener noreferrer">Tra cứu mã số thuế <i className="bi bi-box-arrow-up-right" aria-hidden="true" /></a>
      <small className="text-muted">Đối chiếu tên doanh nghiệp và tình trạng hoạt động trên cổng Cục Thuế. Đúng định dạng không đồng nghĩa đã xác minh.</small>
      <label>Email<input className="form-control" type="email" maxLength={100} {...register("email")} /></label>
      <label>Số điện thoại<input className="form-control" type="tel" maxLength={15} pattern="[0-9+(). \-]+" {...register("phoneNumber")} /></label>
      <label>Địa chỉ<input className="form-control" maxLength={255} {...register("address")} /></label>
      <button className="btn btn-brand" disabled={isSubmitting}>Lưu nhà cung cấp</button>
    </form>
  </Modal>;
}
