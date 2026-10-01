const API_BASE = document.querySelector('meta[name="api-base-url"]')?.content?.replace(/\/$/, '') || '';
const IDLE_TIMEOUT_MS = 15 * 60 * 1000;
const WARNING_BEFORE_MS = 60 * 1000;
const REFRESH_INTERVAL_MS = 5 * 60 * 1000;
const STORAGE_KEY = 'salesForecast.session';

const elements = {
  loginView: document.querySelector('#loginView'),
  sessionView: document.querySelector('#sessionView'),
  loginForm: document.querySelector('#loginForm'),
  email: document.querySelector('#email'),
  password: document.querySelector('#password'),
  rememberMe: document.querySelector('#rememberMe'),
  togglePassword: document.querySelector('#togglePassword'),
  loginButton: document.querySelector('#loginButton'),
  loginMessage: document.querySelector('#loginMessage'),
  logoutButton: document.querySelector('#logoutButton'),
  userEmail: document.querySelector('#userEmail'),
  userRole: document.querySelector('#userRole'),
  idleCountdown: document.querySelector('#idleCountdown'),
  sessionProgress: document.querySelector('#sessionProgress'),
  warning: document.querySelector('#sessionWarning'),
  warningCountdown: document.querySelector('#warningCountdown'),
  continueButton: document.querySelector('#continueSessionButton'),
  adminPanel: document.querySelector('#adminPanel'),
  accessDeniedPanel: document.querySelector('#accessDeniedPanel'),
  adminSidebar: document.querySelector('#adminSidebar'),
  sidebarBackdrop: document.querySelector('#sidebarBackdrop'),
  sidebarToggle: document.querySelector('#sidebarToggle'),
  navItems: document.querySelectorAll('.admin-nav-item'),
  topbarTitle: document.querySelector('#topbarTitle'),
  sectionTitle: document.querySelector('#sectionTitle'),
  sectionDescription: document.querySelector('#sectionDescription'),
  managementSearch: document.querySelector('#managementSearch'),
  resultCount: document.querySelector('#resultCount'),
  refreshDataButton: document.querySelector('#refreshDataButton'),
  retryDataButton: document.querySelector('#retryDataButton'),
  dataLoading: document.querySelector('#dataLoading'),
  dataError: document.querySelector('#dataError'),
  dataEmpty: document.querySelector('#dataEmpty'),
  dataTableWrap: document.querySelector('#dataTableWrap'),
  dataTableHead: document.querySelector('#dataTableHead'),
  dataTableBody: document.querySelector('#dataTableBody'),
  tableFooter: document.querySelector('#tableFooter'),
  pageInfo: document.querySelector('#pageInfo'),
  previousPageButton: document.querySelector('#previousPageButton'),
  nextPageButton: document.querySelector('#nextPageButton'),
  primaryActionButton: document.querySelector('#primaryActionButton'),
  categoryFilters: document.querySelector('#categoryFilters'),
  categoryStatusFilter: document.querySelector('#categoryStatusFilter'),
  warehouseFilters: document.querySelector('#warehouseFilters'),
  warehouseStatusFilter: document.querySelector('#warehouseStatusFilter'),
  productFilters: document.querySelector('#productFilters'),
  categoryFilter: document.querySelector('#categoryFilter'),
  stockFilter: document.querySelector('#stockFilter'),
  orderFilters: document.querySelector('#orderFilters'),
  orderStatusFilter: document.querySelector('#orderStatusFilter'),
  orderFromDate: document.querySelector('#orderFromDate'),
  orderToDate: document.querySelector('#orderToDate'),
  entityModal: document.querySelector('#entityModal'),
  entityModalDialog: document.querySelector('#entityModalDialog'),
  entityForm: document.querySelector('#entityForm'),
  entityFormMessage: document.querySelector('#entityFormMessage'),
  entityModalEyebrow: document.querySelector('#entityModalEyebrow'),
  entityModalTitle: document.querySelector('#entityModalTitle'),
  entityId: document.querySelector('#entityId'),
  categoryFormFields: document.querySelector('#categoryFormFields'),
  productFormFields: document.querySelector('#productFormFields'),
  warehouseFormFields: document.querySelector('#warehouseFormFields'),
  categoryIdDisplay: document.querySelector('#categoryIdDisplay'),
  categoryName: document.querySelector('#categoryName'),
  categoryParent: document.querySelector('#categoryParent'),
  categoryDescription: document.querySelector('#categoryDescription'),
  categoryActive: document.querySelector('#categoryActive'),
  productIdDisplay: document.querySelector('#productIdDisplay'),
  productSku: document.querySelector('#productSku'),
  productName: document.querySelector('#productName'),
  productCategory: document.querySelector('#productCategory'),
  productUnit: document.querySelector('#productUnit'),
  productPrice: document.querySelector('#productPrice'),
  productMinimumStock: document.querySelector('#productMinimumStock'),
  productQuantity: document.querySelector('#productQuantity'),
  quantityField: document.querySelector('#quantityField'),
  productActive: document.querySelector('#productActive'),
  productImageFile: document.querySelector('#productImageFile'),
  productImageUrl: document.querySelector('#productImageUrl'),
  productImagePreview: document.querySelector('#productImagePreview'),
  productDescription: document.querySelector('#productDescription'),
  warehouseIdDisplay: document.querySelector('#warehouseIdDisplay'),
  warehouseName: document.querySelector('#warehouseName'),
  warehouseAddress: document.querySelector('#warehouseAddress'),
  warehouseActive: document.querySelector('#warehouseActive'),
  orderFormFields: document.querySelector('#orderFormFields'),
  orderIdDisplay: document.querySelector('#orderIdDisplay'),
  orderNumber: document.querySelector('#orderNumber'),
  orderDate: document.querySelector('#orderDate'),
  orderWarehouse: document.querySelector('#orderWarehouse'),
  orderCustomer: document.querySelector('#orderCustomer'),
  orderStatusDisplay: document.querySelector('#orderStatusDisplay'),
  orderShippingAddress: document.querySelector('#orderShippingAddress'),
  orderNotes: document.querySelector('#orderNotes'),
  addOrderItemButton: document.querySelector('#addOrderItemButton'),
  orderItemsBody: document.querySelector('#orderItemsBody'),
  orderItemsEmpty: document.querySelector('#orderItemsEmpty'),
  orderTotalAmount: document.querySelector('#orderTotalAmount'),
  closeEntityModal: document.querySelector('#closeEntityModal'),
  cancelEntityModal: document.querySelector('#cancelEntityModal'),
  saveEntityButton: document.querySelector('#saveEntityButton'),
  confirmModal: document.querySelector('#confirmModal'),
  confirmModalTitle: document.querySelector('#confirmModalTitle'),
  confirmModalMessage: document.querySelector('#confirmModalMessage'),
  confirmModalError: document.querySelector('#confirmModalError'),
  cancelConfirmButton: document.querySelector('#cancelConfirmButton'),
  confirmActionButton: document.querySelector('#confirmActionButton'),
  appToast: document.querySelector('#appToast')
};

const roleLabels = {
  Admin: 'Quản trị viên',
  WarehouseManager: 'Quản lý kho',
  SalesStaff: 'Nhân viên bán hàng'
};

const managementSections = {
  categories: {
    title: 'TRANG QUẢN TRỊ - QUẢN LÝ DANH MỤC',
    heading: 'Danh mục sản phẩm',
    description: 'Theo dõi và quản lý các nhóm sản phẩm trong hệ thống.',
    placeholder: 'Tìm kiếm danh mục...'
  },
  products: {
    title: 'TRANG QUẢN TRỊ - QUẢN LÝ SẢN PHẨM',
    heading: 'Danh sách sản phẩm',
    description: 'Theo dõi thông tin, giá bán và lượng tồn của sản phẩm.',
    placeholder: 'Tìm theo SKU hoặc tên sản phẩm...'
  },
  warehouses: {
    title: 'TRANG QUẢN TRỊ - QUẢN LÝ KHO',
    heading: 'Danh sách kho hàng',
    description: 'Theo dõi địa chỉ và trạng thái hoạt động của các kho trong hệ thống.',
    placeholder: 'Tìm theo tên hoặc địa chỉ kho...'
  },
  orders: {
    title: 'TRANG QUẢN TRỊ - QUẢN LÝ ĐƠN HÀNG',
    heading: 'Danh sách đơn hàng',
    description: 'Theo dõi khách hàng, sản phẩm, giá trị và tiến trình giao hàng.',
    placeholder: 'Tìm theo mã đơn, tên hoặc số điện thoại khách hàng...'
  },
  users: {
    title: 'TRANG QUẢN TRỊ - QUẢN LÝ NGƯỜI DÙNG',
    heading: 'Tài khoản người dùng',
    description: 'Theo dõi tài khoản, vai trò và trạng thái hoạt động.',
    placeholder: 'Tìm theo tên hoặc email...'
  }
};

