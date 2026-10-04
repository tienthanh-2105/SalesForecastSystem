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
  purchases: {
    label: "Nhập hàng", title: "QUẢN LÝ NHẬP HÀNG", icon: "box-arrow-in-down", heading: "Phiếu nhập hàng",
  },
  orders: {
    label: "Đơn hàng",
    title: "QUẢN LÝ ĐƠN HÀNG",
    icon: "receipt",
    heading: "Danh sách đơn hàng",
  },
  customers: {
    label: "Khách hàng",
    title: "QUẢN LÝ LỊCH SỬ ĐƠN HÀNG CỦA KHÁCH HÀNG",
    icon: "person-lines-fill",
    heading: "Lịch sử đơn hàng của khách hàng",
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
  (role === "WarehouseManager" && ["warehouses", "purchases"].includes(section)) ||
  (role === "SalesStaff" && ["orders", "customers"].includes(section));
export const home = (role: Role) =>
  role === "Admin"
    ? "/categories"
    : role === "WarehouseManager"
      ? "/warehouses"
      : "/orders";
