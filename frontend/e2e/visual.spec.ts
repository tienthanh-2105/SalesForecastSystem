import { test, expect } from "@playwright/test";
for (const [name, width, height] of [
  ["desktop", 1440, 900],
  ["mobile", 390, 844],
] as const) {
  test(`layout ${name} không có lỗi console`, async ({ page }) => {
    const errors: string[] = [];
    page.on("pageerror", (e) => errors.push(e.message));
    await page.setViewportSize({ width, height });
    await page.route("**/api/**", async (route) => {
      const path = new URL(route.request().url()).pathname;
      const user = {
        userId: 1,
        email: "admin@test.invalid",
        fullName: "Admin",
        role: "Admin",
      };
      await route.fulfill({
        json:
          path === "/api/auth/login"
            ? {
                accessToken: "test",
                expiresAt: new Date(Date.now() + 3600000).toISOString(),
                user,
              }
            : path === "/api/auth/me"
              ? user
              : path === "/api/categories"
                ? [
                    {
                      categoryId: 1,
                      code: "DM-0001",
                      name: "Điện thoại thông minh",
                      description: "Điện thoại thông minh",
                      parentCategoryId: null,
                      isActive: true,
                    },
                    {
                      categoryId: 2,
                      code: "DM-0002",
                      name: "Phụ kiện",
                      description: "Phụ kiện điện thoại",
                      parentCategoryId: 1,
                      isActive: false,
                    },
                  ]
                : { items: [], totalItems: 0, totalPages: 1 },
      });
    });
    await page.goto("/login");
    await page.getByLabel("Email đăng nhập").fill("admin@test.invalid");
    await page.getByLabel("Mật khẩu", { exact: true }).fill("test");
    await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
    await expect(page.getByRole("cell", { name: "DM-0001" })).toBeVisible();
    await page.screenshot({ path: `test-results/${name}.png`, fullPage: true });
    expect(errors).toEqual([]);
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= window.innerWidth,
      ),
    ).toBe(true);
  });
}