let session = readSession();
let idleTimer = null;
let countdownTimer = null;
let refreshInFlight = false;
let activityWriteAt = 0;
let managementState = { section: 'categories', page: 1, totalPages: 1, search: '', categoryStatus: '', warehouseStatus: '', categoryId: '', stockStatus: '', orderStatus: '', fromDate: '', toDate: '' };
let searchTimer = null;
let categoryCache = null;
let editingProductRowVersion = '';
let pendingConfirmation = null;
let toastTimer = null;
let productImagePreviewUrl = '';
let customerCache = null;
let warehouseCache = null;
let orderProductCache = null;
let editingOrder = null;

function readSession() {
  try {
    return JSON.parse(sessionStorage.getItem(STORAGE_KEY) || localStorage.getItem(STORAGE_KEY) || 'null');
  } catch {
    sessionStorage.removeItem(STORAGE_KEY);
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

function saveSession() {
  sessionStorage.removeItem(STORAGE_KEY);
  localStorage.removeItem(STORAGE_KEY);
  if (!session) return;
  const storage = session.rememberMe ? localStorage : sessionStorage;
  storage.setItem(STORAGE_KEY, JSON.stringify(session));
}

async function api(path, options = {}) {
  const headers = new Headers(options.headers || {});
  if (session?.accessToken) headers.set('Authorization', `Bearer ${session.accessToken}`);
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
  const response = await fetch(`${API_BASE}${path}`, { ...options, headers, cache: 'no-store' });
  const data = response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    const message = data?.errors ? Object.values(data.errors).flat().join(' ') : data?.detail || data?.title || data?.message;
    const error = new Error(message || `Yêu cầu thất bại (${response.status}).`);
    error.status = response.status;
    throw error;
  }
  return data;
}

function setButtonBusy(button, busy, text = 'Đang đăng nhập...') {
  if (busy) {
    button.dataset.content = button.innerHTML;
    button.disabled = true;
    button.innerHTML = `<span class="spinner-border spinner-border-sm" aria-hidden="true"></span><span>${text}</span>`;
  } else {
    button.disabled = false;
    button.innerHTML = button.dataset.content || button.innerHTML;
  }
}

function showMessage(message, type = 'danger') {
  elements.loginMessage.className = `alert app-alert alert-${type}`;
  elements.loginMessage.textContent = message;
}

function hideMessage() {
  elements.loginMessage.classList.add('d-none');
}

function updateFieldValidationMessages() {
  elements.loginForm.querySelectorAll('.form-control').forEach(field => {
    const feedback = field.closest('.form-group')?.querySelector('.invalid-feedback');
    feedback?.classList.toggle('d-block', !field.validity.valid);
  });
}

function clearFieldValidationMessages() {
  elements.loginForm.querySelectorAll('.invalid-feedback').forEach(feedback =>
    feedback.classList.remove('d-block'));
}

function initials(name) {
  return String(name || 'A').trim().split(/\s+/).slice(-2).map(part => part[0]).join('').toUpperCase();
}

function escapeHtml(value) {
  return String(value ?? '').replace(/[&<>'"]/g, character => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;'
  })[character]);
}

function formatMoney(value) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 })
    .format(Number(value) || 0);
}

function formatDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat('vi-VN').format(date);
}

function statusBadge(label, style = 'active') {
  return `<span class="status-badge status-${style}">${escapeHtml(label)}</span>`;
}

const orderStatusConfig = {
  Draft: { label: 'Nháp', style: 'info' },
  Pending: { label: 'Chờ xử lý', style: 'warning' },
  Delivering: { label: 'Đang giao', style: 'delivering' },
  Completed: { label: 'Đã giao', style: 'active' },
  Cancelled: { label: 'Đã hủy', style: 'inactive' }
};

function orderStatusBadge(status) {
  const config = orderStatusConfig[status] || { label: status, style: 'info' };
  return statusBadge(config.label, config.style);
}

function toDateInputValue(value = new Date()) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

function showToast(message, type = 'success') {
  window.clearTimeout(toastTimer);
  elements.appToast.classList.remove('d-none', 'toast-error');
  elements.appToast.classList.toggle('toast-error', type === 'error');
  elements.appToast.querySelector('i').className = `bi bi-${type === 'error' ? 'exclamation-circle' : 'check-circle'}`;
  elements.appToast.querySelector('span').textContent = message;
  toastTimer = window.setTimeout(() => elements.appToast.classList.add('d-none'), 3500);
}

function closeEntityModal() {
  elements.entityModal.classList.add('d-none');
  elements.entityForm.classList.remove('was-validated');
  elements.entityFormMessage.classList.add('d-none');
  document.body.classList.remove('modal-open');
}

function closeConfirmModal() {
  elements.confirmModal.classList.add('d-none');
  elements.confirmModalError.classList.add('d-none');
  pendingConfirmation = null;
  document.body.classList.remove('modal-open');
}

function updateImagePreview() {
  if (productImagePreviewUrl) {
    URL.revokeObjectURL(productImagePreviewUrl);
    productImagePreviewUrl = '';
  }

  const file = elements.productImageFile.files[0];
  const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
  const validFile = !file || (allowedTypes.includes(file.type) && file.size <= 5 * 1024 * 1024);
  elements.productImageFile.setCustomValidity(validFile ? '' : 'Ảnh không hợp lệ.');

  if (file && validFile) {
    productImagePreviewUrl = URL.createObjectURL(file);
    elements.productImagePreview.querySelector('img').src = productImagePreviewUrl;
    elements.productImagePreview.classList.remove('d-none');
    return;
  }

  const value = elements.productImageUrl.value.trim();
  elements.productImagePreview.classList.toggle('d-none', !value);
  if (value) elements.productImagePreview.querySelector('img').src = value;
}

async function uploadProductImage(file) {
  const formData = new FormData();
  formData.append('image', file);
  return api('/api/products/images', { method: 'POST', body: formData });
}

async function getCategories(force = false) {
  if (!force && categoryCache) return categoryCache;
  categoryCache = await api('/api/categories');
  return categoryCache;
}

function categoryDisplayName(category) {
  return category.parentCategoryName
    ? `${category.parentCategoryName} / ${category.name}`
    : category.name;
}

function fillCategorySelect(select, categories, includeAll = false, leafOnly = false) {
  const currentValue = select.value;
  select.innerHTML = includeAll
    ? '<option value="">Tất cả danh mục</option>'
    : '<option value="">Chọn danh mục</option>';
  categories
    .filter(category => !leafOnly || (!category.hasChildren && category.isActive))
    .forEach(category => {
    const option = document.createElement('option');
    option.value = String(category.categoryId);
    option.textContent = `${categoryDisplayName(category)}${category.isActive ? '' : ' (ngừng hoạt động)'}`;
    select.append(option);
  });
  select.value = currentValue;
}

function fillParentCategorySelect(categories, editingId = null) {
  const currentValue = elements.categoryParent.value;
  elements.categoryParent.innerHTML = '<option value="">Đây là danh mục gốc</option>';
  categories
    .filter(category => !category.parentCategoryId && category.categoryId !== editingId)
    .forEach(category => {
      const option = document.createElement('option');
      option.value = String(category.categoryId);
      option.textContent = `${category.code || formatEntityCode('DM', category.categoryId)} — ${category.name}`;
      elements.categoryParent.append(option);
    });
  elements.categoryParent.value = currentValue;
}

function formatEntityCode(prefix, id) {
  return `${prefix}-${String(id).padStart(4, '0')}`;
}

function getNextCategoryCode(categories) {
  const usedNumbers = new Set(categories
    .map(category => /^DM-(\d+)$/.exec(category.code || '')?.[1])
    .filter(Boolean)
    .map(Number));
  let nextNumber = 1;
  while (usedNumbers.has(nextNumber)) nextNumber += 1;
  return formatEntityCode('DM', nextNumber);
}

