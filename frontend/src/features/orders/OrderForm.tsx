import { useState } from "react";
import { useFieldArray, useForm, useWatch } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { allPages, api } from "../../lib/api";
import type {
  Customer,
  Order,
  OrderItem,
  Product,
  Warehouse,
} from "../../types";
import { Modal } from "../../components/Modal";
import { localDate, money } from "../../lib/format";
import { lineTotal, PartialOrderError, saveOrder } from "./service";
interface Fields {
  orderNumber: string;
  warehouseId: number;
  customerId: string;
  orderDate: string;
  shippingAddress: string;
  notes: string;
  items: OrderItem[];
}
export function OrderForm({
  entity,
  viewOnly,
  onClose,
  onSaved,
}: {
  entity: Order | null;
  viewOnly: boolean;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [existing, setExisting] = useState(entity);
  const [error, setError] = useState("");
  const [recoverId, setRecoverId] = useState<number | null>(null);
  const [recovering, setRecovering] = useState(false);
  const options = useQuery({
    queryKey: ["order-options"],
    queryFn: async () => {
      const [customers, warehouses, products] = await Promise.all([
        allPages<Customer>("/api/customers?isActive=true"),
        allPages<Warehouse>("/api/warehouses?isActive=true"),
        allPages<Product>(
          "/api/products?isActive=true&sortBy=Name&sortDirection=Asc",
        ),
      ]);
      return { customers, warehouses, products };
    },
  });
  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    formState: { isSubmitting, errors },
  } = useForm<Fields>({ defaultValues: defaults(entity) });
  const { fields, append, remove } = useFieldArray({ control, name: "items" });
  const items = useWatch({ control, name: "items" }) || [];
  const reload = async (id: number) => {
    setRecovering(true);
    try {
      const saved = await api<Order>(`/api/sales/${id}`);
      setExisting(saved);
      reset(defaults(saved));
      setRecoverId(null);
      setError(
        "Đã tải lại phần dữ liệu được lưu. Kiểm tra và bổ sung các dòng còn thiếu trước khi lưu tiếp.",
      );
    } catch (e) {
      setError(`Không tải được đơn đã lưu. ${(e as Error).message}`);
    } finally {
      setRecovering(false);
    }
  };
  return (
    <Modal
      title={
        viewOnly
          ? "Chi tiết đơn hàng"
          : existing
            ? "Sửa đơn hàng"
            : "Thêm đơn hàng"
      }
      onClose={onClose}
      wide
      busy={isSubmitting || recovering}
    >
      <form
        onSubmit={handleSubmit(async (v) => {
          setError("");
          try {
            await saveOrder(
              existing,
              {
                orderNumber: v.orderNumber.trim(),
                warehouseId: Number(v.warehouseId),
                customerId: Number(v.customerId) || null,
                orderDate: `${v.orderDate}T00:00:00`,
                shippingAddress: v.shippingAddress.trim() || null,
                notes: v.notes.trim() || null,
              },
              v.items,
            );
            onSaved();
          } catch (e) {
            if (e instanceof PartialOrderError) {
              setRecoverId(e.orderId);
              setError(e.message);
              await reload(e.orderId);
            } else setError((e as Error).message);
          }
        })}
      >
        {Object.keys(errors).length > 0 && (
          <div className="alert alert-danger" role="alert">
            Vui lòng kiểm tra các trường bắt buộc.
          </div>
        )}
        {error && (
          <div className="alert alert-warning" role="alert">
            {error}
            {recoverId && (
              <button
                type="button"
                disabled={recovering}
                onClick={() => void reload(recoverId)}
              >
                Tải lại đơn đã lưu
              </button>
            )}
          </div>
        )}
        {options.isError && (
          <div className="alert alert-danger">
            Không tải được dữ liệu lựa chọn.{" "}
            <button type="button" onClick={() => void options.refetch()}>
              Thử lại
            </button>
          </div>
        )}
        <fieldset
          disabled={viewOnly || isSubmitting || recovering || !!recoverId}
        >
          <div className="form-grid">
            <label>
              Mã hệ thống
              <input
                className="form-control"
                readOnly
                value={
                  existing
                    ? `DH-${String(existing.salesOrderId).padStart(4, "0")}`
                    : "Tự động khi lưu"
                }
              />
            </label>
            <label>
              Mã đơn hàng *
              <input
                className="form-control"
                required
                maxLength={50}
                {...register("orderNumber", { required: true })}
              />
            </label>
            <label>
              Ngày đặt *
              <input
                className="form-control"
                type="date"
                required
                {...register("orderDate", { required: true })}
              />
            </label>
            <label>
              Kho *
              <select
                className="form-select"
                required
                aria-label="Kho *"
                {...register("warehouseId", { valueAsNumber: true })}
              >
                <option value="">Chọn kho</option>
                {existing &&
                  !options.data?.warehouses.some(
                    (w) => w.warehouseId === existing.warehouseId,
                  ) && (
                    <option value={existing.warehouseId}>
                      Kho #{existing.warehouseId}
                    </option>
                  )}
                {options.data?.warehouses.map((w) => (
                  <option key={w.warehouseId} value={w.warehouseId}>
                    {w.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Khách hàng
              <select
                className="form-select"
                aria-label="Khách hàng"
                {...register("customerId", {
                  onChange: (event) => {
                    const customer = options.data?.customers.find(
                      (c) => c.customerId === Number(event.target.value),
                    );
                    if (customer?.address)
                      setValue("shippingAddress", customer.address);
                  },
                })}
              >
                <option value="">Khách lẻ</option>
                {existing?.customerId &&
                  !options.data?.customers.some(
                    (c) => c.customerId === existing.customerId,
                  ) && (
                    <option value={existing.customerId}>
                      {existing.customerName || `Khách #${existing.customerId}`}
                    </option>
                  )}
                {options.data?.customers.map((c) => (
                  <option key={c.customerId} value={c.customerId}>
                    {c.fullName} {c.phoneNumber}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Địa chỉ giao hàng
              <input
                className="form-control"
                maxLength={255}
                {...register("shippingAddress")}
              />
            </label>
            <label>
              Ghi chú
              <textarea
                className="form-control"
                maxLength={500}
                {...register("notes")}
              />
            </label>
          </div>
          <div className="table-responsive mt-3">
            <table className="table">
              <thead>
                <tr>
                  <th>Sản phẩm</th>
                  <th>Số lượng</th>
                  <th>Đơn giá</th>
                  <th>Giảm giá</th>
                  <th>Thành tiền</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {fields.map((field, index) => (
                  <tr key={field.id}>
                    <td>
                      <select
                        className="form-select"
                        required
                        {...register(`items.${index}.productId`, {
                          valueAsNumber: true,
                          onChange: (e) => {
                            const product = options.data?.products.find(
                              (p) => p.productId === Number(e.target.value),
                            );
                            if (product)
                              setValue(
                                `items.${index}.unitPrice`,
                                product.salePrice,
                              );
                          },
                        })}
                      >
                        <option value="">Chọn sản phẩm</option>
                        {field.productId &&
                          !options.data?.products.some(
                            (p) => p.productId === field.productId,
                          ) && (
                            <option value={field.productId}>
                              Sản phẩm #{field.productId}
                            </option>
                          )}
                        {options.data?.products.map((p) => (
                          <option key={p.productId} value={p.productId}>
                            {p.sku} · {p.name}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td>
                      <input
                        className="form-control"
                        style={{ minWidth: 90 }}
                        type="number"
                        min="1"
                        max="2147483647"
                        step="1"
                        required
                        {...register(`items.${index}.quantity`, {
                          valueAsNumber: true,
                        })}
                      />
                    </td>
                    <td>
                      <input
                        className="form-control"
                        style={{ minWidth: 120 }}
                        type="number"
                        min="0"
                        step="0.01"
                        required
                        {...register(`items.${index}.unitPrice`, {
                          valueAsNumber: true,
                        })}
                      />
                    </td>
                    <td>
                      <input
                        className="form-control"
                        style={{ minWidth: 100 }}
                        type="number"
                        min="0"
                        step="0.01"
                        required
                        {...register(`items.${index}.discount`, {
                          valueAsNumber: true,
                        })}
                      />
                    </td>
                    <td>{money(lineTotal(items[index] || field))}</td>
                    <td>
                      {!viewOnly && (
                        <button
                          type="button"
                          className="btn btn-outline-danger"
                          aria-label="Xóa dòng"
                          onClick={() => remove(index)}
                        >
                          ×
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {!viewOnly && (
            <button
              type="button"
              className="btn btn-light border"
              onClick={() =>
                append({ productId: 0, quantity: 1, unitPrice: 0, discount: 0 })
              }
            >
              Thêm dòng sản phẩm
            </button>
          )}
          <p className="text-end fs-5 mt-3">
            Tổng tiền:{" "}
            {money(items.reduce((sum, item) => sum + lineTotal(item), 0))}
          </p>
        </fieldset>
        {!viewOnly && (
          <button
            className="btn btn-brand"
            disabled={
              isSubmitting ||
              recovering ||
              !!recoverId ||
              options.isPending ||
              options.isError
            }
          >
            {isSubmitting ? "Đang lưu..." : "Lưu đơn hàng"}
          </button>
        )}
      </form>
    </Modal>
  );
}
function defaults(order: Order | null): Fields {
  return {
    orderNumber: order?.orderNumber || `DH-${Date.now()}`,
    warehouseId: order?.warehouseId || 0,
    customerId: String(order?.customerId || ""),
    orderDate: order?.orderDate.slice(0, 10) || localDate(),
    shippingAddress: order?.shippingAddress || "",
    notes: order?.notes || "",
    items: order?.items || [
      { productId: 0, quantity: 1, unitPrice: 0, discount: 0 },
    ],
  };
}
