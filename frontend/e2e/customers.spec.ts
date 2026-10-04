import { test, expect } from "@playwright/test";

test("khách hàng: không cho thêm, sửa không trạng thái, tìm kiếm và lịch sử", async ({ page }) => {
  const user = { userId: 1, email: "sales@test.invalid", fullName: "Sales test", role: "SalesStaff" };
  let customer = { customerId: 1, fullName: "Khách mẫu", email: null, phoneNumber: null, address: "Địa chỉ mẫu", isActive: false };
  let saved: Record<string, unknown> = {};
  await page.route("**/api/**", async route => {
    const request = route.request(), url = new URL(request.url());
    if (url.pathname === "/api/auth/login") return route.fulfill({ json: { accessToken: "test", expiresAt: new Date(Date.now() + 3600000).toISOString(), user } });
    if (url.pathname === "/api/auth/me") return route.fulfill({ json: user });
    if (url.pathname === "/api/customers/1/orders") return route.fulfill({ json: { items: [{ salesOrderId: 1, orderNumber: "DH-MAU", orderDate: "2026-10-01", status: "Completed" }], totalItems: 11, totalPages: 2 } });
    if (["POST", "PUT"].includes(request.method())) {
      saved = request.postDataJSON();
      customer = { ...customer, ...saved };
      return route.fulfill({ json: customer });
    }
    if (url.pathname === "/api/customers/1") return route.fulfill({ json: customer });
    return route.fulfill({ json: { items: url.pathname === "/api/customers" ? [customer] : [], totalItems: 1, totalPages: 1 } });
  });
  await page.goto("/customers");
  await page.getByLabel("Email đăng nhập").fill(user.email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill("test");
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await expect(page.getByRole("link", { name: "Khách hàng", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "Thêm khách hàng" })).toHaveCount(0);
  await expect(page.getByRole("columnheader", { name: "Trạng thái", exact: true })).toHaveCount(0);
  await expect(page.getByRole("combobox")).toHaveCount(0);
  await page.getByRole("button", { name: "Sửa", exact: true }).click();
  await expect(page.getByRole("dialog").getByRole("combobox")).toHaveCount(0);
  await page.getByLabel("Họ tên").fill("Khách sửa");
  await page.getByRole("button", { name: "Lưu khách hàng" }).click();
  await expect(page.getByRole("cell", { name: "Khách sửa", exact: true })).toBeVisible();
  expect(saved.isActive).toBe(false);
  expect(saved.email).toBeNull();
  await page.getByLabel("Tìm kiếm").fill("Khách sửa");
  await expect(page).toHaveURL(/search=/);
  await page.getByRole("button", { name: "Lịch sử đơn hàng" }).click();
  await expect(page.getByRole("cell", { name: "Đã giao", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Trang lịch sử sau" }).click();
  await expect(page.getByRole("dialog")).toContainText("Trang 2/2");
  await page.getByRole("link", { name: "DH-MAU" }).click();
  await expect(page).toHaveURL(/\/orders\?search=DH-MAU/);
});