async function refreshCategoryOptions(force = false) {
  const categories = await getCategories(force);
  fillCategorySelect(elements.categoryFilter, categories, true);
  fillCategorySelect(elements.productCategory, categories, false, true);
  return categories;
}

async function getOrderFormOptions(force = false) {
  if (force || !customerCache || !warehouseCache || !orderProductCache) {
    const [customers, warehouses, products] = await Promise.all([
      api('/api/customers?isActive=true&page=1&pageSize=100'),
      api('/api/warehouses?isActive=true&page=1&pageSize=100'),
      api('/api/products?isActive=true&page=1&pageSize=100&sortBy=Name&sortDirection=Asc')
    ]);
    customerCache = customers.items;
    warehouseCache = warehouses.items;
    orderProductCache = products.items;
  }
  return { customers: customerCache, warehouses: warehouseCache, products: orderProductCache };
}

function fillOrderReferenceSelects() {
  const customerValue = elements.orderCustomer.value;
  const warehouseValue = elements.orderWarehouse.value;
  elements.orderCustomer.innerHTML = '<option value="">Khách lẻ</option>';
  customerCache.forEach(customer => elements.orderCustomer.add(new Option(
    `${customer.fullName}${customer.phoneNumber ? ` — ${customer.phoneNumber}` : ''}`,
    String(customer.customerId)
  )));
  elements.orderWarehouse.innerHTML = '<option value="">Chọn kho</option>';
  warehouseCache.forEach(warehouse => elements.orderWarehouse.add(new Option(warehouse.name, String(warehouse.warehouseId))));
  elements.orderCustomer.value = customerValue;
  elements.orderWarehouse.value = warehouseValue;
}

function createOrderNumber() {
  const now = new Date();
  const stamp = [now.getFullYear(), String(now.getMonth() + 1).padStart(2, '0'), String(now.getDate()).padStart(2, '0')].join('');
  const time = [now.getHours(), now.getMinutes(), now.getSeconds()].map(value => String(value).padStart(2, '0')).join('');
  return `DH-${stamp}-${time}`;
}

function orderProductOptions(selectedId) {
  return ['<option value="">Chọn sản phẩm</option>', ...orderProductCache.map(product =>
    `<option value="${product.productId}" data-price="${product.salePrice}" ${product.productId === Number(selectedId) ? 'selected' : ''}>${escapeHtml(product.sku)} — ${escapeHtml(product.name)}</option>`
  )].join('');
}

function updateOrderTotal() {
  let total = 0;
  elements.orderItemsBody.querySelectorAll('tr').forEach(row => {
    const quantity = Number(row.querySelector('.order-item-quantity').value) || 0;
    const price = Number(row.querySelector('.order-item-price').value) || 0;
    const discount = Number(row.querySelector('.order-item-discount').value) || 0;
    const lineTotal = Math.max(0, quantity * price - discount);
    row.querySelector('.order-line-total').textContent = formatMoney(lineTotal);
    total += lineTotal;
  });
  elements.orderTotalAmount.textContent = formatMoney(total);
  elements.orderItemsEmpty.classList.toggle('d-none', elements.orderItemsBody.children.length > 0);
}

function addOrderItemRow(item = {}) {
  const row = document.createElement('tr');
  if (item.salesOrderItemId) row.dataset.itemId = String(item.salesOrderItemId);
  row.innerHTML = `
    <td><select class="form-select order-item-product" required>${orderProductOptions(item.productId)}</select><div class="invalid-feedback">Chọn sản phẩm.</div></td>
    <td><input class="form-control order-item-quantity" type="number" min="1" step="1" value="${item.quantity || 1}" required></td>
    <td><input class="form-control order-item-price" type="number" min="0" step="1000" value="${item.unitPrice ?? ''}" required></td>
    <td><input class="form-control order-item-discount" type="number" min="0" step="1000" value="${item.discount || 0}" required></td>
    <td><strong class="order-line-total">${formatMoney(item.lineTotal || 0)}</strong></td>
    <td><button class="table-action table-action-danger remove-order-item" type="button" title="Bỏ sản phẩm"><i class="bi bi-x-lg"></i></button></td>`;
  elements.orderItemsBody.append(row);
  if (!item.unitPrice && item.productId) {
    row.querySelector('.order-item-price').value = orderProductCache.find(product => product.productId === item.productId)?.salePrice || 0;
  }
  updateOrderTotal();
}

function setOrderFormReadOnly(readOnly) {
  elements.orderFormFields.querySelectorAll('input:not([readonly]), select, textarea').forEach(field => field.disabled = readOnly);
  elements.addOrderItemButton.classList.toggle('d-none', readOnly);
  elements.orderItemsBody.querySelectorAll('.remove-order-item').forEach(button => button.classList.toggle('d-none', readOnly));
  elements.saveEntityButton.classList.toggle('d-none', readOnly);
}

function resetEntityForm() {
  if (productImagePreviewUrl) {
    URL.revokeObjectURL(productImagePreviewUrl);
    productImagePreviewUrl = '';
  }
  elements.entityForm.reset();
  elements.entityForm.classList.remove('was-validated');
  elements.entityFormMessage.classList.add('d-none');
  elements.entityId.value = '';
  elements.categoryIdDisplay.value = 'DM-0001';
  elements.productIdDisplay.value = 'SP-0001';
  elements.warehouseIdDisplay.value = 'KHO-0001';
  elements.categoryActive.value = 'true';
  elements.warehouseActive.value = 'true';
  elements.productActive.checked = true;
  elements.productMinimumStock.value = '0';
  elements.productQuantity.value = '0';
  elements.productImagePreview.classList.add('d-none');
  elements.productImagePreview.querySelector('img').removeAttribute('src');
  elements.orderIdDisplay.value = 'DH-0001';
  elements.orderNumber.value = '';
  elements.orderDate.value = toDateInputValue();
  elements.orderStatusDisplay.value = 'Nháp';
  elements.orderItemsBody.innerHTML = '';
  elements.orderItemsEmpty.classList.remove('d-none');
  elements.orderTotalAmount.textContent = formatMoney(0);
  elements.saveEntityButton.classList.remove('d-none');
  editingProductRowVersion = '';
  editingOrder = null;
}

