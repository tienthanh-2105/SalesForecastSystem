import { test, expect } from "@playwright/test";
test("kho CRUD, phân trang, users và Back/Forward", async ({ page }) => {
  const user = {
    userId: 1,
    email: "admin@test.invalid",
    fullName: "Admin test",
    role: "Admin",
  };
  let warehouse = {
    warehouseId: 1,
    name: "Kho cũ",
    address: "Địa chỉ cũ",
    isActive: true,
  };
  await page.route("**/api/**", async (route) => {
    const request = route.request(),
      url = new URL(request.url()),
      path = url.pathname;
    if (path === "/api/auth/login")
      return route.fulfill({
        json: {
          accessToken: "test",
          expiresAt: new Date(Date.now() + 3600000).toISOString(),
          user,
        },
      });
    if (path === "/api/auth/me") return route.fulfill({ json: user });
    if (path === "/api/users")
      return route.fulfill({
        json: {
          items: [
            {
              ...user,
              roleName: "Admin",
              phoneNumber: "0900000000",
              status: "Locked",
              createdAt: "2026-10-01T00:00:00",
            },
          ],
          totalItems: 1,
          totalPages: 1,
        },
      });
    if (request.method() === "POST" || request.method() === "PUT") {
      warehouse = { ...warehouse, ...request.postDataJSON() };
      return route.fulfill({ json: warehouse });
    }
    if (request.method() === "DELETE") {
      warehouse.isActive = false;
      return route.fulfill({ json: warehouse });
    }
    return route.fulfill({
      json:
        path === "/api/warehouses/1"
          ? warehouse
          : { items: [warehouse], totalItems: 11, totalPages: 2 },
    });
  });
  await page.goto("/warehouses");
  await page.getByLabel("Email đăng nhập").fill(user.email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill("test");
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await page.getByRole("button", { name: "Thêm kho hàng" }).click();
  await page.getByLabel("Tên kho *").fill("Kho mới");
  await page.getByLabel("Địa chỉ").fill("Địa chỉ mới");
  await page.getByRole("button", { name: "Lưu kho", exact: true }).click();
  await expect(
    page.getByRole("cell", { name: "Kho mới", exact: true }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Sửa", exact: true }).click();
  await page.getByLabel("Tên kho *").fill("Kho đã sửa");
  await page.getByRole("button", { name: "Lưu kho", exact: true }).click();
  await page
    .getByRole("button", { name: "Ngừng hoạt động", exact: true })
    .click();
  await page.getByRole("button", { name: "Xác nhận", exact: true }).click();
  await expect(
    page.getByRole("cell", { name: "Ngừng hoạt động", exact: true }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Trang sau" }).click();
  await expect(page).toHaveURL(/page=2/);
  await page.goBack();
  await expect(page).toHaveURL(/\/warehouses$/);
  await page.goForward();
  await expect(page).toHaveURL(/page=2/);
  await page.getByRole("link", { name: "Người dùng", exact: true }).click();
  await expect(page.getByRole("cell", { name: "Đã khóa" })).toBeVisible();
  await expect(page.getByRole("cell", { name: "0900000000" })).toBeVisible();
});
