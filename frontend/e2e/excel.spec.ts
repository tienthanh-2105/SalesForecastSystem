import { test, expect } from "@playwright/test";
import ExcelJS from "exceljs";

test("Excel backend preview, import and filtered download", async ({ page }) => {
  const calls: string[] = [];
  const user = { userId: 1, email: "admin@test.invalid", fullName: "Admin", role: "Admin" };
  const book = new ExcelJS.Workbook(); book.addWorksheet("SanPham").addRow(["SKU"]);
  const buffer = Buffer.from(await book.xlsx.writeBuffer());
  await page.route("**/api/**", async route => {
    const request = route.request(), url = new URL(request.url()), path = url.pathname;
    calls.push(path + url.search);
    if (path === "/api/auth/login") return route.fulfill({ json: { accessToken: "test", expiresAt: new Date(Date.now() + 3600000).toISOString(), user } });
    if (path === "/api/auth/me") return route.fulfill({ json: user });
    if (path.endsWith("/template") || path.endsWith("/export")) return route.fulfill({ contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", body: buffer });
    if (path.includes("/import")) {
      expect(request.headers().authorization).toBe("Bearer test");
      expect(request.headers()["content-type"]).toContain("multipart/form-data");
      return route.fulfill({ json: { totalRows: 1, validRows: 1, errors: [], isValid: true, createdCount: path.endsWith("preview") ? 0 : 1 } });
    }
    if (path === "/api/categories") return route.fulfill({ json: [] });
    return route.fulfill({ json: { items: [{ productId: 1, sku: "TEST", name: "Test", salePrice: 100, minimumStockLevel: 0, isActive: true }], totalItems: 1, totalPages: 1 } });
  });
  await page.goto("/products?search=TEST");
  await page.getByLabel("Email đăng nhập").fill(user.email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill("password");
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await page.getByRole("button", { name: "Nhập Excel", exact: true }).click();
  await expect(page.getByRole("button", { name: "Nhập vào hệ thống" })).toBeDisabled();
  await page.getByLabel("File Excel (.xlsx)").setInputFiles({ name: "test.xlsx", mimeType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", buffer });
  await page.getByRole("button", { name: "Kiểm tra file" }).click();
  await expect(page.getByRole("button", { name: "Nhập vào hệ thống" })).toBeEnabled();
  await page.getByRole("button", { name: "Nhập vào hệ thống" }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  expect(calls).toContain("/api/products/import");
  const download = page.waitForEvent("download");
  await page.getByRole("button", { name: "Xuất Excel", exact: true }).click();
  expect((await download).suggestedFilename()).toBe("SanPham.xlsx");
  expect(calls.some(c => c === "/api/products/export?search=TEST")).toBe(true);
});

test("SalesStaff can view profit and group reports on mobile", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const user = { userId: 2, email: "staff@test.invalid", fullName: "Nhân viên", role: "SalesStaff" };
  await page.route("**/api/**", async route => {
    const url = new URL(route.request().url());
    if (url.pathname === "/api/auth/login") return route.fulfill({ json: { accessToken: "test", expiresAt: new Date(Date.now() + 3600000).toISOString(), user } });
    if (url.pathname === "/api/auth/me") return route.fulfill({ json: user });
    if (url.pathname.endsWith("sales-by-staff")) return route.fulfill({ json: [{ userId: 2, fullName: "Nhân viên", orderCount: 1, quantitySold: 1, revenue: 100 }] });
    if (url.pathname.endsWith("sales-by-group")) {
      expect(url.searchParams.getAll("StaffUserIds")).toEqual(["2", "3"]);
      return route.fulfill({ json: { groupName: "Nhóm thử", revenue: 100, orderCount: 1, quantitySold: 1, members: [{ userId: 2, fullName: "Nhân viên", orderCount: 1, quantitySold: 1, revenue: 100 }] } });
    }
    return route.fulfill({ json: { costMethod: "FIFO", totals: { revenue: 100, orderCount: 1, quantitySold: 1, costOfGoodsSold: null, grossProfit: null, missingCostQuantity: 1 }, periods: [] } });
  });
  await page.goto("/reports");
  await page.getByLabel("Email đăng nhập").fill(user.email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill("password");
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await expect(page.getByText(/Thiếu giá vốn/)).toBeVisible();
  await page.getByLabel("Tên nhóm", { exact: true }).fill("Nhóm thử");
  await page.getByLabel("Mã nhân viên, cách nhau bằng dấu phẩy").fill("2,3,2");
  await page.getByRole("button", { name: "Xem doanh số nhóm" }).click();
  await expect(page.getByRole("heading", { name: "Nhóm thử" })).toBeVisible();
  await page.screenshot({ path: "test-results/reports-mobile.png", fullPage: true });
});