async function openEntityModal(type, id = null, viewOnly = false) {
  resetEntityForm();
  const editing = Number.isInteger(id);
  elements.categoryFormFields.classList.toggle('d-none', type !== 'category');
  elements.productFormFields.classList.toggle('d-none', type !== 'product');
  elements.warehouseFormFields.classList.toggle('d-none', type !== 'warehouse');
  elements.orderFormFields.classList.toggle('d-none', type !== 'order');
  elements.entityModalDialog.classList.toggle('product-modal-dialog', type === 'product');
  elements.entityModalDialog.classList.toggle('order-modal-dialog', type === 'order');
  elements.categoryFormFields.querySelectorAll('input, textarea, select').forEach(field => {
    field.disabled = type !== 'category';
  });
  elements.productFormFields.querySelectorAll('input, textarea, select').forEach(field => {
    field.disabled = type !== 'product';
  });
  elements.warehouseFormFields.querySelectorAll('input, textarea, select').forEach(field => {
    field.disabled = type !== 'warehouse';
  });
  elements.orderFormFields.querySelectorAll('input, textarea, select').forEach(field => {
    field.disabled = type !== 'order';
  });
  elements.quantityField.classList.toggle('d-none', type !== 'product' || !editing);
  elements.entityModalEyebrow.textContent = viewOnly ? 'CHI TIẾT' : editing ? 'CHỈNH SỬA' : 'TẠO MỚI';
  const entityLabel = type === 'category' ? 'danh mục'
    : type === 'product' ? 'sản phẩm'
      : type === 'warehouse' ? 'kho' : 'đơn hàng';
  elements.entityModalTitle.textContent = `${viewOnly ? 'Chi tiết' : editing ? 'Cập nhật' : 'Thêm'} ${entityLabel}`;
  elements.entityForm.dataset.entityType = type;
  elements.entityId.value = editing ? String(id) : '';
  if (editing) {
    if (type === 'category') elements.categoryIdDisplay.value = formatEntityCode('DM', id);
    if (type === 'product') elements.productIdDisplay.value = formatEntityCode('SP', id);
    if (type === 'warehouse') elements.warehouseIdDisplay.value = formatEntityCode('KHO', id);
  }
  document.body.classList.add('modal-open');
  elements.entityModal.classList.remove('d-none');

  try {
    if (type === 'category') {
      const categories = await getCategories();
      fillParentCategorySelect(categories, editing ? id : null);
      if (editing) {
        const category = categories.find(item => item.categoryId === id) || await api(`/api/categories/${id}`);
        elements.categoryIdDisplay.value = category.code || formatEntityCode('DM', category.categoryId);
        elements.categoryName.value = category.name;
        elements.categoryParent.value = category.parentCategoryId ? String(category.parentCategoryId) : '';
        elements.categoryDescription.value = category.description || '';
        elements.categoryActive.value = String(category.isActive);
      } else {
        elements.categoryIdDisplay.value = getNextCategoryCode(categories);
      }
    }

    if (type === 'product') {
      await refreshCategoryOptions();
      if (editing) {
        const [product, stock] = await Promise.all([
          api(`/api/products/${id}`),
          api(`/api/products/${id}/stock`)
        ]);
        elements.productSku.value = product.sku;
        elements.productName.value = product.name;
        elements.productCategory.value = String(product.categoryId);
        if (![...elements.productUnit.options].some(option => option.value === product.unit)) {
          elements.productUnit.add(new Option(product.unit, product.unit));
        }
        elements.productUnit.value = product.unit;
        elements.productPrice.value = product.salePrice;
        elements.productMinimumStock.value = product.minimumStockLevel;
        elements.productQuantity.value = stock.quantityOnHand;
        elements.productActive.checked = product.isActive;
        elements.productImageUrl.value = product.imageUrl || '';
        elements.productDescription.value = product.description || '';
        editingProductRowVersion = product.rowVersion;
        updateImagePreview();
      } else {
        const latest = await api('/api/products?page=1&pageSize=1&sortBy=CreatedAt&sortDirection=Desc');
        const nextId = latest.items.length ? latest.items[0].productId + 1 : 1;
        elements.productIdDisplay.value = formatEntityCode('SP', nextId);
      }
    }

    if (type === 'warehouse') {
      if (editing) {
        const warehouse = await api(`/api/warehouses/${id}`);
        elements.warehouseIdDisplay.value = formatEntityCode('KHO', warehouse.warehouseId);
        elements.warehouseName.value = warehouse.name;
        elements.warehouseAddress.value = warehouse.address || '';
        elements.warehouseActive.value = String(warehouse.isActive);
      } else {
        const latest = await api('/api/warehouses?page=1&pageSize=100');
        const nextId = latest.items.length
          ? Math.max(...latest.items.map(item => item.warehouseId)) + 1
          : 1;
        elements.warehouseIdDisplay.value = formatEntityCode('KHO', nextId);
      }
    }

    if (type === 'order') {
      await getOrderFormOptions();
      fillOrderReferenceSelects();
      if (editing) {
        editingOrder = await api(`/api/sales/${id}`);
        elements.orderIdDisplay.value = formatEntityCode('DH', editingOrder.salesOrderId);
        elements.orderNumber.value = editingOrder.orderNumber;
        elements.orderDate.value = toDateInputValue(editingOrder.orderDate);
        elements.orderWarehouse.value = String(editingOrder.warehouseId);
        elements.orderCustomer.value = editingOrder.customerId ? String(editingOrder.customerId) : '';
        elements.orderStatusDisplay.value = orderStatusConfig[editingOrder.status]?.label || editingOrder.status;
        elements.orderShippingAddress.value = editingOrder.shippingAddress || '';
        elements.orderNotes.value = editingOrder.notes || '';
        editingOrder.items.forEach(addOrderItemRow);
        setOrderFormReadOnly(viewOnly || editingOrder.status !== 'Draft');
      } else {
        elements.orderNumber.value = createOrderNumber();
        elements.orderDate.value = toDateInputValue();
        addOrderItemRow();
        setOrderFormReadOnly(false);
      }
    }
  } catch (error) {
    closeEntityModal();
    showToast(`Không thể mở dữ liệu chỉnh sửa. ${error.message}`, 'error');
  }
}

function openDeleteConfirmation(type, id, name) {
  pendingConfirmation = { type, id };
  const isCategory = type === 'category';
  const isWarehouse = type === 'warehouse';
  elements.confirmModalTitle.textContent = isCategory
    ? 'Xóa danh mục?'
    : isWarehouse ? 'Ngừng hoạt động kho?' : 'Ngừng kinh doanh sản phẩm?';
  elements.confirmModalMessage.textContent = isCategory
    ? `Danh mục “${name}” sẽ bị xóa nếu chưa được sản phẩm sử dụng.`
    : isWarehouse
      ? `Kho “${name}” sẽ chuyển sang trạng thái ngừng hoạt động và không còn xuất hiện khi tạo đơn mới.`
      : `Sản phẩm “${name}” sẽ chuyển sang trạng thái ngừng kinh doanh.`;
  elements.confirmActionButton.textContent = isCategory
    ? 'Xóa danh mục'
    : isWarehouse ? 'Ngừng hoạt động' : 'Ngừng kinh doanh';
  elements.confirmActionButton.classList.add('btn-danger');
  elements.confirmActionButton.classList.remove('btn-brand');
  elements.confirmModalError.classList.add('d-none');
  document.body.classList.add('modal-open');
  elements.confirmModal.classList.remove('d-none');
}

function openOrderConfirmation(id, orderNumber, action) {
  const actions = {
    submit: { title: 'Xác nhận đơn hàng?', message: `Đơn nháp “${orderNumber}” sẽ chuyển sang Chờ xử lý và không còn được sửa thông tin.`, button: 'Xác nhận đơn' },
    cancel: { title: 'Hủy đơn hàng?', message: `Đơn “${orderNumber}” sẽ chuyển sang Đã hủy và được giữ lại trong lịch sử.`, button: 'Hủy đơn' },
    dispatch: { title: 'Bắt đầu giao hàng?', message: `Đơn “${orderNumber}” sẽ chuyển sang trạng thái Đang giao.`, button: 'Bắt đầu giao' },
    complete: { title: 'Xác nhận đã giao?', message: `Đơn “${orderNumber}” sẽ được hoàn tất và ghi nhận xuất kho.`, button: 'Xác nhận đã giao' },
    delete: { title: 'Xóa đơn hàng?', message: `Đơn “${orderNumber}” và toàn bộ sản phẩm bên trong sẽ bị xóa.`, button: 'Xóa đơn' }
  };
  const config = actions[action];
  pendingConfirmation = { type: 'order', id, action };
  elements.confirmModalTitle.textContent = config.title;
  elements.confirmModalMessage.textContent = config.message;
  elements.confirmActionButton.textContent = config.button;
  elements.confirmActionButton.classList.toggle('btn-danger', ['delete', 'cancel'].includes(action));
  elements.confirmActionButton.classList.toggle('btn-brand', !['delete', 'cancel'].includes(action));
  elements.confirmModalError.classList.add('d-none');
  document.body.classList.add('modal-open');
  elements.confirmModal.classList.remove('d-none');
}

function showDataState(state) {
  elements.dataLoading.classList.toggle('d-none', state !== 'loading');
  elements.dataError.classList.toggle('d-none', state !== 'error');
  elements.dataEmpty.classList.toggle('d-none', state !== 'empty');
  elements.dataTableWrap.classList.toggle('d-none', state !== 'table');
  elements.tableFooter.classList.toggle('d-none', state !== 'table');
}

function closeSidebar() {
  elements.adminSidebar.classList.remove('open');
  elements.sidebarBackdrop.classList.remove('show');
}

