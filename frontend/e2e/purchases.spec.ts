import { expect, test } from "@playwright/test";
test("nhập hàng: nhà cung cấp, lưu dở dang không tạo trùng, xác nhận tăng kho và lọc", async ({ page }) => {
  const user = { userId: 1, email: "warehouse@test.invalid", fullName: "Kho test", role: "WarehouseManager" };
  let suppliers: { supplierId: number; name: string; isActive: boolean }[] = [];
  let purchase: Record<string, unknown> | null = null;
  let items: Record<string, unknown>[] = [];
  let created = 0, failItem = true, stock = 0;
  await page.route("**/api/**", async route => {
    const request = route.request(), path = new URL(request.url()).pathname;
    if (path === "/api/auth/login") return route.fulfill({ json: { accessToken: "test", expiresAt: new Date(Date.now() + 3600000).toISOString(), user } });
    if (path === "/api/auth/me") return route.fulfill({ json: user });
    if (path === "/api/suppliers" && request.method() === "POST") { suppliers = [{ ...request.postDataJSON(), supplierId: 1 }]; return route.fulfill({ json: suppliers[0] }); }
    if (path === "/api/purchases" && request.method() === "POST") { created++; purchase = { ...request.postDataJSON(), purchaseOrderId: 1, status: "Draft" }; return route.fulfill({ json: { ...purchase, items } }); }
    if (path === "/api/purchases/1" && request.method() === "PUT") { purchase = { ...purchase, ...request.postDataJSON() }; return route.fulfill({ json: { ...purchase, items } }); }
    if (path === "/api/purchases/1/items" && request.method() === "POST") {
      if (failItem) { failItem = false; return route.fulfill({ status: 400, json: { message: "Lỗi dòng nhập thử" } }); }
      items = [{ ...request.postDataJSON(), purchaseOrderItemId: 1 }]; return route.fulfill({ json: { ...purchase, items } });
    }
    if (path === "/api/purchases/1/post") { purchase = { ...purchase, status: "Posted" }; stock += Number(items[0].quantity); return route.fulfill({ json: { ...purchase, items } }); }
    if (path === "/api/purchases/1") return route.fulfill({ json: { ...purchase, items } });
    const rows = path === "/api/suppliers" ? suppliers : path === "/api/warehouses" ? [{ warehouseId: 1, name: "Kho test", isActive: true }] : path === "/api/products" ? [{ productId: 1, name: "Sản phẩm hết hàng", isActive: true, quantityOnHand: 0 }] : purchase ? [purchase] : [];
    return route.fulfill({ json: { items: rows, totalItems: rows.length, totalPages: 1 } });
  });
  await page.goto("/purchases");
  await page.getByLabel("Email đăng nhập").fill(user.email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill("test");
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await expect(page.getByRole("link", { name: "Nhập hàng", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Thêm nhà cung cấp" }).click();
  await page.getByLabel("Tên nhà cung cấp").fill("Nhà cung cấp test");
  await page.getByLabel("Mã số thuế", { exact: false }).fill("0123456789");
  await page.getByRole("button", { name: "Lưu nhà cung cấp" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.getByRole("button", { name: "Thêm phiếu nhập" }).click();
  await page.getByLabel("Kho nhập").selectOption("1");
  await page.getByRole("dialog").getByLabel("Nhà cung cấp").selectOption("1");
  await page.getByLabel("Mã sản phẩm").selectOption("1");
  await page.getByLabel("Số lượng dòng 1").fill("5");
  await page.getByLabel("Giá nhập dòng 1").fill("100000");
  await page.getByRole("button", { name: "Lưu phiếu nháp" }).click();
  await expect(page.getByRole("alert").filter({ hasText: "Đã tải lại phiếu lưu một phần" })).toBeVisible();
  expect(created).toBe(1); expect(stock).toBe(0);
  await page.getByRole("button", { name: "Thêm dòng sản phẩm" }).click();
  await page.getByLabel("Mã sản phẩm").selectOption("1");
  await page.getByLabel("Số lượng dòng 1").fill("5");
  await page.getByLabel("Giá nhập dòng 1").fill("100000");
  await page.getByRole("button", { name: "Lưu phiếu nháp" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  expect(created).toBe(1); expect(stock).toBe(0);
  await page.getByRole("button", { name: "Xác nhận nhập kho" }).click();
  await page.getByRole("dialog").getByRole("button", { name: "Xác nhận", exact: true }).click();
  await expect(page.getByRole("cell", { name: "Đã nhập kho", exact: true })).toBeVisible();
  expect(stock).toBe(5);
  await expect(page.getByRole("button", { name: "Sửa", exact: true })).toHaveCount(0);
  await page.getByLabel("Trạng thái phiếu").selectOption("Posted");
  await expect(page).toHaveURL(/status=Posted/);
  await page.reload();
  await expect(page.getByLabel("Trạng thái phiếu")).toHaveValue("Posted");
});
