import { useForm } from "react-hook-form";
import { useState } from "react";
import { allPages, send } from "../../lib/api";
import { useQuery } from "@tanstack/react-query";
import { code } from "../../lib/format";
import type { Warehouse } from "../../types";
import { Modal } from "../../components/Modal";
export function WarehouseForm({
  entity,
  onClose,
  onSaved,
}: {
  entity: Warehouse | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const warehouses = useQuery({
    queryKey: ["warehouse-code-preview"],
    queryFn: () => allPages<Warehouse>("/api/warehouses"),
    enabled: !entity,
    staleTime: 0,
  });
  const nextId = (warehouses.data || []).reduce((max, item) => Math.max(max, item.warehouseId), 0) + 1;
  const {
    register,
    handleSubmit,
    formState: { isSubmitting, errors },
  } = useForm({
    defaultValues: {
      name: entity?.name || "",
      address: entity?.address || "",
      isActive: String(entity?.isActive ?? true),
    },
  });
  const [error, setError] = useState("");
  return (
    <Modal
      title={entity ? "Sửa kho" : "Thêm kho"}
      onClose={onClose}
      busy={isSubmitting}
    >
      <form
        onSubmit={handleSubmit(async (v) => {
          try {
            setError("");
            await send(
              `/api/warehouses${entity ? `/${entity.warehouseId}` : ""}`,
              entity ? "PUT" : "POST",
              {
                name: v.name.trim(),
                address: v.address.trim(),
                isActive: v.isActive === "true",
              },
            );
            onSaved();
          } catch (e) {
            setError((e as Error).message);
          }
        })}
      >
        {Object.keys(errors).length > 0 && (
          <div className="alert alert-danger" role="alert">
            Vui lòng kiểm tra các trường bắt buộc.
          </div>
        )}
        {error && (
          <div className="alert alert-danger" role="alert">
            {error}
          </div>
        )}
        <label>
          <span>Mã kho <span className="text-danger">*</span></span>
          <input className="form-control" aria-label="Mã kho *" readOnly value={entity ? code("KHO", entity.warehouseId) : warehouses.data ? code("KHO", nextId) : warehouses.isError ? "Chưa tải được mã" : "Đang tải mã..."}/>
        </label>
        <label>
          <span>Tên kho <span className="text-danger">*</span></span>
          <input
            className="form-control"
            required
            maxLength={100}
            {...register("name", {
              required: true,
              validate: (v) => !!v.trim(),
            })}
          />
        </label>
        <label>
          <span>Địa chỉ <span className="text-danger">*</span></span>
          <input
            className="form-control"
            required
            maxLength={255}
            {...register("address", { required: true, validate: v => !!v.trim() })}
          />
        </label>
        <label>
          <span>Trạng thái <span className="text-danger">*</span></span>
          <select
            aria-label="Trạng thái"
            className="form-select"
            required
            {...register("isActive", { required: true })}
          >
            <option value="true">Đang hoạt động</option>
            <option value="false">Ngừng hoạt động</option>
          </select>
        </label>
        <button className="btn btn-brand" disabled={isSubmitting}>
          {isSubmitting ? "Đang lưu..." : "Lưu kho"}
        </button>
      </form>
    </Modal>
  );
}