function configureManagementSection(section) {
  const config = managementSections[section];
  managementState = { section, page: 1, totalPages: 1, search: '', categoryStatus: '', warehouseStatus: '', categoryId: '', stockStatus: '', orderStatus: '', fromDate: '', toDate: '' };
  elements.topbarTitle.textContent = config.title;
  elements.sectionTitle.textContent = config.heading;
  elements.sectionDescription.textContent = config.description;
  elements.managementSearch.value = '';
  elements.managementSearch.placeholder = config.placeholder;
  elements.categoryFilters.classList.toggle('d-none', section !== 'categories');
  elements.warehouseFilters.classList.toggle('d-none', section !== 'warehouses');
  elements.productFilters.classList.toggle('d-none', section !== 'products');
  elements.orderFilters.classList.toggle('d-none', section !== 'orders');
  elements.primaryActionButton.classList.toggle('d-none', section === 'users');
  if (section !== 'users') {
    elements.primaryActionButton.querySelector('span').textContent = section === 'categories'
      ? 'Thêm danh mục'
      : section === 'products' ? 'Thêm sản phẩm'
        : section === 'warehouses' ? 'Thêm kho' : 'Thêm đơn hàng';
  }
  elements.categoryStatusFilter.value = '';
  elements.warehouseStatusFilter.value = '';
  elements.categoryFilter.value = '';
  elements.stockFilter.value = '';
  elements.orderStatusFilter.value = '';
  elements.orderFromDate.value = '';
  elements.orderToDate.value = '';
  elements.navItems.forEach(item => item.classList.toggle('active', item.dataset.section === section));
  closeSidebar();
  if (section === 'products') refreshCategoryOptions().catch(error => showToast(error.message, 'error'));
  if (section === 'orders') getOrderFormOptions().catch(error => showToast(error.message, 'error'));
  loadManagementData();
}

function renderCategories(items) {
  const roots = items.filter(item => !item.parentCategoryId);
  const orderedItems = roots.flatMap(root => [
    root,
    ...items.filter(item => item.parentCategoryId === root.categoryId)
  ]);
  const knownIds = new Set(orderedItems.map(item => item.categoryId));
  orderedItems.push(...items.filter(item => !knownIds.has(item.categoryId)));
  elements.dataTableHead.innerHTML = '<th>Mã danh mục</th><th>Tên danh mục</th><th>Loại danh mục</th><th>Mô tả</th><th>Trạng thái</th><th class="text-end">Thao tác</th>';
  elements.dataTableBody.innerHTML = orderedItems.map(item => `
    <tr>
      <td><span class="id-badge">${escapeHtml(item.code || formatEntityCode('DM', item.categoryId))}</span></td>
      <td><span class="table-primary-text ${item.parentCategoryId ? 'category-child' : ''}">${item.parentCategoryId ? '<i class="bi bi-arrow-return-right"></i>' : '<i class="bi bi-folder2"></i>'}${escapeHtml(item.name)}</span></td>
      <td>${escapeHtml(item.parentCategoryName || 'Danh mục gốc')}</td>
      <td>${escapeHtml(item.description || '—')}</td>
      <td>${statusBadge(item.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động', item.isActive ? 'active' : 'inactive')}</td>
      <td><div class="table-actions"><button class="table-action" type="button" data-action="edit" data-entity="category" data-id="${item.categoryId}" title="Sửa danh mục"><i class="bi bi-pencil"></i></button><button class="table-action table-action-danger" type="button" data-action="delete" data-entity="category" data-id="${item.categoryId}" data-name="${escapeHtml(item.name)}" title="Xóa danh mục"><i class="bi bi-trash"></i></button></div></td>
    </tr>`).join('');
}

function renderWarehouses(items) {
  elements.dataTableHead.innerHTML = '<th>Mã kho</th><th>Tên kho</th><th>Địa chỉ</th><th>Trạng thái</th><th class="text-end">Thao tác</th>';
  elements.dataTableBody.innerHTML = items.map(item => `
    <tr>
      <td><span class="id-badge">${formatEntityCode('KHO', item.warehouseId)}</span></td>
      <td><span class="table-primary-text"><i class="bi bi-building"></i>${escapeHtml(item.name)}</span></td>
      <td>${escapeHtml(item.address || '—')}</td>
      <td>${statusBadge(item.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động', item.isActive ? 'active' : 'inactive')}</td>
      <td><div class="table-actions"><button class="table-action" type="button" data-action="edit" data-entity="warehouse" data-id="${item.warehouseId}" title="Sửa kho"><i class="bi bi-pencil"></i></button>${item.isActive ? `<button class="table-action table-action-danger" type="button" data-action="delete" data-entity="warehouse" data-id="${item.warehouseId}" data-name="${escapeHtml(item.name)}" title="Ngừng hoạt động"><i class="bi bi-power"></i></button>` : ''}</div></td>
    </tr>`).join('');
}

function renderProducts(items) {
  elements.dataTableHead.innerHTML = '<th>Mã hệ thống</th><th>SKU</th><th>Sản phẩm</th><th>Danh mục</th><th>Đơn vị</th><th>Giá bán</th><th>Tồn kho</th><th>Trạng thái</th><th class="text-end">Thao tác</th>';
  elements.dataTableBody.innerHTML = items.map(item => {
    const stockStyle = item.stockStatus === 'Available' ? 'active' : item.stockStatus === 'OutOfStock' ? 'inactive' : 'warning';
    const stockLabel = item.stockStatus === 'Available' ? 'Còn hàng' : item.stockStatus === 'OutOfStock' ? 'Hết hàng' : 'Sắp hết';
    return `<tr>
      <td><span class="id-badge">${formatEntityCode('SP', item.productId)}</span></td>
      <td><span class="table-primary-text">${escapeHtml(item.sku)}</span></td>
      <td><div class="product-cell"><div class="product-thumb">${item.imageUrl ? `<img src="${escapeHtml(item.imageUrl)}" alt="">` : '<i class="bi bi-image"></i>'}</div><div><span class="table-primary-text">${escapeHtml(item.name)}</span><span class="table-secondary-text">Tối thiểu: ${item.minimumStockLevel}</span></div></div></td>
      <td>${escapeHtml(item.categoryName)}</td>
      <td>${escapeHtml(item.unit)}</td>
      <td>${formatMoney(item.salePrice)}</td>
      <td>${Number(item.quantityOnHand).toLocaleString('vi-VN')}</td>
      <td>${statusBadge(item.isActive ? stockLabel : 'Ngừng kinh doanh', item.isActive ? stockStyle : 'inactive')}</td>
      <td><div class="table-actions"><button class="table-action" type="button" data-action="edit" data-entity="product" data-id="${item.productId}" title="Sửa sản phẩm"><i class="bi bi-pencil"></i></button><button class="table-action table-action-danger" type="button" data-action="delete" data-entity="product" data-id="${item.productId}" data-name="${escapeHtml(item.name)}" title="Ngừng kinh doanh"><i class="bi bi-trash"></i></button></div></td>
    </tr>`;
  }).join('');
}

function renderOrderActions(item) {
  const buttons = [
    `<button class="table-action" type="button" data-action="view-order" data-id="${item.salesOrderId}" title="Xem chi tiết"><i class="bi bi-eye"></i></button>`
  ];
  if (item.status === 'Draft') {
    buttons.push(`<button class="table-action" type="button" data-action="edit-order" data-id="${item.salesOrderId}" title="Sửa đơn hàng"><i class="bi bi-pencil"></i></button>`);
    buttons.push(`<button class="table-action table-action-primary" type="button" data-action="order-transition" data-transition="submit" data-id="${item.salesOrderId}" data-name="${escapeHtml(item.orderNumber)}" title="Xác nhận đơn"><i class="bi bi-send-check"></i></button>`);
    buttons.push(`<button class="table-action table-action-danger" type="button" data-action="order-transition" data-transition="delete" data-id="${item.salesOrderId}" data-name="${escapeHtml(item.orderNumber)}" title="Xóa đơn hàng"><i class="bi bi-trash"></i></button>`);
  }
  if (item.status === 'Pending') {
    buttons.push(`<button class="table-action table-action-primary" type="button" data-action="order-transition" data-transition="dispatch" data-id="${item.salesOrderId}" data-name="${escapeHtml(item.orderNumber)}" title="Bắt đầu giao"><i class="bi bi-truck"></i></button>`);
  }
  if (item.status === 'Delivering') {
    buttons.push(`<button class="table-action table-action-primary" type="button" data-action="order-transition" data-transition="complete" data-id="${item.salesOrderId}" data-name="${escapeHtml(item.orderNumber)}" title="Xác nhận đã giao"><i class="bi bi-check2-circle"></i></button>`);
  }
  if (['Draft', 'Pending', 'Delivering'].includes(item.status)) {
    buttons.push(`<button class="table-action table-action-danger" type="button" data-action="order-transition" data-transition="cancel" data-id="${item.salesOrderId}" data-name="${escapeHtml(item.orderNumber)}" title="Hủy đơn"><i class="bi bi-x-circle"></i></button>`);
  }
  return `<div class="table-actions">${buttons.join('')}</div>`;
}

