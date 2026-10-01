import { useForm } from "react-hook-form";
import { useState } from "react";
import { send } from "../../lib/api";
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
                address: v.address.trim() || null,
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
          Tên kho *
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
          Địa chỉ
          <input
            className="form-control"
            maxLength={255}
            {...register("address")}
          />
        </label>
        <label>
          Trạng thái
          <select
            aria-label="Trạng thái"
            className="form-select"
            {...register("isActive")}
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
