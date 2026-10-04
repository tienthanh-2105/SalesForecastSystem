import { test, expect } from '@playwright/test';
test('form thêm sản phẩm: mã, dấu bắt buộc, selection và tồn kho chỉ đọc', async ({ page }) => {
  let payload: Record<string, unknown> | undefined;
  const user = { userId: 1, email: 'admin@test.invalid', fullName: 'Admin', role: 'Admin' };
  await page.route('**/api/**', async route => {
    const request = route.request(), url = new URL(request.url()), path = url.pathname;
    if (path === '/api/auth/login') return route.fulfill({ json: { accessToken: 'test', expiresAt: new Date(Date.now() + 3600000).toISOString(), user } });
    if (path === '/api/auth/me') return route.fulfill({ json: user });
    if (path === '/api/categories') return route.fulfill({ json: [{ categoryId: 1, name: 'Laptop', parentCategoryId: null, isActive: true }] });
    if (path === '/api/products' && request.method() === 'POST') { payload = request.postDataJSON(); return route.fulfill({ json: { ...payload, productId: 4 } }); }
    return route.fulfill({ json: { items: url.searchParams.get('pageSize') === '1' ? [{ productId: 3 }] : [], totalItems: 0, totalPages: 1 } });
  });
  await page.goto('/products');
  await page.getByLabel('Email đăng nhập').fill(user.email);
  await page.getByLabel('Mật khẩu', { exact: true }).fill('test');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await page.getByRole('button', { name: 'Thêm sản phẩm' }).click();
  await expect(page.getByLabel('Mã hệ thống', { exact: true })).toHaveValue('D-0004');
  await expect(page.getByLabel('Mã hệ thống', { exact: true })).toHaveAttribute('readonly', '');
  await expect(page.getByLabel('Tồn kho tối thiểu')).toHaveValue('0');
  await expect(page.getByLabel('Tồn kho tối thiểu')).toHaveAttribute('readonly', '');
  await expect(page.getByRole('dialog').locator('.text-danger')).toHaveCount(6);
  await page.getByLabel('Mã sản phẩm (SKU) *').fill('LAP-TEST');
  await page.getByLabel('Tên sản phẩm *').fill('Laptop test');
  await page.getByLabel('Danh mục *', { exact: true }).selectOption('1');
  await page.getByLabel('Đơn vị *', { exact: true }).selectOption('Máy');
  await page.getByLabel('Giá bán *').fill('12000000');
  await page.getByLabel('Trạng thái *', { exact: true }).selectOption('false');
  await page.getByRole('button', { name: 'Lưu sản phẩm' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  expect(payload).toMatchObject({ unit: 'Máy', minimumStockLevel: 0, isActive: false, salePrice: 12000000 });
  expect(payload).not.toHaveProperty('productId');
});
