import { useState } from "react";
import { useForm } from "react-hook-form";
import { Modal } from "../../components/Modal";
import { send } from "../../lib/api";
import type { Customer } from "../../types";

export function CustomerForm({ entity, onClose, onSaved }: {
  entity: Customer | null; onClose: () => void; onSaved: () => void;
}) {
  const [error, setError] = useState("");
  const { register, handleSubmit, formState: { isSubmitting, errors } } = useForm({
    defaultValues: { fullName: entity?.fullName || "", phoneNumber: entity?.phoneNumber || "", email: entity?.email || "", address: entity?.address || "" },
  });
  return <Modal title={entity ? "Sửa khách hàng" : "Thêm khách hàng"} onClose={onClose} busy={isSubmitting}>
    <form onSubmit={handleSubmit(async values => {
      setError("");
      try {
        await send(`/api/customers${entity ? `/${entity.customerId}` : ""}`, entity ? "PUT" : "POST", {
          fullName: values.fullName.trim(), phoneNumber: values.phoneNumber.trim() || null,
          email: values.email.trim() || null, address: values.address.trim() || null,
          // Preserve the existing backend flag; customer status is not a UI field.
          isActive: entity?.isActive ?? true,
        });
        onSaved();
      } catch (e) { setError((e as Error).message); }
    })}>
      {error && <div role="alert" className="alert alert-danger">{error}</div>}
      {Object.keys(errors).length > 0 && <div role="alert" className="alert alert-danger">Vui lòng kiểm tra họ tên và số điện thoại.</div>}
      <label><span>Họ tên <span className="text-danger">*</span></span><input className="form-control" required maxLength={100} {...register("fullName", { required: true, validate: v => !!v.trim() })} /></label>
      <label>Số điện thoại<input className="form-control" type="tel" maxLength={15} {...register("phoneNumber", { validate: v => !v.trim() || /^[0-9+(). -]+$/.test(v.trim()) })} /></label>
      <label>Email<input className="form-control" type="email" maxLength={100} {...register("email")} /></label>
      <label>Địa chỉ<textarea className="form-control" rows={3} maxLength={255} {...register("address")} /></label>
      <button className="btn btn-brand" disabled={isSubmitting}>{isSubmitting ? "Đang lưu..." : "Lưu khách hàng"}</button>
    </form>
  </Modal>;
}
