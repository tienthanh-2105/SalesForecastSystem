import { useState } from "react";
import { useForm } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { Modal } from "../../components/Modal";
import { roleLabels } from "../../app/config";
import { api, send } from "../../lib/api";
import { code } from "../../lib/format";
import type { Role, UserRow } from "../../types";

interface Fields {
  fullName: string;
  email: string;
  phoneNumber: string;
  role: Role | "";
  password: string;
  confirmPassword: string;
}

export function UserForm({ entity, onClose, onSaved }: {
  entity: UserRow | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [error, setError] = useState("");
  const [visible, setVisible] = useState(false);
  const nextCode = useQuery({ queryKey: ["user-next-code"], queryFn: () => api<{ code: string }>("/api/users/next-code"), enabled: !entity, staleTime: 0 });
  const { register, handleSubmit, getValues, formState: { isSubmitting, errors } } = useForm<Fields>({
    defaultValues: { fullName: entity?.fullName || "", email: entity?.email || "", phoneNumber: entity?.phoneNumber || "", role: entity?.roleName || "", password: "", confirmPassword: "" },
  });
  return <Modal title={entity ? "Sửa người dùng" : "Thêm người dùng"} onClose={onClose} busy={isSubmitting}>
    <form onSubmit={handleSubmit(async values => {
      setError("");
      try {
        await send(entity ? `/api/users/${entity.userId}` : "/api/users", entity ? "PUT" : "POST", {
          fullName: values.fullName.trim(), email: values.email.trim(),
          phoneNumber: values.phoneNumber.trim() || null, role: values.role,
          ...(entity ? {} : { password: values.password }),
        });
        onSaved();
      } catch (e) { setError((e as Error).message); }
    })}>
      {error && <div role="alert" className="alert alert-danger">{error}</div>}
      <div className="form-grid user-form-grid">
        <label className="user-code-field"><span>Mã người dùng</span><input aria-label="Mã người dùng" className="form-control" readOnly value={entity ? entity.code || code("ND", entity.userId) : nextCode.data?.code || (nextCode.isError ? "Không tải được mã" : "Đang cấp mã...")} /></label>
        <label><span>Họ tên <span className="text-danger">*</span></span>
          <input className="form-control" autoComplete="name" maxLength={100} required {...register("fullName", { required: "Vui lòng nhập họ tên.", validate: value => !!value.trim() || "Vui lòng nhập họ tên." })} />
          {errors.fullName && <small className="text-danger">{errors.fullName.message}</small>}
        </label>
        <label><span>Email <span className="text-danger">*</span></span>
          <input className="form-control" type="email" autoComplete="off" maxLength={100} required {...register("email", { required: "Vui lòng nhập email.", pattern: { value: /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/, message: "Email không đúng định dạng (ví dụ: ten@example.com)." } })} />
          {errors.email && <small className="text-danger">{errors.email.message}</small>}
        </label>
        <label>Số điện thoại
          <input className="form-control" type="tel" autoComplete="tel" maxLength={15} {...register("phoneNumber", { validate: value => !value.trim() || /^[0-9+(). -]+$/.test(value.trim()) || "Số điện thoại có ký tự không hợp lệ." })} />
          {errors.phoneNumber && <small className="text-danger">{errors.phoneNumber.message}</small>}
        </label>
        <label><span>Vai trò <span className="text-danger">*</span></span>
          <select className="form-select" required {...register("role", { required: "Vui lòng chọn vai trò." })}>
            <option value="">Chọn vai trò</option>
            {entity?.roleName === "Admin" ? <option value="Admin">Quản trị viên</option> : Object.entries(roleLabels).filter(([value]) => value !== "Admin").map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </label>
        {!entity && <><label><span>Mật khẩu <span className="text-danger">*</span></span>
          <input className="form-control" type={visible ? "text" : "password"} autoComplete="new-password" required minLength={12} {...register("password", {
            required: "Vui lòng nhập mật khẩu.", minLength: { value: 12, message: "Mật khẩu cần ít nhất 12 ký tự." },
            validate: {
              byteLength: value => new TextEncoder().encode(value).length <= 72 || "Mật khẩu quá dài; tối đa 72 byte UTF-8.",
              complexity: value => (/[A-Za-z]/.test(value) && /[0-9]/.test(value)) || "Mật khẩu phải có ít nhất một chữ cái (hoa hoặc thường) và một chữ số.",
            },
          })} />
          <small>Ít nhất 12 ký tự, có chữ cái (hoa hoặc thường) và chữ số.</small>
          {errors.password && <small className="text-danger">{errors.password.message}</small>}
        </label>
        <label><span>Xác nhận mật khẩu <span className="text-danger">*</span></span>
          <input className="form-control" type={visible ? "text" : "password"} autoComplete="new-password" required {...register("confirmPassword", { required: "Vui lòng xác nhận mật khẩu.", validate: value => value === getValues("password") || "Mật khẩu xác nhận chưa khớp." })} />
          {errors.confirmPassword && <small className="text-danger">{errors.confirmPassword.message}</small>}
        </label></>}
      </div>
      {!entity && <label className="d-flex align-items-center gap-2"><input className="form-check-input m-0" type="checkbox" checked={visible} onChange={event => setVisible(event.target.checked)} />Hiển thị mật khẩu</label>}
      <button className="btn btn-brand" disabled={isSubmitting}>{isSubmitting ? "Đang lưu..." : "Lưu người dùng"}</button>
    </form>
  </Modal>;
}
