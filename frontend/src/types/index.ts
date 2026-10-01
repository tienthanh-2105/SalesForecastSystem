export type Role = "Admin" | "WarehouseManager" | "SalesStaff";
export interface User {
  userId: number | string;
  email: string;
  fullName: string;
  role: Role;
}
export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}
export interface Session {
  accessToken: string;
  expiresAt: number;
  user: User;
  rememberMe: boolean;
  lastActivityAt: number;
  lastRefreshAt: number;
}
export interface Page<T> {
  items: T[];
  totalItems: number;
  totalPages: number;
}
export interface Category {
  categoryId: number;
  code: string;
  name: string;
  description: string | null;
  parentCategoryId: number | null;
  isActive: boolean;
}
export interface Warehouse {
  warehouseId: number;
  name: string;
  address: string | null;
  isActive: boolean;
}
export interface Product {
  productId: number;
  sku: string;
  name: string;
  unit: string;
  categoryId: number;
  categoryName?: string;
  description: string | null;
  imageUrl: string | null;
  salePrice: number;
  minimumStockLevel: number;
  isActive: boolean;
  rowVersion: string;
  quantityOnHand?: number;
  stockStatus?: string;
}
export interface Customer {
  customerId: number;
  fullName: string;
  phoneNumber?: string;
  address?: string;
}
export type OrderStatus =
  "Draft" | "Pending" | "Delivering" | "Completed" | "Cancelled";
export interface OrderItem {
  salesOrderItemId?: number;
  productId: number;
  quantity: number;
  unitPrice: number;
  discount: number;
}
export interface Order {
  salesOrderId: number;
  orderNumber: string;
  warehouseId: number;
  customerId: number | null;
  orderDate: string;
  status: OrderStatus;
  notes: string | null;
  customerName?: string;
  customerPhone?: string;
  shippingAddress: string | null;
  totalAmount: number;
  items: OrderItem[];
}
export interface UserRow {
  userId: number;
  email: string;
  fullName: string;
  roleName: Role;
  status: string;
  phoneNumber?: string;
  createdAt: string;
}
export type Section =
  "categories" | "products" | "warehouses" | "orders" | "users";