function renderOrders(items) {
  elements.dataTableHead.innerHTML = '<th>Mã đơn</th><th>Khách hàng</th><th>Ngày đặt</th><th>Giao hàng</th><th>Tổng tiền</th><th>Trạng thái</th><th class="text-end">Thao tác</th>';
  elements.dataTableBody.innerHTML = items.map(item => `
    <tr>
      <td><span class="table-primary-text">${escapeHtml(item.orderNumber)}</span><span class="table-secondary-text">${formatEntityCode('DH', item.salesOrderId)}</span></td>
      <td><span class="table-primary-text">${escapeHtml(item.customerName || 'Khách lẻ')}</span><span class="table-secondary-text">${escapeHtml(item.customerPhone || 'Không có số điện thoại')}</span></td>
      <td>${formatDate(item.orderDate)}</td>
      <td><span class="order-address" title="${escapeHtml(item.shippingAddress || '')}">${escapeHtml(item.shippingAddress || 'Nhận tại cửa hàng')}</span></td>
      <td><strong class="order-amount">${formatMoney(item.totalAmount)}</strong></td>
      <td>${orderStatusBadge(item.status)}</td>
      <td>${renderOrderActions(item)}</td>
    </tr>`).join('');
}

function renderUsers(items) {
  elements.dataTableHead.innerHTML = '<th>Người dùng</th><th>Email</th><th>Số điện thoại</th><th>Vai trò</th><th>Trạng thái</th><th>Ngày tạo</th>';
  elements.dataTableBody.innerHTML = items.map(item => `
    <tr>
      <td><span class="table-primary-text">${escapeHtml(item.fullName)}</span></td>
      <td>${escapeHtml(item.email)}</td>
      <td>${escapeHtml(item.phoneNumber || '—')}</td>
      <td>${statusBadge(roleLabels[item.roleName] || item.roleName, 'info')}</td>
      <td>${statusBadge(item.status === 'Active' ? 'Đang hoạt động' : 'Đã khóa', item.status === 'Active' ? 'active' : 'inactive')}</td>
      <td>${formatDate(item.createdAt)}</td>
    </tr>`).join('');
}

async function loadManagementData() {
  const role = session?.user?.role;
  const canAccessSection = role === 'Admin'
    || (role === 'SalesStaff' && managementState.section === 'orders')
    || (role === 'WarehouseManager' && managementState.section === 'warehouses');
  if (!canAccessSection) return;
  showDataState('loading');
  elements.refreshDataButton.disabled = true;
  try {
    const query = new URLSearchParams();
    if (managementState.search) query.set('search', managementState.search);
    if (managementState.categoryId) query.set('categoryId', managementState.categoryId);
    if (managementState.stockStatus) query.set('stockStatus', managementState.stockStatus);
    if (managementState.orderStatus) query.set('status', managementState.orderStatus);
    if (managementState.section === 'warehouses' && managementState.warehouseStatus) {
      query.set('isActive', managementState.warehouseStatus);
    }
    if (managementState.fromDate) query.set('fromDate', managementState.fromDate);
    if (managementState.toDate) query.set('toDate', managementState.toDate);
    query.set('page', String(managementState.page));
    query.set('pageSize', '10');

    let response;
    let items;
    let totalItems;
    if (managementState.section === 'categories') {
      response = await api('/api/categories');
      categoryCache = response;
      const keyword = managementState.search.toLocaleLowerCase('vi');
      items = response.filter(item => {
        const matchesKeyword = !keyword
          || `${item.code || ''} ${item.name} ${item.description || ''}`.toLocaleLowerCase('vi').includes(keyword);
        const matchesStatus = !managementState.categoryStatus
          || (managementState.categoryStatus === 'active' ? item.isActive : !item.isActive);
        return matchesKeyword && matchesStatus;
      });
      totalItems = items.length;
      managementState.totalPages = 1;
      renderCategories(items);
    } else {
      const resource = managementState.section === 'orders' ? 'sales' : managementState.section;
      response = await api(`/api/${resource}?${query}`);
      items = response.items;
      totalItems = response.totalItems;
      managementState.totalPages = Math.max(1, response.totalPages);
      if (managementState.section === 'products') renderProducts(items);
      else if (managementState.section === 'warehouses') renderWarehouses(items);
      else if (managementState.section === 'orders') renderOrders(items);
      else renderUsers(items);
    }

    elements.resultCount.textContent = `${totalItems.toLocaleString('vi-VN')} kết quả`;
    elements.pageInfo.textContent = managementState.section === 'categories'
      ? `Hiển thị ${totalItems} danh mục`
      : `Trang ${managementState.page}/${managementState.totalPages} · ${totalItems.toLocaleString('vi-VN')} kết quả`;
    elements.previousPageButton.disabled = managementState.page <= 1;
    elements.nextPageButton.disabled = managementState.page >= managementState.totalPages;
    showDataState(items.length ? 'table' : 'empty');
  } catch (error) {
    elements.dataError.querySelector('span').textContent = error.status === 403
      ? 'Tài khoản không có quyền truy cập dữ liệu này.'
      : `Không thể tải dữ liệu. ${error.message}`;
    elements.resultCount.textContent = '0 kết quả';
    showDataState('error');
  } finally {
    elements.refreshDataButton.disabled = false;
  }
}

function collectOrderItems() {
  const rows = [...elements.orderItemsBody.querySelectorAll('tr')];
  if (!rows.length) throw new Error('Đơn hàng cần có ít nhất một sản phẩm.');
  const productIds = new Set();
  return rows.map(row => {
    const productId = Number(row.querySelector('.order-item-product').value);
    const quantity = Number(row.querySelector('.order-item-quantity').value);
    const unitPrice = Number(row.querySelector('.order-item-price').value);
    const discount = Number(row.querySelector('.order-item-discount').value);
    if (productIds.has(productId)) throw new Error('Mỗi sản phẩm chỉ được thêm một lần trong đơn hàng.');
    if (discount > quantity * unitPrice) throw new Error('Giảm giá không được lớn hơn thành tiền của sản phẩm.');
    productIds.add(productId);
    return {
      row,
      itemId: Number(row.dataset.itemId) || null,
      payload: { productId, quantity, unitPrice, discount }
    };
  });
}

async function saveOrder(id, payload, items) {
  let order = await api(id ? `/api/sales/${id}` : '/api/sales', {
    method: id ? 'PUT' : 'POST',
    body: JSON.stringify(payload)
  });
  const orderId = order.salesOrderId;
  elements.entityId.value = String(orderId);

  const retainedIds = new Set(items.filter(item => item.itemId).map(item => item.itemId));
  for (const existing of (editingOrder?.items || [])) {
    if (!retainedIds.has(existing.salesOrderItemId)) {
      await api(`/api/sales/${orderId}/items/${existing.salesOrderItemId}`, { method: 'DELETE' });
    }
  }
  for (const item of items) {
    order = await api(item.itemId
      ? `/api/sales/${orderId}/items/${item.itemId}`
      : `/api/sales/${orderId}/items`, {
      method: item.itemId ? 'PUT' : 'POST',
      body: JSON.stringify(item.payload)
    });
    if (!item.itemId) {
      const created = order.items.find(candidate => candidate.productId === item.payload.productId);
      if (created) item.row.dataset.itemId = String(created.salesOrderItemId);
    }
  }
  return order;
}

