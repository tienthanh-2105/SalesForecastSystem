export const money = (value: number) =>
  value.toLocaleString("vi-VN", { style: "currency", currency: "VND" });
export const date = (value: string) =>
  new Date(value).toLocaleDateString("vi-VN");
export const code = (prefix: string, id: number) =>
  `${prefix}-${String(id).padStart(4, "0")}`;
export function localDate() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}
