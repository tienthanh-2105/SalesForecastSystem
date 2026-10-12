import { test, expect } from "@playwright/test";

test("mobile stock alerts automatically clear after stock recovers", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.clock.install();
  const user = { userId: 2, email: "staff@test.invalid", fullName: "Nhân viên", role: "SalesStaff" };
  let lowStock = true;
  let checks = 0;
  await page.route("**/api/**", async route => {
    const path = new URL(route.request().url()).pathname;
    if (path === "/api/auth/login") return route.fulfill({ json: { accessToken: "test", expiresAt: new Date(Date.now() + 3600000).toISOString(), user } });
    if (path === "/api/auth/me") return route.fulfill({ json: user });
    if (path === "/api/reports/stock-alerts") {
      checks++;
      expect(route.request().headers().authorization).toBe("Bearer test");
      return route.fulfill({ json: { checkedAtUtc: new Date().toISOString(), items: lowStock ? [{ productId: 1, sku: "HOT", productName: "Sản phẩm bán chạy A", quantityOnHand: 1, minimumStockLevel: 5, quantitySold: 100, isBestSeller: true, isOutOfStock: false }] : [] } });
    }
    if (path.endsWith("sales-by-staff")) return route.fulfill({ json: [] });
    return route.fulfill({ json: { costMethod: "FIFO", totals: { revenue: 0, orderCount: 0, quantitySold: 0, costOfGoodsSold: 0, grossProfit: 0, missingCostQuantity: 0 }, periods: [] } });
  });
  await page.goto("/reports");
  await page.getByLabel("Email đăng nhập").fill(user.email);
  await page.getByLabel("Mật khẩu", { exact: true }).fill("password");
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
  await page.getByText(/1 sản phẩm cần chú ý/).click();
  await expect(page.getByText("Tồn kho thấp", { exact: true })).toBeVisible();
  await expect(page.getByText("Bán chạy", { exact: true })).toBeVisible();
  lowStock = false;
  await page.clock.fastForward(60_000);
  await expect(page.getByText("Không có sản phẩm đang kinh doanh cần cảnh báo tồn kho.")).toBeVisible();
  expect(checks).toBeGreaterThanOrEqual(2);
});
