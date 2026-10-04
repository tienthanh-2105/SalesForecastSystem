import { useState } from "react";
import { useFieldArray, useForm, useWatch } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { Modal } from "../../components/Modal";
import { allPages, api } from "../../lib/api";
import { code, localDate, money } from "../../lib/format";
import type { Product, Purchase, PurchaseItem, Supplier, Warehouse } from "../../types";
import { PartialPurchaseError, savePurchase } from "./service";
interface Fields { orderNumber: string; orderDate: string; warehouseId: string; supplierId: string; notes: string; items: PurchaseItem[] }
const defaults = (entity: Purchase | null): Fields => ({ orderNumber: entity?.orderNumber || `NH-${localDate().replaceAll("-", "")}-${crypto.randomUUID().slice(0, 8).toUpperCase()}`, orderDate: entity?.orderDate.slice(0, 10) || localDate(), warehouseId: String(entity?.warehouseId || ""), supplierId: String(entity?.supplierId || ""), notes: entity?.notes || "", items: entity?.items || [{ productId: 0, quantity: 1, unitPrice: 0 }] });
export function PurchaseForm({ entity, view, onClose, onSaved }: { entity: Purchase | null; view: boolean; onClose: () => void; onSaved: () => void }) {
  const [existing, setExisting] = useState(entity);
  const [error, setError] = useState("");
  const [recoverId, setRecoverId] = useState<number | null>(null);
  const [recovering, setRecovering] = useState(false);
  const { register, control, reset, handleSubmit, formState: { isSubmitting } } = useForm<Fields>({ defaultValues: defaults(entity) });
  const { fields, append, remove } = useFieldArray({ control, name: "items" });
  const items = useWatch({ control, name: "items" }) || [];
  const options = useQuery({ queryKey: ["purchase-options"], queryFn: async () => {
    const [products, warehouses, suppliers] = await Promise.all([allPages<Product>("/api/products?isActive=true"), allPages<Warehouse>("/api/warehouses?isActive=true"), allPages<Supplier>("/api/suppliers?isActive=true")]);
    return { products, warehouses, suppliers };
  } });
  const reload = async (id: number) => {
    setRecovering(true);
    try { const saved = await api<Purchase>(`/api/purchases/${id}`); setExisting(saved); reset(defaults(saved)); setRecoverId(null); setError("Đã tải lại phiếu lưu một phần. Kiểm tra các dòng hàng và tiếp tục lưu trên phiếu này."); }
    catch (e) { setError((e as Error).message); }
    finally { setRecovering(false); }
  };
  return <Modal title={view ? "Chi tiết phiếu nhập" : existing ? "Sửa phiếu nhập" : "Thêm phiếu nhập"} wide busy={isSubmitting || recovering} onClose={onClose}>
    <form onSubmit={handleSubmit(async values => {
      setError("");
      try { await savePurchase(existing, { orderNumber: values.orderNumber.trim(), warehouseId: Number(values.warehouseId), supplierId: Number(values.supplierId), orderDate: `${values.orderDate}T00:00:00`, notes: values.notes.trim() || null }, values.items); onSaved(); }
      catch (e) { if (e instanceof PartialPurchaseError) { setRecoverId(e.id); setError(e.message); await reload(e.id); } else setError((e as Error).message); }
    })}>
      {error && <div role="alert" className="alert alert-warning">{error}{recoverId && <button type="button" onClick={() => void reload(recoverId)} disabled={recovering}>Tải lại phiếu đã lưu</button>}</div>}
      {options.isError && <div role="alert" className="alert alert-danger">Không tải được dữ liệu lựa chọn. <button type="button" onClick={() => void options.refetch()}>Thử lại</button></div>}
      {!view && options.data && !options.data.suppliers.length && <div className="alert alert-warning">Chưa có nhà cung cấp đang hoạt động. Đóng phiếu và chọn Thêm nhà cung cấp trước khi nhập hàng.</div>}
      <fieldset disabled={view || isSubmitting || recovering || !!recoverId}>
        <div className="form-grid">
          <label>Mã phiếu <span className="text-danger">*</span><input className="form-control" required readOnly {...register("orderNumber", { required: true })} /></label>
          <label>Ngày nhập <span className="text-danger">*</span><input className="form-control" type="date" required {...register("orderDate", { required: true })} /></label>
          <label>Kho nhập <span className="text-danger">*</span><select className="form-select" required {...register("warehouseId", { required: true })}><option value="">Chọn kho</option>{existing && !options.data?.warehouses.some(w => w.warehouseId === existing.warehouseId) && <option value={existing.warehouseId}>{code("KHO", existing.warehouseId)}</option>}{options.data?.warehouses.map(w => <option key={w.warehouseId} value={w.warehouseId}>{code("KHO", w.warehouseId)} · {w.name}</option>)}</select></label>
          <label>Nhà cung cấp <span className="text-danger">*</span><select className="form-select" required {...register("supplierId", { required: true })}><option value="">Chọn nhà cung cấp</option>{existing && !options.data?.suppliers.some(s => s.supplierId === existing.supplierId) && <option value={existing.supplierId}>{code("NCC", existing.supplierId)}</option>}{options.data?.suppliers.map(s => <option key={s.supplierId} value={s.supplierId}>{s.name}</option>)}</select></label>
          <label>Ghi chú<textarea className="form-control" maxLength={500} {...register("notes")} /></label>
        </div>
        <div className="table-responsive mt-3"><table className="table purchase-form-table"><thead><tr><th>Sản phẩm <span className="text-danger">*</span></th><th>Số lượng <span className="text-danger">*</span></th><th>Giá nhập <span className="text-danger">*</span></th><th>Thành tiền</th><th /></tr></thead><tbody>{fields.map((field, index) => {
          const product = options.data?.products.find(p => p.productId === items[index]?.productId);
          return <tr key={field.id}>
            <td className="purchase-product-cell">
              <div className="purchase-product-fields">
                <label className="d-grid gap-1">Mã sản phẩm
                  <select className="form-select" required {...register(`items.${index}.productId`, { valueAsNumber: true })}>
                    <option value="">Chọn mã sản phẩm</option>
                    {field.productId && !options.data?.products.some(p => p.productId === field.productId) && <option value={field.productId}>{code("SP", field.productId)}</option>}
                    {options.data?.products.map(p => <option key={p.productId} value={p.productId}>{code("SP", p.productId)}</option>)}
                  </select>
                </label>
                <label className="d-grid gap-1">Tên sản phẩm
                  <input className="form-control" readOnly value={product?.name || ""} placeholder="Tự hiển thị theo mã" />
                </label>
                {product?.imageUrl && <div className="order-product-preview purchase-product-image"><img src={product.imageUrl} alt={product.name} /></div>}
              </div>
            </td>
            <td className="purchase-value-cell"><input aria-label={`Số lượng dòng ${index + 1}`} className="form-control" type="number" min={1} max={2147483647} step={1} required {...register(`items.${index}.quantity`, { valueAsNumber: true })} /></td>
            <td className="purchase-value-cell"><input aria-label={`Giá nhập dòng ${index + 1}`} className="form-control" type="number" min={0} step="0.01" required {...register(`items.${index}.unitPrice`, { valueAsNumber: true })} /></td>
            <td className="purchase-value-cell purchase-line-total">{money((items[index]?.quantity || 0) * (items[index]?.unitPrice || 0))}</td>
            <td className="purchase-value-cell">{!view && <button type="button" className="btn btn-outline-danger" aria-label="Xóa dòng" onClick={() => remove(index)}>×</button>}</td>
          </tr>;
        })}</tbody></table></div>
        {!view && <button type="button" className="btn btn-light border" onClick={() => append({ productId: 0, quantity: 1, unitPrice: 0 })}>Thêm dòng sản phẩm</button>}
        <p className="text-end fs-5">Tổng tiền: {money(items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0))}</p>
      </fieldset>
      {!view && <><p className="text-muted">Lưu nháp chưa tăng tồn kho. Sau khi kiểm tra hàng nhận, chọn Xác nhận nhập kho trên danh sách.</p><button className="btn btn-brand" disabled={isSubmitting || recovering || !!recoverId || options.isPending || options.isError}>{isSubmitting ? "Đang lưu..." : "Lưu phiếu nháp"}</button></>}
    </form>
  </Modal>;
}
