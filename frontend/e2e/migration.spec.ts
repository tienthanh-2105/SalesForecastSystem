import { test, expect } from "@playwright/test";
import type { Page } from "@playwright/test";
const user = {
  userId: 1,
  email: "admin@test.invalid",
  fullName: "Kiểm thử",
  role: "Admin",
};
async function mockApi(page: Page, role = "Admin") {
  const currentUser = { ...user, role };
  await page.route("**/api/**", async (route) => {
    const url = new URL(route.request().url());
    const path = url.pathname;
    const body =
      path === "/api/auth/login" || path === "/api/auth/refresh"
        ? {
            accessToken: "test-token",
            expiresAt: new Date(Date.now() + 3600000).toISOString(),
            user: currentUser,
          }
        : path === "/api/auth/me"
          ? currentUser
          : path === "/api/categories"
            ? [
                {
                  categoryId: 1,
                  code: "DM-0001",
                  name: "Điện thoại",
                  description: "Mẫu",
                  parentCategoryId: null,
                  isActive: true,
                },
              ]
            : { items: [], totalItems: 0, totalPages: 1 };
    await route.fulfill({ json: body });
  });
}
async function login(page: Page) {
  await page.getByLabel("Email đăng nhập").fill("admin@test.invalid");
  await page.getByLabel("Mật khẩu", { exact: true }).fill("test-password");
  await page.getByRole("button", { name: "Đăng nhập", exact: true }).click();
}
test("login, URL, reload, tìm kiếm và đăng xuất", async ({ page }) => {
  await mockApi(page);
  await page.goto("/categories");
  await login(page);
  await expect(page).toHaveURL(/\/categories$/);
  await expect(page.getByRole("cell", { name: "▱ Điện thoại" })).toBeVisible();
  await page.getByRole("link", { name: "Sản phẩm", exact: true }).click();
  await expect(page).toHaveURL(/\/products$/);
  await page.reload();
  await expect(
    page.getByRole("heading", { name: "TRANG QUẢN TRỊ - QUẢN LÝ SẢN PHẨM" }),
  ).toBeVisible();
  await page.getByLabel("Tìm kiếm", { exact: true }).fill("abc");
  await expect(page).toHaveURL(/search=abc/);
  await page.getByRole("button", { name: "Đăng xuất" }).click();
  await expect(page).toHaveURL(/\/login$/);
});
for (const role of ["WarehouseManager", "SalesStaff"])
  test(`route bảo vệ ${role}`, async ({ page }) => {
    await mockApi(page, role);
    await page.goto("/users");
    await login(page);
    await expect(page).toHaveURL(/\/forbidden$/);
    await expect(
      page.getByText("Tài khoản không có quyền truy cập trang này."),
    ).toBeVisible();
    await expect(page.getByRole("link", { name: "Người dùng" })).toHaveCount(0);
  });