async function saveEntity(event) {
  event.preventDefault();
  elements.entityForm.classList.add('was-validated');
  elements.entityFormMessage.classList.add('d-none');
  if (!elements.entityForm.checkValidity()) return;

  const type = elements.entityForm.dataset.entityType;
  const id = Number(elements.entityId.value) || null;
  let payload;
  if (type === 'category') {
    payload = {
      name: elements.categoryName.value.trim(),
      parentCategoryId: elements.categoryParent.value ? Number(elements.categoryParent.value) : null,
      description: elements.categoryDescription.value.trim() || null,
      isActive: elements.categoryActive.value === 'true'
    };
  } else if (type === 'product') {
    payload = {
      sku: elements.productSku.value.trim(),
      name: elements.productName.value.trim(),
      categoryId: Number(elements.productCategory.value),
      unit: elements.productUnit.value,
      description: elements.productDescription.value.trim() || null,
      imageUrl: elements.productImageUrl.value.trim() || null,
      salePrice: Number(elements.productPrice.value),
      minimumStockLevel: Number(elements.productMinimumStock.value),
      isActive: elements.productActive.checked
    };
    if (id) payload.rowVersion = editingProductRowVersion;
  } else if (type === 'warehouse') {
    payload = {
      name: elements.warehouseName.value.trim(),
      address: elements.warehouseAddress.value.trim() || null,
      isActive: elements.warehouseActive.value === 'true'
    };
  } else {
    payload = {
      orderNumber: elements.orderNumber.value.trim(),
      warehouseId: Number(elements.orderWarehouse.value),
      customerId: elements.orderCustomer.value ? Number(elements.orderCustomer.value) : null,
      orderDate: `${elements.orderDate.value}T00:00:00`,
      shippingAddress: elements.orderShippingAddress.value.trim() || null,
      notes: elements.orderNotes.value.trim() || null
    };
  }

  setButtonBusy(elements.saveEntityButton, true, 'Đang lưu...');
  try {
    if (type === 'product' && elements.productImageFile.files[0]) {
      const upload = await uploadProductImage(elements.productImageFile.files[0]);
      payload.imageUrl = upload.imageUrl;
      elements.productImageUrl.value = upload.imageUrl;
    }
    if (type === 'order') {
      const items = collectOrderItems();
      await saveOrder(id, payload, items);
    } else {
      const endpoint = type === 'category' ? '/api/categories'
        : type === 'product' ? '/api/products' : '/api/warehouses';
      await api(id ? `${endpoint}/${id}` : endpoint, {
        method: id ? 'PUT' : 'POST',
        body: JSON.stringify(payload)
      });
    }
    if (type === 'category') {
      categoryCache = null;
      await refreshCategoryOptions(true);
    }
    if (type === 'warehouse') warehouseCache = null;
    closeEntityModal();
    const entityLabel = type === 'category' ? 'danh mục'
      : type === 'product' ? 'sản phẩm'
        : type === 'warehouse' ? 'kho' : 'đơn hàng';
    showToast(`${id ? 'Cập nhật' : 'Thêm'} ${entityLabel} thành công.`);
    await loadManagementData();
  } catch (error) {
    elements.entityFormMessage.textContent = error.message;
    elements.entityFormMessage.classList.remove('d-none');
  } finally {
    setButtonBusy(elements.saveEntityButton, false);
  }
}

async function executeConfirmedAction() {
  if (!pendingConfirmation) return;
  const { type, id, action } = pendingConfirmation;
  setButtonBusy(elements.confirmActionButton, true, 'Đang xử lý...');
  try {
    if (type === 'order') {
      await api(action === 'delete' ? `/api/sales/${id}` : `/api/sales/${id}/${action}`, {
        method: action === 'delete' ? 'DELETE' : 'POST'
      });
    } else {
      const endpoint = type === 'category' ? '/api/categories'
        : type === 'product' ? '/api/products' : '/api/warehouses';
      await api(`${endpoint}/${id}`, { method: 'DELETE' });
    }
    if (type === 'category') {
      categoryCache = null;
      await refreshCategoryOptions(true);
    }
    if (type === 'warehouse') warehouseCache = null;
    closeConfirmModal();
    const orderMessages = {
      submit: 'Đơn hàng đã chuyển sang Chờ xử lý.',
      cancel: 'Đơn hàng đã được hủy.',
      dispatch: 'Đơn hàng đã chuyển sang Đang giao.',
      complete: 'Đơn hàng đã được xác nhận giao thành công.',
      delete: 'Đã xóa đơn hàng.'
    };
    showToast(type === 'order' ? orderMessages[action]
      : type === 'category' ? 'Xóa danh mục thành công.'
        : type === 'warehouse' ? 'Kho đã ngừng hoạt động.' : 'Sản phẩm đã ngừng kinh doanh.');
    await loadManagementData();
  } catch (error) {
    elements.confirmModalError.textContent = error.message;
    elements.confirmModalError.classList.remove('d-none');
  } finally {
    setButtonBusy(elements.confirmActionButton, false);
  }
}

function initializeAdminWorkspace() {
  const role = session?.user?.role;
  const isAdmin = role === 'Admin';
  const canManageWarehouses = isAdmin || role === 'WarehouseManager';
  const canManageSales = isAdmin || role === 'SalesStaff';
  const hasManagementAccess = canManageWarehouses || canManageSales;
  document.querySelectorAll('[data-admin-only]').forEach(item => item.classList.toggle('d-none', !isAdmin));
  document.querySelectorAll('[data-warehouse-access]').forEach(item => item.classList.toggle('d-none', !canManageWarehouses));
  document.querySelectorAll('[data-sales-access]').forEach(item => item.classList.toggle('d-none', !canManageSales));
  elements.adminPanel.classList.toggle('d-none', !hasManagementAccess);
  elements.accessDeniedPanel.classList.toggle('d-none', hasManagementAccess);
  if (hasManagementAccess) {
    const initialSection = isAdmin ? 'categories' : role === 'WarehouseManager' ? 'warehouses' : 'orders';
    configureManagementSection(initialSection);
  }
}

function applyLoginResponse(response, rememberMe = session?.rememberMe ?? false) {
  const now = Date.now();
  session = {
    accessToken: response.accessToken,
    expiresAt: new Date(response.expiresAt).getTime(),
    user: response.user,
    rememberMe,
    lastActivityAt: now,
    lastRefreshAt: now
  };
  saveSession();
}

async function handleLogin(event) {
  event.preventDefault();
  hideMessage();
  updateFieldValidationMessages();
  if (!elements.loginForm.checkValidity()) {
    elements.loginForm.classList.add('was-validated');
    return;
  }

  setButtonBusy(elements.loginButton, true);
  try {
    const response = await api('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({
        email: elements.email.value.trim(),
        password: elements.password.value
      })
    });
    applyLoginResponse(response, elements.rememberMe.checked);
    elements.loginForm.reset();
    elements.loginForm.classList.remove('was-validated');
    clearFieldValidationMessages();
    showAuthenticatedView();
  } catch (error) {
    showMessage(error.status === 401 ? 'Email hoặc mật khẩu không chính xác.' : error.message);
  } finally {
    setButtonBusy(elements.loginButton, false);
  }
}

function showAuthenticatedView() {
  if (!session?.user) return showLoginView();
  elements.loginView.classList.add('d-none');
  elements.sessionView.classList.remove('d-none');
  elements.userEmail.textContent = session.user.email;
  elements.userRole.textContent = roleLabels[session.user.role] || session.user.role;
  initializeAdminWorkspace();
  startSessionMonitoring();
}

function showLoginView(message = '', type = 'danger') {
  stopSessionMonitoring();
  elements.warning.classList.add('d-none');
  closeEntityModal();
  closeConfirmModal();
  elements.sessionView.classList.add('d-none');
  closeSidebar();
  elements.loginView.classList.remove('d-none');
  if (message) showMessage(message, type); else hideMessage();
  requestAnimationFrame(() => elements.email.focus());
}

function startSessionMonitoring() {
  stopSessionMonitoring();
  checkSessionTime();
  countdownTimer = window.setInterval(checkSessionTime, 1000);
  scheduleIdleLogout();
}

function stopSessionMonitoring() {
  window.clearTimeout(idleTimer);
  window.clearInterval(countdownTimer);
  idleTimer = null;
  countdownTimer = null;
}

function scheduleIdleLogout() {
  window.clearTimeout(idleTimer);
  if (!session) return;
  const remaining = Math.max(0, IDLE_TIMEOUT_MS - (Date.now() - session.lastActivityAt));
  idleTimer = window.setTimeout(() => logout('idle'), remaining);
}

function checkSessionTime() {
  if (!session) return;
  const remaining = IDLE_TIMEOUT_MS - (Date.now() - session.lastActivityAt);
  if (remaining <= 0) {
    logout('idle');
    return;
  }

    const seconds = Math.ceil(remaining / 1000);
  const minutesPart = Math.floor(seconds / 60);
  const secondsPart = seconds % 60;

  if (elements.idleCountdown) {
    elements.idleCountdown.textContent =
      `${String(minutesPart).padStart(2, '0')}:${String(secondsPart).padStart(2, '0')}`;
  }

  if (elements.sessionProgress) {
    elements.sessionProgress.style.width =
      `${Math.max(0, Math.min(100, remaining / IDLE_TIMEOUT_MS * 100))}%`;

    elements.sessionProgress.classList.toggle(
      'bg-danger',
      remaining <= WARNING_BEFORE_MS
    );
  }
  elements.warningCountdown.textContent = String(seconds);
  elements.warning.classList.toggle('d-none', remaining > WARNING_BEFORE_MS);

  if (session.expiresAt <= Date.now()) logout('expired');
}

