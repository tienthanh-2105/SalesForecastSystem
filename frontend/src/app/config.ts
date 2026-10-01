import type { Role, Section } from "../types";
export const sections: Record<
  Section,
  { label: string; title: string; icon: string; heading: string }
> = {
  categories: {
    label: "Danh mục",
    title: "QUẢN LÝ DANH MỤC",
    icon: "tags",
    heading: "Danh mục sản phẩm",
  },
  products: {
    label: "Sản phẩm",
    title: "QUẢN LÝ SẢN PHẨM",
    icon: "box-seam",
    heading: "Danh sách sản phẩm",
  },
  warehouses: {
    label: "Kho hàng",
    title: "QUẢN LÝ KHO",
    icon: "buildings",
    heading: "Danh sách kho hàng",
  },
  orders: {
    label: "Đơn hàng",
    title: "QUẢN LÝ ĐƠN HÀNG",
    icon: "receipt",
    heading: "Danh sách đơn hàng",
  },
  users: {
    label: "Người dùng",
    title: "QUẢN LÝ NGƯỜI DÙNG",
    icon: "people",
    heading: "Tài khoản người dùng",
  },
};
export const roleLabels: Record<Role, string> = {
  Admin: "Quản trị viên",
  WarehouseManager: "Quản lý kho",
  SalesStaff: "Nhân viên bán hàng",
};
export const allowed = (role: Role, section: string) =>
  role === "Admin" ||
  (role === "WarehouseManager" && section === "warehouses") ||
  (role === "SalesStaff" && section === "orders");
export const home = (role: Role) =>
  role === "Admin"
    ? "/categories"
    : role === "WarehouseManager"
      ? "/warehouses"
      : "/orders";
