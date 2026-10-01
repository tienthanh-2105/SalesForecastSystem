import { useForm } from "react-hook-form";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api, send } from "../../lib/api";
import type { Category } from "../../types";
import { Modal } from "../../components/Modal";
interface Fields {
  name: string;
  description: string;
  parentCategoryId: string;
  isActive: string;
}
export function CategoryForm({
  entity,
  onClose,
  onSaved,
}: {
  entity: Category | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { data: categories = [] } = useQuery({
    queryKey: ["categories"],
    queryFn: () => api<Category[]>("/api/categories"),
  });
  const {
    register,
    handleSubmit,
    formState: { isSubmitting, errors },
  } = useForm<Fields>({
    defaultValues: {
      name: entity?.name || "",
      description: entity?.description || "",
      parentCategoryId: String(entity?.parentCategoryId || ""),
      isActive: String(entity?.isActive ?? true),
    },
  });
  const [error, setError] = useState("");
  const excluded = new Set<number>(entity ? [entity.categoryId] : []);
  let changed = true;
  while (changed) {
    changed = false;
    for (const c of categories)
      if (
        c.parentCategoryId &&
        excluded.has(c.parentCategoryId) &&
        !excluded.has(c.categoryId)
      ) {
        excluded.add(c.categoryId);
        changed = true;
      }
  }
  return (
    <Modal
      title={entity ? "Sửa danh mục" : "Thêm danh mục"}
      onClose={onClose}
      busy={isSubmitting}
    >
      <form
        onSubmit={handleSubmit(async (v) => {
          setError("");
          try {
            await send(
              `/api/categories${entity ? `/${entity.categoryId}` : ""}`,
              entity ? "PUT" : "POST",
              {
                name: v.name.trim(),
                description: v.description.trim() || null,
                parentCategoryId: Number(v.parentCategoryId) || null,
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
        <div className="form-grid">
          <label>
            Mã danh mục
            <input
              className="form-control"
              readOnly
              value={entity?.code || "Tự động khi lưu"}
            />
          </label>
          <label>
            Tên danh mục *
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
            Danh mục cha
            <select className="form-select" {...register("parentCategoryId")}>
              <option value="">Danh mục gốc</option>
              {categories
                .filter(
                  (c) => !c.parentCategoryId && !excluded.has(c.categoryId),
                )
                .map((c) => (
                  <option key={c.categoryId} value={c.categoryId}>
                    {c.name}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Mô tả
            <textarea
              className="form-control"
              maxLength={500}
              {...register("description")}
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
        </div>
        <button className="btn btn-brand" disabled={isSubmitting}>
          {isSubmitting ? "Đang lưu..." : "Lưu danh mục"}
        </button>
      </form>
    </Modal>
  );
}
