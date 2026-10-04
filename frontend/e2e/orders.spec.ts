import { test, expect } from "@playwright/test";
import type { Page } from "@playwright/test";
const user = {
  userId: 1,
  email: "sales@test.invalid",
  fullName: "Sales test",
  role: "SalesStaff",
};
const order = {
  salesOrderId: 10,
  orderNumber: "DH-TEST",
  warehouseId: 1,
  customerId: null,
  orderDate: "2026-10-01T00:00:00",
  status: "Draft",
  notes: null,
  shippingAddress: null,
  totalAmount: 100,
  items: [
    {
      salesOrderItemId: 1,
      productId: 1,
      quantity: 1,
      unitPrice: 100,
      discount: 0,
    },
  ],
};
async function login(page: Page) {
  await page.goto("/orders");
  await page.getByLabel("Email đăng nhập").fill(user.email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill("test-password");
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
}
test("5 trạng thái và các endpoint chuyển trạng thái", async ({ page }) => {
  const current = { ...order };
  const calls: string[] = [];
  await page.route("**/api/**", async (route) => {
    const url = new URL(route.request().url()),
      path = url.pathname;
    if (path === "/api/auth/login")
      return route.fulfill({
        json: {
          accessToken: "x",
          expiresAt: new Date(Date.now() + 3600000).toISOString(),
          user,
        },
      });
    if (path === "/api/auth/me") return route.fulfill({ json: user });
    if (route.request().method() === "POST") {
      calls.push(path);
      current.status = path.endsWith("/submit")
        ? "Pending"
        : path.endsWith("/dispatch")
          ? "Delivering"
          : path.endsWith("/complete")
            ? "Completed"
            : "Cancelled";
      return route.fulfill({ json: current });
    }
    await route.fulfill({
      json:
        path === "/api/sales/10"
          ? current
          : { items: [current], totalItems: 1, totalPages: 1 },
    });
  });
  await login(page);
  await expect(
    page.getByLabel("Tất cả trạng thái").locator("option"),
  ).toHaveCount(6);
  await expect(page.getByRole("button", { name: "Sửa", exact: true })).toBeEnabled();
  await expect(page.getByRole("button", { name: "Xóa", exact: true })).toBeEnabled();
  for (const label of ["Xác nhận đơn", "Bắt đầu giao", "Xác nhận đã giao"]) {
    await page.getByRole("button", { name: label, exact: true }).click();
    await page.getByRole("button", { name: "Xác nhận", exact: true }).click();
    await expect(page.getByRole("dialog")).toHaveCount(0);
  }
  await expect(
    page.getByRole("button", { name: "Sửa", exact: true }),
  ).toBeDisabled();
  await expect(page.getByRole("button", { name: "Xóa", exact: true })).toBeDisabled();
  expect(calls).toEqual([
    "/api/sales/10/submit",
    "/api/sales/10/dispatch",
    "/api/sales/10/complete",
  ]);
  current.status = "Draft";
  await page.getByRole("button", { name: "Làm mới" }).click();
  await page.getByRole("button", { name: "Hủy đơn", exact: true }).click();
  await page.getByRole("button", { name: "Xác nhận", exact: true }).click();
  await expect(
    page.getByRole("cell", { name: "Đã hủy", exact: true }),
  ).toBeVisible();
});
test("lưu đơn một phần tải lại theo ID, không tạo trùng", async ({ page }) => {
  let created = 0,
    failItem = true;
  let stockQuantity = 10;
  const current = {
    ...order,
    customerName: "",
    customerPhone: "",
    salesOrderId: 42,
    items: [] as typeof order.items,
  };
  await page.route("**/api/**", async (route) => {
    const request = route.request(),
      path = new URL(request.url()).pathname;
    if (path === "/api/auth/login")
      return route.fulfill({
        json: {
          accessToken: "x",
          expiresAt: new Date(Date.now() + 3600000).toISOString(),
          user,
        },
      });
    if (path === "/api/auth/me") return route.fulfill({ json: user });
    if (path === "/api/products/1/stock") return route.fulfill({ json: { productId: 1, quantityOnHand: stockQuantity } });
    if (path === "/api/customers" && request.method() === "POST") {
      const contact = request.postDataJSON();
      current.customerName = contact.fullName;
      current.customerPhone = contact.phoneNumber;
      return route.fulfill({ json: { ...contact, customerId: 1 } });
    }
    if (path === "/api/customers/1") return route.fulfill({ json: { customerId: 1, fullName: "Khách kiểm thử", phoneNumber: "0901234567", email: null } });
    if (path === "/api/sales" && request.method() === "POST") {
      created++;
      Object.assign(current, request.postDataJSON());
      return route.fulfill({ json: current });
    }
    if (path === "/api/sales/42" && request.method() === "PUT")
      return route.fulfill({ json: current });
    if (path === "/api/sales/42/items" && request.method() === "POST") {
      if (failItem) {
        failItem = false;
        return route.fulfill({
          status: 400,
          json: { message: "Lỗi dòng kiểm thử" },
        });
      }
      current.items = [{ ...request.postDataJSON(), salesOrderItemId: 5 }];
      return route.fulfill({ json: current });
    }
    if (path === "/api/sales/42") return route.fulfill({ json: current });
    const items =
      path === "/api/warehouses"
        ? [{ warehouseId: 1, name: "Kho test", isActive: true }]
        : path === "/api/products"
          ? [{ productId: 1, sku: "TEST", name: "Hàng test", salePrice: 100 }]
          : [];
    return route.fulfill({
      json: { items, totalItems: items.length, totalPages: 1 },
    });
  });
  await login(page);
  await page.getByRole("button", { name: "Thêm đơn hàng" }).click();
  await page.getByLabel("Ngày đặt").fill("2026-10-02");
  await page.getByRole("dialog").getByLabel("Khách hàng", { exact: true }).fill("Khách kiểm thử");
  await page.getByLabel("Số điện thoại").fill("0901234567");
  await page.getByLabel("Địa chỉ giao hàng").fill("Địa chỉ kiểm thử");
  await page.getByLabel("Kho *", { exact: true }).selectOption("1");
  await page.getByRole("dialog").locator("tbody select").selectOption("1");
  await expect(page.getByLabel("Mã đơn hàng")).toHaveValue("DH-021020260901234567");
  await page.getByLabel("Giảm giá (%) dòng 1", { exact: true }).fill("10");
  await expect(page.getByRole("dialog").locator("tbody tr td").nth(4)).toHaveText(/90/);
  await expect(page.getByLabel("Mã đơn hàng")).toHaveAttribute("readonly", "");
  stockQuantity = 0;
  await page.getByRole("button", { name: "Lưu đơn hàng" }).click();
  await expect(page.getByRole("alert").filter({ hasText: "chỉ còn 0" })).toBeVisible();
  expect(created).toBe(0);
  stockQuantity = 10;
  await page.getByRole("button", { name: "Cập nhật tồn kho" }).click();
  await expect(page.getByText("Tồn kho đã chọn: 10")).toBeVisible();
  await page.getByRole("button", { name: "Lưu đơn hàng" }).click();
  await expect(
    page.getByText("Đã tải lại phần dữ liệu được lưu.", { exact: false }),
  ).toBeVisible();
  expect(created).toBe(1);
  await page.getByRole("button", { name: "Thêm dòng sản phẩm" }).click();
  await page.getByRole("dialog").locator("tbody select").selectOption("1");
  await page.getByRole("button", { name: "Lưu đơn hàng" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  expect(created).toBe(1);
});