function registerActivity() {
  if (!session || elements.sessionView.classList.contains('d-none')) return;
  const now = Date.now();
  session.lastActivityAt = now;
  elements.warning.classList.add('d-none');
  scheduleIdleLogout();

  if (now - activityWriteAt >= 1000) {
    activityWriteAt = now;
    saveSession();
  }

  if (now - session.lastRefreshAt >= REFRESH_INTERVAL_MS) refreshSession();
}

async function refreshSession() {
  if (!session || refreshInFlight) return;
  refreshInFlight = true;
  try {
    const response = await api('/api/auth/refresh', { method: 'POST' });
    const lastActivityAt = session.lastActivityAt;
    applyLoginResponse(response);
    session.lastActivityAt = lastActivityAt;
    saveSession();
  } catch (error) {
    if (error.status === 401) await logout('expired', false);
  } finally {
    refreshInFlight = false;
  }
}

async function logout(reason = 'manual', notifyServer = true) {
  if (!session) return showLoginView();
  const token = session.accessToken;
  session = null;
  saveSession();
  stopSessionMonitoring();

  if (notifyServer && token) {
    fetch(`${API_BASE}/api/auth/logout`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      cache: 'no-store'
    }).catch(() => {});
  }

  const messages = {
    expired: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
    manual: 'Bạn đã đăng xuất thành công. Vui lòng đăng nhập để sử dụng.'
  };
  showLoginView(messages[reason] || messages.manual, reason === 'manual' ? 'success' : 'danger');
}

async function restoreSession() {
  if (!session?.accessToken || !session?.user || !session?.lastActivityAt) {
    session = null;
    saveSession();
    showLoginView();
    return;
  }
  if (Date.now() - session.lastActivityAt >= IDLE_TIMEOUT_MS || session.expiresAt <= Date.now()) {
    await logout('expired', false);
    return;
  }

  try {
    await api('/api/auth/me');
    showAuthenticatedView();
  } catch {
    session = null;
    saveSession();
    showLoginView('Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.');
  }
}

elements.loginForm.addEventListener('submit', handleLogin);
elements.loginForm.addEventListener('input', () => {
  if (elements.loginForm.classList.contains('was-validated')) updateFieldValidationMessages();
});
elements.togglePassword.addEventListener('click', () => {
  const visible = elements.password.type === 'text';
  elements.password.type = visible ? 'password' : 'text';
  elements.togglePassword.innerHTML = `<i class="bi bi-eye${visible ? '' : '-slash'}"></i>`;
  elements.togglePassword.setAttribute('aria-label', visible ? 'Hiện mật khẩu' : 'Ẩn mật khẩu');
});
elements.logoutButton.addEventListener('click', () => logout('manual'));
elements.continueButton.addEventListener('click', registerActivity);
elements.sidebarToggle.addEventListener('click', () => {
  elements.adminSidebar.classList.toggle('open');
  elements.sidebarBackdrop.classList.toggle('show');
});
elements.sidebarBackdrop.addEventListener('click', closeSidebar);
elements.navItems.forEach(item => item.addEventListener('click', () => configureManagementSection(item.dataset.section)));
elements.refreshDataButton.addEventListener('click', loadManagementData);
elements.retryDataButton.addEventListener('click', loadManagementData);
elements.managementSearch.addEventListener('input', () => {
  window.clearTimeout(searchTimer);
  searchTimer = window.setTimeout(() => {
    managementState.search = elements.managementSearch.value.trim();
    managementState.page = 1;
    loadManagementData();
  }, 350);
});
elements.previousPageButton.addEventListener('click', () => {
  if (managementState.page <= 1) return;
  managementState.page -= 1;
  loadManagementData();
});
elements.nextPageButton.addEventListener('click', () => {
  if (managementState.page >= managementState.totalPages) return;
  managementState.page += 1;
  loadManagementData();
});
elements.primaryActionButton.addEventListener('click', async () => {
  try {
    const entityType = managementState.section === 'products' ? 'product'
      : managementState.section === 'orders' ? 'order'
        : managementState.section === 'warehouses' ? 'warehouse' : 'category';
    await openEntityModal(entityType);
  } catch (error) {
    showToast(`Không thể mở biểu mẫu. ${error.message}`, 'error');
  }
});
elements.categoryStatusFilter.addEventListener('change', () => {
  managementState.categoryStatus = elements.categoryStatusFilter.value;
  managementState.page = 1;
  loadManagementData();
});
elements.warehouseStatusFilter.addEventListener('change', () => {
  managementState.warehouseStatus = elements.warehouseStatusFilter.value;
  managementState.page = 1;
  loadManagementData();
});
elements.categoryFilter.addEventListener('change', () => {
  managementState.categoryId = elements.categoryFilter.value;
  managementState.page = 1;
  loadManagementData();
});
elements.stockFilter.addEventListener('change', () => {
  managementState.stockStatus = elements.stockFilter.value;
  managementState.page = 1;
  loadManagementData();
});
elements.orderStatusFilter.addEventListener('change', () => {
  managementState.orderStatus = elements.orderStatusFilter.value;
  managementState.page = 1;
  loadManagementData();
});
[elements.orderFromDate, elements.orderToDate].forEach(input => input.addEventListener('change', () => {
  managementState.fromDate = elements.orderFromDate.value;
  managementState.toDate = elements.orderToDate.value;
  managementState.page = 1;
  loadManagementData();
}));
elements.dataTableBody.addEventListener('click', event => {
  const button = event.target.closest('[data-action]');
  if (!button) return;
  const id = Number(button.dataset.id);
  if (button.dataset.action === 'edit') openEntityModal(button.dataset.entity, id);
  if (button.dataset.action === 'delete') openDeleteConfirmation(button.dataset.entity, id, button.dataset.name);
  if (button.dataset.action === 'view-order') openEntityModal('order', id, true);
  if (button.dataset.action === 'edit-order') openEntityModal('order', id);
  if (button.dataset.action === 'order-transition') openOrderConfirmation(id, button.dataset.name, button.dataset.transition);
});
elements.entityForm.addEventListener('submit', saveEntity);
elements.closeEntityModal.addEventListener('click', closeEntityModal);
elements.cancelEntityModal.addEventListener('click', closeEntityModal);
elements.productImageFile.addEventListener('change', updateImagePreview);
elements.addOrderItemButton.addEventListener('click', () => addOrderItemRow());
elements.orderItemsBody.addEventListener('change', event => {
  if (event.target.classList.contains('order-item-product')) {
    const row = event.target.closest('tr');
    const selected = event.target.selectedOptions[0];
    row.querySelector('.order-item-price').value = selected?.dataset.price || '';
  }
  updateOrderTotal();
});
elements.orderItemsBody.addEventListener('input', updateOrderTotal);
elements.orderItemsBody.addEventListener('click', event => {
  const button = event.target.closest('.remove-order-item');
  if (!button) return;
  button.closest('tr').remove();
  updateOrderTotal();
});
elements.orderCustomer.addEventListener('change', () => {
  const customer = customerCache?.find(item => item.customerId === Number(elements.orderCustomer.value));
  if (customer?.address) elements.orderShippingAddress.value = customer.address;
});
elements.cancelConfirmButton.addEventListener('click', closeConfirmModal);
elements.confirmActionButton.addEventListener('click', executeConfirmedAction);
elements.entityModal.addEventListener('click', event => {
  if (event.target === elements.entityModal) closeEntityModal();
});
elements.confirmModal.addEventListener('click', event => {
  if (event.target === elements.confirmModal) closeConfirmModal();
});
document.addEventListener('keydown', event => {
  if (event.key !== 'Escape') return;
  if (!elements.confirmModal.classList.contains('d-none')) closeConfirmModal();
  else if (!elements.entityModal.classList.contains('d-none')) closeEntityModal();
});

['pointerdown', 'keydown', 'scroll', 'touchstart'].forEach(eventName =>
  window.addEventListener(eventName, registerActivity, { passive: true }));
document.addEventListener('visibilitychange', () => {
  if (!document.hidden && session) checkSessionTime();
});

restoreSession();