test("mobile menu", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await mockApi(page);
  await page.goto("/login");
  await login(page);
  await page.getByRole("button", { name: "Mở menu" }).click();
  await page.getByRole("link", { name: "Kho hàng", exact: true }).click();
  await expect(page).toHaveURL(/\/warehouses$/);
});
test("danh mục thêm/sửa/xóa, trạng thái và modal", async ({ page }) => {
  await mockApi(page);
  const records = [
    {
      categoryId: 1,
      code: "DM-0001",
      name: "Điện thoại",
      description: "Mẫu",
      parentCategoryId: null,
      isActive: true,
    },
  ];
  await page.route("**/api/categories**", async (route) => {
    const method = route.request().method();
    if (method === "GET") return route.fulfill({ json: records });
    if (method === "POST")
      records.push({
        ...route.request().postDataJSON(),
        categoryId: 2,
        code: "DM-0002",
      });
    if (method === "PUT")
      Object.assign(
        records.find((c) => c.categoryId === 2)!,
        route.request().postDataJSON(),
      );
    if (method === "DELETE")
      records.splice(
        records.findIndex((c) => c.categoryId === 2),
        1,
      );
    await route.fulfill({ json: records[records.length - 1] || {} });
  });
  await page.goto("/login");
  await login(page);
  await page.getByRole("button", { name: "Thêm danh mục" }).click();
  await page.getByLabel("Tên danh mục *").fill("Danh mục test");
  await page.getByRole("button", { name: "Lưu danh mục" }).click();
  const row = page.getByRole("row").filter({ hasText: "Danh mục test" });
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: "Sửa", exact: true }).click();
  await page.getByLabel("Trạng thái", { exact: true }).selectOption("false");
  await page.getByRole("button", { name: "Lưu danh mục" }).click();
  await page.getByLabel("Tất cả trạng thái").selectOption("false");
  await expect(
    page.getByRole("row").filter({ hasText: "Điện thoại" }),
  ).toHaveCount(0);
  await row.getByRole("button", { name: "Xóa", exact: true }).click();
  await page.getByRole("button", { name: "Xác nhận", exact: true }).click();
  await expect(page.getByText("Không tìm thấy dữ liệu phù hợp.")).toBeVisible();
});
test("sản phẩm upload đúng field, preview và xung đột rowVersion", async ({
  page,
}) => {
  await mockApi(page);
  const product = {
    productId: 1,
    sku: "SP-TEST",
    name: "Sản phẩm test",
    categoryId: 1,
    unit: "Cái",
    salePrice: 100,
    minimumStockLevel: 0,
    isActive: true,
    rowVersion: "original",
    imageUrl: null,
    description: null,
  };
  await page.route("**/api/products**", async (route) => {
    const request = route.request(),
      path = new URL(request.url()).pathname;
    if (path === "/api/products/images") {
      expect(request.headers()["content-type"]).toContain(
        "multipart/form-data",
      );
      expect(request.postDataBuffer()?.toString()).toContain('name="image"');
      return route.fulfill({
        json: { imageUrl: "http://127.0.0.1:5173/uploads/test.png" },
      });
    }
    if (request.method() === "PUT") {
      expect(request.postDataJSON().rowVersion).toBe("original");
      return route.fulfill({
        status: 409,
        json: { message: "Dữ liệu đã thay đổi." },
      });
    }
    await route.fulfill({
      json: path.endsWith("/stock")
        ? { quantityOnHand: 5 }
        : path === "/api/products/1"
          ? product
          : { items: [product], totalItems: 1, totalPages: 1 },
    });
  });
  await page.goto("/products");
  await login(page);
  await page.getByRole("button", { name: "Sửa", exact: true }).click();
  await page.locator("input[type=file]").setInputFiles({
    name: "test.png",
    mimeType: "image/png",
    buffer: Buffer.from("test-image"),
  });
  await expect(page.getByAltText("Xem trước sản phẩm")).toBeVisible();
  await page.getByRole("button", { name: "Lưu sản phẩm" }).click();
  await expect(page.getByText("Dữ liệu đã thay đổi.")).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Lưu sản phẩm" }),
  ).toBeDisabled();
});
test("lỗi tải dữ liệu và retry không mất URL", async ({ page }) => {
  await mockApi(page);
  let failed = true;
  await page.route("**/api/warehouses**", (route) =>
    route.fulfill(
      failed
        ? { status: 500, json: { message: "Lỗi kiểm thử" } }
        : { json: { items: [], totalItems: 0, totalPages: 1 } },
    ),
  );
  await page.goto("/warehouses?search=kho");
  await login(page);
  await expect(page.getByText("Lỗi kiểm thử")).toBeVisible();
  failed = false;
  await page.getByRole("button", { name: "Thử lại", exact: true }).click();
  await expect(page.getByText("Không tìm thấy dữ liệu phù hợp.")).toBeVisible();
  await expect(page).toHaveURL(/search=kho/);
});
