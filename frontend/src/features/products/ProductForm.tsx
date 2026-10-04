import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { api, ApiError, send } from "../../lib/api";
import type { Category, Page, Product } from "../../types";
import { code } from "../../lib/format";
import { Modal } from "../../components/Modal";
import { validateImage } from "./image";
interface Fields {
  sku: string;
  name: string;
  categoryId: number;
  unit: string;
  description: string;
  salePrice: number;
  minimumStockLevel: number;
  isActive: string;
}
export function ProductForm({
  entity,
  onClose,
  onSaved,
}: {
  entity: Product | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { data: categories = [] } = useQuery({
    queryKey: ["categories"],
    queryFn: () => api<Category[]>("/api/categories"),
  });
  const latest = useQuery({
    queryKey: ["product-code-preview"],
    queryFn: () => api<Page<Product>>("/api/products?page=1&pageSize=1&sortBy=CreatedAt&sortDirection=Desc"),
    enabled: !entity,
    staleTime: 0,
  });
  const units = Array.from(new Set(["Cái", "Chiếc", "Bộ", "Hộp", "Máy", ...(entity?.unit ? [entity.unit] : [])]));
  const { data: stock } = useQuery({
    queryKey: ["stock", entity?.productId],
    queryFn: () =>
      api<{ quantityOnHand: number }>(
        `/api/products/${entity!.productId}/stock`,
      ),
    enabled: !!entity,
  });
  const {
    register,
    handleSubmit,
    formState: { isSubmitting, errors },
  } = useForm<Fields>({
    defaultValues: {
      sku: entity?.sku || "",
      name: entity?.name || "",
      categoryId: entity?.categoryId,
      unit: entity?.unit || "",
      description: entity?.description || "",
      salePrice: entity?.salePrice || 0,
      minimumStockLevel: entity?.minimumStockLevel || 0,
      isActive: String(entity?.isActive ?? true),
    },
  });
  const [error, setError] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState("");
  const [imageUrl, setImageUrl] = useState(entity?.imageUrl || null);
  const [conflict, setConflict] = useState(false);
  useEffect(() => {
    if (!file) {
      setPreview("");
      return;
    }
    const url = URL.createObjectURL(file);
    setPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);
  const leafCategories = categories.filter(
    (c) => !categories.some((child) => child.parentCategoryId === c.categoryId),
  );
  return (
    <Modal
      title={entity ? "Sửa sản phẩm" : "Thêm sản phẩm"}
      onClose={onClose}
      busy={isSubmitting}
      wide
    >
      <form
        onSubmit={handleSubmit(async (values) => {
          setError("");
          if (conflict) return;
          try {
            let image = imageUrl;
            if (file) {
              const message = validateImage(file);
              if (message) throw new Error(message);
              const body = new FormData();
              body.append("image", file);
              const uploaded = await api<{ imageUrl: string }>(
                "/api/products/images",
                { method: "POST", body },
              );
              image = uploaded.imageUrl;
              setImageUrl(image);
              setFile(null);
            }
            await send(
              `/api/products${entity ? `/${entity.productId}` : ""}`,
              entity ? "PUT" : "POST",
              {
                ...values,
                sku: values.sku.trim(),
                name: values.name.trim(),
                unit: values.unit.trim(),
                categoryId: Number(values.categoryId),
                salePrice: Number(values.salePrice),
                minimumStockLevel: entity ? Number(values.minimumStockLevel) : 0,
                isActive: values.isActive === "true",
                description: values.description.trim() || null,
                imageUrl: image,
                ...(entity ? { rowVersion: entity.rowVersion } : {}),
              },
            );
            onSaved();
          } catch (e) {
            if (e instanceof ApiError && e.status === 409 && !/SKU already exists/i.test(e.message)) setConflict(true);
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
            {conflict && (
              <p>
                Dữ liệu có thể đã thay đổi. Đóng form và tải lại sản phẩm trước
                khi sửa tiếp.
              </p>
            )}
          </div>
        )}
        <div className="form-grid">
          <label>
            Mã hệ thống
            <input
              className="form-control"
              readOnly
              aria-label="Mã hệ thống"
              value={
                entity
                  ? code("D", entity.productId)
                  : latest.data ? code("D", (latest.data.items[0]?.productId || 0) + 1) : latest.isError ? "Chưa tải được mã dự kiến" : "Đang tải mã..."
              }
            />
          </label>
          <label>
            <span>Mã sản phẩm (SKU) <span className="text-danger">*</span></span>
            <input
              className="form-control"
              required
              maxLength={50}
              pattern="[\x20-\x7E]+"
              {...register("sku", { required: true })}
            />
          </label>
          <label>
            <span>Tên sản phẩm <span className="text-danger">*</span></span>
            <input
              className="form-control"
              required
              maxLength={200}
              {...register("name", {
                required: true,
                validate: (v) => !!v.trim(),
              })}
            />
          </label>
          <label>
            <span>Danh mục <span className="text-danger">*</span></span>
            <select
              className="form-select"
              aria-label="Danh mục *"
              required
              {...register("categoryId", {
                required: true,
                valueAsNumber: true,
              })}
            >
              <option value="">Chọn danh mục</option>
              {leafCategories.map((c) => (
                <option key={c.categoryId} value={c.categoryId}>
                  {c.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>Đơn vị <span className="text-danger">*</span></span>
            <select
              className="form-select"
              aria-label="Đơn vị *"
              required
              {...register("unit", { required: true })}
            >
              <option value="">Chọn đơn vị</option>
              {units.map(unit => <option key={unit} value={unit}>{unit}</option>)}
            </select>
          </label>
          <label>
            <span>Giá bán <span className="text-danger">*</span></span>
            <input
              className="form-control"
              type="number"
              min="0"
              max="9999999999999999.99"
              step="0.01"
              required
              {...register("salePrice", { valueAsNumber: true })}
            />
          </label>
          <label>
            Tồn kho tối thiểu
            <input
              className="form-control"
              type="number"
              readOnly={!entity}
              min="0"
              max="2147483647"
              step="1"
              required
              {...register("minimumStockLevel", { valueAsNumber: true })}
            />
          </label>
          <label>
            Mô tả
            <textarea
              className="form-control"
              maxLength={1000}
              {...register("description")}
            />
          </label>
          <label>
            Ảnh từ thiết bị
            <input
              className="form-control"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={(e) => {
                const next = e.target.files?.[0];
                setError("");
                if (next) {
                  const message = validateImage(next);
                  if (message) {
                    setError(message);
                    e.target.value = "";
                    setFile(null);
                  } else setFile(next);
                } else setFile(null);
              }}
            />
            {(preview || imageUrl) && (
              <img
                className="product-preview"
                src={preview || imageUrl!}
                alt="Xem trước sản phẩm"
              />
            )}
            <small>JPEG/PNG/WebP, tối đa 5 MB</small>
          </label>
        </div>
        {entity && (
          <p>Tồn kho hiện tại: {stock?.quantityOnHand ?? "Đang tải..."}</p>
        )}
        <label>
          <span>Trạng thái <span className="text-danger">*</span></span>
          <select className="form-select" aria-label="Trạng thái *" required {...register("isActive", { required: true })}>
            <option value="true">Đang kinh doanh</option>
            <option value="false">Ngừng kinh doanh</option>
          </select>
        </label>
        <button className="btn btn-brand" disabled={isSubmitting || conflict}>
          {isSubmitting ? "Đang lưu..." : "Lưu sản phẩm"}
        </button>
      </form>
    </Modal>
  );
}
