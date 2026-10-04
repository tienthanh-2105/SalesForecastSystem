import { useEffect, useRef, useState } from "react";
import { useFieldArray, useForm, useWatch } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { allPages, api, send } from "../../lib/api";
import type {
  Customer,
  Order,
  OrderItem,
  Product,
  Warehouse,
} from "../../types";
import { Modal } from "../../components/Modal";
import { code, localDate, money } from "../../lib/format";
import { lineTotal, PartialOrderError, saveOrder, validateItems } from "./service";
interface Fields {
  orderNumber: string;
  warehouseId: number;
  customerId: string;
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  orderDate: string;
  shippingAddress: string;
  notes: string;
  items: (OrderItem & { discountPercent: number })[];
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
  const customerSaved = useRef<{ signature: string; id: number } | null>(null);
  const orderCodes = useQuery({
    queryKey: ["order-code-preview"],
    queryFn: () => allPages<Order>("/api/sales"),
    enabled: !existing,
    staleTime: 0,
  });
  const nextOrderId = (orderCodes.data || []).reduce((max, order) => Math.max(max, order.salesOrderId), 0) + 1;
  const options = useQuery({
    queryKey: ["order-options"],
    queryFn: async () => {
      const [customers, warehouses, products] = await Promise.all([
        allPages<Customer>("/api/customers"),
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
  const orderDate = useWatch({ control, name: "orderDate" });
  const warehouseId = useWatch({ control, name: "warehouseId" });
  const warehouseStock = useQuery({
    queryKey: ["order-stock", warehouseId, options.data?.products.map(p => p.productId)],
    enabled: !!warehouseId && !!options.data && !viewOnly,
    queryFn: async () => Object.fromEntries(await Promise.all((options.data?.products || []).map(async product => {
      const stock = await api<{ quantityOnHand: number }>(`/api/products/${product.productId}/stock?warehouseId=${warehouseId}`);
      return [product.productId, stock.quantityOnHand] as const;
    }))),
  });
  const customerPhone = useWatch({ control, name: "customerPhone" }) || "";
  const [codeYear, codeMonth, codeDay] = (orderDate || "").split("-");
  const customerDetail = useQuery({
    queryKey: ["order-customer", existing?.customerId],
    queryFn: () => api<Customer>(`/api/customers/${existing!.customerId}`),
    enabled: !!existing?.customerId,
  });
  useEffect(() => {
    if (customerDetail.data) {
      setValue("customerEmail", customerDetail.data.email || "");
    }
  }, [customerDetail.data, setValue]);
  useEffect(() => {
    if (existing) return;
    const [year, month, day] = (orderDate || "").split("-");
    const phone = customerPhone.replace(/[^0-9]/g, "");
    setValue("orderNumber", phone && day ? `DH-${day}${month}${year}${phone}` : "");
  }, [existing, orderDate, customerPhone, setValue]);
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
            if (v.items.some(item => !Number.isFinite(item.discountPercent) || item.discountPercent < 0 || item.discountPercent > 100)) throw new Error("Giảm giá phải từ 0 đến 100%.");
            const apiItems = v.items.map(item => ({ ...item, discount: percentDiscount(item) }));
            const itemError = validateItems(apiItems);
            if (itemError) throw new Error(itemError);
            // Recheck live stock before creating a customer or an order header.
            for (const item of apiItems) {
              const stock = await api<{ quantityOnHand: number }>(`/api/products/${item.productId}/stock?warehouseId=${v.warehouseId}`);
              if (!Number.isFinite(stock.quantityOnHand)) throw new Error("Không xác định được tồn kho. Vui lòng thử lại.");
              if (item.quantity > stock.quantityOnHand) {
                void warehouseStock.refetch();
                throw new Error(`${code("SP", item.productId)} chỉ còn ${stock.quantityOnHand} sản phẩm tại kho đã chọn; không thể lưu số lượng ${item.quantity}.`);
              }
            }
            const contact = {
              fullName: v.customerName.trim(), phoneNumber: v.customerPhone.trim() || null,
              email: v.customerEmail.trim() || null, address: v.shippingAddress.trim(),
            };
            let customerId: number | null = null;
            if (contact.fullName) {
              const signature = JSON.stringify(contact);
              const match = [...(options.data?.customers || []), ...(customerDetail.data ? [customerDetail.data] : [])].find(c =>
                c.fullName === contact.fullName && (c.phoneNumber || null) === contact.phoneNumber &&
                (c.email || null) === contact.email);
              if (customerSaved.current?.signature === signature) customerId = customerSaved.current.id;
              else if (match) customerId = match.customerId;
              else {
                const customer = await send<Customer>("/api/customers", "POST", { ...contact, isActive: true });
                customerId = customer.customerId;
                customerSaved.current = { signature, id: customerId };
              }
            } else if (contact.phoneNumber || contact.email) throw new Error("Vui lòng nhập tên khách hàng khi có số điện thoại hoặc email.");
            await saveOrder(
              existing,
              {
                orderNumber: v.orderNumber.trim(),
                warehouseId: Number(v.warehouseId),
                customerId,
                orderDate: `${v.orderDate}T00:00:00`,
                shippingAddress: v.shippingAddress.trim() || null,
                notes: v.notes.trim() || null,
              },
              apiItems,
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
        {customerDetail.isError && <div className="alert alert-danger" role="alert">Không tải được thông tin khách hàng. <button type="button" onClick={() => void customerDetail.refetch()}>Thử lại</button></div>}
        {warehouseStock.isError && <div className="alert alert-danger" role="alert">Không tải được tồn kho. <button type="button" onClick={() => void warehouseStock.refetch()}>Thử lại</button></div>}
        {!viewOnly && !!warehouseId && <button type="button" className="btn btn-light border" disabled={warehouseStock.isFetching || isSubmitting} onClick={() => void warehouseStock.refetch()}>Cập nhật tồn kho</button>}
        <fieldset
          disabled={viewOnly || isSubmitting || recovering || !!recoverId}
        >
          <div className="form-grid">
            <label>
              Mã hệ thống
              <input
                aria-label="Mã hệ thống"
                className="form-control"
                readOnly
                value={
                  existing
                    ? `DH-${String(existing.salesOrderId).padStart(4, "0")}`
                    : orderCodes.data ? `DH-${String(nextOrderId).padStart(4, "0")}` : orderCodes.isError ? "Không tải được mã dự kiến" : "Đang tải mã..."
                }
              />
            </label>
            <label>
              <span>Mã đơn hàng <span className="text-danger">*</span></span>
              <input
                aria-label="Mã đơn hàng"
                className="form-control"
                required
                readOnly
                placeholder={codeDay ? `DH-${codeDay}${codeMonth}${codeYear}…` : "Chọn ngày đặt và nhập số điện thoại"}
                maxLength={50}
                {...register("orderNumber", { required: true })}
              />
            </label>
            <label>
              <span>Ngày đặt <span className="text-danger">*</span></span>
              <input
                className="form-control"
                type="date"
                required
                {...register("orderDate", { required: true })}
              />
            </label>
            <label>
              <span>Kho <span className="text-danger">*</span></span>
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
              <input
                className="form-control"
                aria-label="Khách hàng"
                placeholder="Nhập tên khách hàng"
                maxLength={100}
                {...register("customerName")}
              />
            </label>
            <label>
              <span>Số điện thoại <span className="text-danger">*</span></span>
              <input className="form-control" type="tel" maxLength={15}
                required
                {...register("customerPhone", { required: true, validate: value => /[0-9]/.test(value) && /^[0-9+(). -]+$/.test(value.trim()) })} />
            </label>
            <label>
              Email
              <input className="form-control" type="email" maxLength={100} {...register("customerEmail")} />
            </label>
            <label>
              <span>Địa chỉ giao hàng <span className="text-danger">*</span></span>
              <input
                className="form-control"
                maxLength={255}
                required
                {...register("shippingAddress", { required: true, validate: value => !!value.trim() })}
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
            <table className="table order-entry-table">
              <thead>
                <tr>
                  <th>Sản phẩm <span className="text-danger">*</span></th>
                  <th>Số lượng <span className="text-danger">*</span></th>
                  <th>Đơn giá <span className="text-danger">*</span></th>
                  <th>Giảm giá (%)</th>
                  <th>Thành tiền</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {fields.map((field, index) => (
                  <tr key={field.id}>
                    <td>
                      <label className="d-grid gap-1 mb-2">
                        <span>Mã sản phẩm <span className="text-danger">*</span></span>
                      <select
                        className="form-select"
                        disabled={!viewOnly && (!warehouseId || warehouseStock.isPending || warehouseStock.isError)}
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
                        <option value="">Chọn mã sản phẩm</option>
                        {field.productId &&
                          !options.data?.products.some(
                            (p) => p.productId === field.productId,
                          ) && (
                            <option value={field.productId}>
                              {code("SP", field.productId)}
                            </option>
                          )}
                        {options.data?.products.map((p) => (
                          <option key={p.productId} value={p.productId} disabled={!viewOnly && (warehouseStock.data?.[p.productId] ?? 0) <= 0}>
                            {code("SP", p.productId)}{!viewOnly && (warehouseStock.data?.[p.productId] ?? 0) <= 0 ? " — Hết hàng tại kho" : ""}
                          </option>
                        ))}
                      </select>
                      </label>
                      {!viewOnly && !!items[index]?.productId && <small className="d-block mb-2 text-muted">Tồn kho đã chọn: {warehouseStock.data?.[items[index].productId] ?? "Đang tải..."}</small>}
                      <label className="d-grid gap-1 mb-2">
                        <span>Tên sản phẩm</span>
                        <input className="form-control" readOnly value={options.data?.products.find(p => p.productId === items[index]?.productId)?.name || ""} placeholder="Tên hiển thị khi chọn mã sản phẩm" />
                      </label>
                      <div className="order-product-preview">
                        {options.data?.products.find(p => p.productId === items[index]?.productId)?.imageUrl
                          ? <img src={options.data.products.find(p => p.productId === items[index]?.productId)!.imageUrl!} alt={options.data.products.find(p => p.productId === items[index]?.productId)!.name} />
                          : <i className="bi bi-image" aria-hidden="true" />}
                      </div>
                    </td>
                    <td>
                      <input
                        className="form-control"
                        style={{ minWidth: 90 }}
                        type="number"
                        min="1"
                        max={viewOnly ? 2147483647 : Math.max(0, warehouseStock.data?.[items[index]?.productId] ?? 0)}
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
                        max="100"
                        step="0.01"
                        required
                        aria-label={`Giảm giá (%) dòng ${index + 1}`}
                        {...register(`items.${index}.discountPercent`, {
                          valueAsNumber: true,
                          min: 0,
                          max: 100,
                        })}
                      />
                    </td>
                    <td>{money(percentTotal(items[index] || field))}</td>
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
                append({ productId: 0, quantity: 1, unitPrice: 0, discount: 0, discountPercent: 0 })
              }
            >
              Thêm dòng sản phẩm
            </button>
          )}
          <p className="text-end fs-5 mt-3">
            Tổng tiền:{" "}
            {money(items.reduce((sum, item) => sum + percentTotal(item), 0))}
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
              options.isError ||
              warehouseStock.isPending || warehouseStock.isError ||
              (!!existing?.customerId && (customerDetail.isPending || customerDetail.isError))
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
    orderNumber: order?.orderNumber || "",
    warehouseId: order?.warehouseId || 0,
    customerId: String(order?.customerId || ""),
    customerName: order?.customerName || "",
    customerPhone: order?.customerPhone || "",
    customerEmail: "",
    orderDate: order?.orderDate.slice(0, 10) || localDate(),
    shippingAddress: order?.shippingAddress || "",
    notes: order?.notes || "",
    items: order?.items.map(item => ({ ...item, discountPercent: item.quantity * item.unitPrice > 0 ? item.discount / (item.quantity * item.unitPrice) * 100 : 0 })) || [
      { productId: 0, quantity: 1, unitPrice: 0, discount: 0, discountPercent: 0 },
    ],
  };
}
function percentDiscount(item: OrderItem & { discountPercent: number }) {
  return Math.round(item.quantity * item.unitPrice * item.discountPercent) / 100;
}
function percentTotal(item: OrderItem & { discountPercent: number }) {
  return lineTotal({ ...item, discount: percentDiscount(item) });
}
