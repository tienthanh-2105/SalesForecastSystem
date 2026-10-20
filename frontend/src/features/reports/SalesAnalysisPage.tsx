import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { allPages, api } from "../../lib/api";
import { localDate, money } from "../../lib/format";
import type { Category, Product as CatalogProduct, Warehouse } from "../../types";

interface Product { productId: number; sku: string; productName: string; orderCount: number; quantitySold: number; revenue: number }
interface Staff { userId: number; fullName: string; orderCount: number; quantitySold: number; revenue: number }
interface Group { groupName: string; revenue: number; quantitySold: number; orderCount: number; members: Staff[] }

export function SalesAnalysisPage() {
  const [from, setFrom] = useState(localDate().slice(0, 7) + "-01");
  const [to, setTo] = useState(localDate());
  const [warehouse, setWarehouse] = useState("");
  const [product, setProduct] = useState("");
  const [category, setCategory] = useState("");
  const [filterQuery, setFilterQuery] = useState(`FromDate=${from}&ToDate=${to}`);
  const [topQuery, setTopQuery] = useState(`FromDate=${from}&ToDate=${to}&Top=10`);
  const [staffQuery, setStaffQuery] = useState(`FromDate=${from}&ToDate=${to}&Top=100`);
  const [groupName, setGroupName] = useState("Nhóm bán hàng");
  const [selectedStaffIds, setSelectedStaffIds] = useState<string[]>([]);
  const [staffSearch, setStaffSearch] = useState("");
  const [groupQuery, setGroupQuery] = useState<string | null>(null);
  const [error, setError] = useState("");

  const warehouses = useQuery({ queryKey: ["analysis-warehouses"], queryFn: () => allPages<Warehouse>("/api/warehouses?isActive=true") });
  const categories = useQuery({ queryKey: ["analysis-categories"], queryFn: () => api<Category[]>("/api/categories") });
  const catalogProducts = useQuery({ queryKey: ["analysis-products"], queryFn: () => allPages<CatalogProduct>("/api/products?isActive=true&sortBy=Name&sortDirection=Asc") });
  const products = useQuery({ queryKey: ["analysis-top-products", topQuery], queryFn: ({ signal }) => api<Product[]>(`/api/reports/top-products?${topQuery}`, { signal }) });
  const staff = useQuery({ queryKey: ["analysis-staff", staffQuery], queryFn: ({ signal }) => api<Staff[]>(`/api/reports/sales-by-staff?${staffQuery}`, { signal }) });
  const group = useQuery({ queryKey: ["analysis-staff-group", groupQuery], queryFn: ({ signal }) => api<Group>(`/api/reports/sales-by-group?${groupQuery}`, { signal }), enabled: groupQuery !== null });
  const staffDirectory = useQuery({ queryKey: ["analysis-staff-directory"], queryFn: () => api<Staff[]>("/api/reports/sales-by-staff?Top=100") });

  function applyFilters() {
    setError("");
    if (!from || !to || from > to || (Date.parse(to) - Date.parse(from)) / 86400000 > 366) { setError("Chọn khoảng ngày hợp lệ, tối đa 366 ngày."); return; }
    const query = new URLSearchParams({ FromDate: from, ToDate: to });
    for (const [key, value] of [["WarehouseId", warehouse], ["ProductId", product], ["CategoryId", category]]) if (value) query.set(key, value);
    setFilterQuery(query.toString());
    const top = new URLSearchParams(query); top.set("Top", "10"); setTopQuery(top.toString());
    const ranking = new URLSearchParams(query); ranking.set("Top", "100"); setStaffQuery(ranking.toString());
  }

  function applyGroup() {
    setError("");
    if (!groupName.trim() || groupName.trim().length > 100) { setError("Tên nhóm bắt buộc và tối đa 100 ký tự."); return; }
    const ids = [...new Set(selectedStaffIds)];
    if (!ids.length || ids.length > 100) { setError("Chọn từ 1 đến 100 nhân viên để phân tích nhóm."); return; }
    const query = new URLSearchParams(filterQuery);
    query.set("GroupName", groupName.trim()); ids.forEach(id => query.append("StaffUserIds", id));
    setGroupQuery(query.toString());
  }

  return <main className="analysis-page">
    <header className="analysis-heading"><div><h2>Phân tích bán hàng</h2><p>Top sản phẩm và hiệu suất nhân viên theo khoảng thời gian đã chọn.</p></div></header>
    <form className="report-filter-bar analysis-filter-bar" onSubmit={e => { e.preventDefault(); applyFilters(); }}>
      <div className="report-date-field"><label className="visually-hidden" htmlFor="analysis-from">Từ ngày</label><input id="analysis-from" type="date" className="form-control" required value={from} onChange={e => setFrom(e.target.value)} /><span>đến</span><label className="visually-hidden" htmlFor="analysis-to">Đến ngày</label><input id="analysis-to" type="date" className="form-control" required value={to} onChange={e => setTo(e.target.value)} /></div>
      <label className="visually-hidden" htmlFor="analysis-warehouse">Kho</label><select id="analysis-warehouse" aria-label="Kho" className="form-select report-filter-input" value={warehouse} onChange={e => setWarehouse(e.target.value)}><option value="">Tất cả kho</option>{(Array.isArray(warehouses.data) ? warehouses.data : []).map(w => <option key={w.warehouseId} value={w.warehouseId}>{w.name}</option>)}</select>
      <label className="visually-hidden" htmlFor="analysis-product">Sản phẩm</label><select id="analysis-product" aria-label="Sản phẩm" className="form-select report-filter-input" value={product} onChange={e => setProduct(e.target.value)}><option value="">Tất cả sản phẩm</option>{(Array.isArray(catalogProducts.data) ? catalogProducts.data : []).map(p => <option key={p.productId} value={p.productId}>{p.name} · {p.sku}</option>)}</select>
      <label className="visually-hidden" htmlFor="analysis-category">Danh mục</label><select id="analysis-category" aria-label="Danh mục" className="form-select report-filter-input" value={category} onChange={e => setCategory(e.target.value)}><option value="">Tất cả danh mục</option>{(Array.isArray(categories.data) ? categories.data : []).filter(c => c.isActive).map(c => <option key={c.categoryId} value={c.categoryId}>{c.name}</option>)}</select>
      <button className="btn btn-primary report-filter-button" aria-label="Lọc phân tích" title="Lọc phân tích" disabled={products.isFetching || staff.isFetching}><i className="bi bi-funnel-fill" aria-hidden="true" /></button>
    </form>
    {error && <div className="alert alert-danger m-3" role="alert">{error}</div>}
    <div className="analysis-grid">
      <section className="card report-card"><header className="report-card-heading"><div><h3>Top sản phẩm bán chạy</h3><p>Xếp theo doanh thu · tối đa 10 sản phẩm</p></div></header>
        {products.isFetching ? <p role="status">Đang tải sản phẩm...</p> : products.error ? <p className="text-danger" role="alert">{products.error.message}</p> : <div className="table-responsive"><table className="table report-ranking-table"><thead><tr><th>#</th><th>Sản phẩm</th><th>SL bán</th><th>Đơn</th><th>Doanh thu</th></tr></thead><tbody>{(Array.isArray(products.data) ? products.data : []).map((p, i) => <tr key={p.productId}><td>{i + 1}</td><td><strong>{p.productName}</strong><small>{p.sku}</small></td><td>{p.quantitySold.toLocaleString("vi-VN")}</td><td>{p.orderCount.toLocaleString("vi-VN")}</td><td>{money(p.revenue)}</td></tr>)}</tbody></table>{!products.data?.length && <p className="report-empty">Chưa có sản phẩm bán trong khoảng này.</p>}</div>}
      </section>
      <section className="card report-card"><header className="report-card-heading"><div><h3>Hiệu suất nhân viên</h3><p>Xếp theo doanh thu trong khoảng đã chọn</p></div></header>
        {staff.isFetching ? <p role="status">Đang tải hiệu suất...</p> : staff.error ? <p className="text-danger" role="alert">{staff.error.message}</p> : <div className="table-responsive"><table className="table report-ranking-table"><thead><tr><th>#</th><th>Nhân viên</th><th>Đơn</th><th>SL bán</th><th>Doanh thu</th></tr></thead><tbody>{(Array.isArray(staff.data) ? staff.data : []).map((s, i) => <tr key={s.userId}><td>{i + 1}</td><td>{s.fullName}</td><td>{s.orderCount.toLocaleString("vi-VN")}</td><td>{s.quantitySold.toLocaleString("vi-VN")}</td><td>{money(s.revenue)}</td></tr>)}</tbody></table>{!staff.data?.length && <p className="report-empty">Chưa có doanh số nhân viên trong khoảng này.</p>}</div>}
      </section>
    </div>
    <section className="card report-card analysis-group-card"><header className="report-card-heading"><div><h3>Phân tích doanh số theo nhóm nhân viên</h3><p>So sánh doanh thu của nhóm nhân viên được chọn.</p></div></header>
      <form className="analysis-group-form" onSubmit={e => { e.preventDefault(); applyGroup(); }}><div><label className="form-label" htmlFor="analysis-group-name">Tên nhóm</label><input id="analysis-group-name" className="form-control" required maxLength={100} value={groupName} onChange={e => setGroupName(e.target.value)} /></div>
        <div className="analysis-staff-picker"><span className="form-label">Nhân viên trong nhóm</span><details className="analysis-directory"><summary>{selectedStaffIds.length ? `Đã chọn ${selectedStaffIds.length} nhân viên` : "Chọn nhân viên theo tên"}</summary>
          {staffDirectory.isFetching ? <p role="status">Đang tải danh sách nhân viên...</p> : staffDirectory.error ? <p className="text-danger" role="alert">{staffDirectory.error.message}</p> : <>
            <label className="visually-hidden" htmlFor="analysis-staff-search">Tìm nhân viên</label><input id="analysis-staff-search" className="form-control form-control-sm" type="search" placeholder="Tìm theo tên" value={staffSearch} onChange={e => setStaffSearch(e.target.value)} />
            <div className="analysis-staff-options">{(staffDirectory.data ?? []).filter(s => s.fullName.toLocaleLowerCase("vi").includes(staffSearch.trim().toLocaleLowerCase("vi"))).map(s => <label className="analysis-staff-option" key={s.userId}><input type="checkbox" checked={selectedStaffIds.includes(String(s.userId))} onChange={e => setSelectedStaffIds(current => e.target.checked ? [...current, String(s.userId)] : current.filter(id => id !== String(s.userId)))} /><span>{s.fullName}</span></label>)}
              {!staffDirectory.data?.length && <p className="text-secondary mb-0">Chưa có nhân viên ghi nhận doanh số.</p>}
            </div>
          </>}
        </details></div><button className="btn btn-primary align-self-start" disabled={group.isFetching}>Xem doanh số nhóm</button>
      </form>
      {groupQuery && (group.isFetching ? <p role="status">Đang tải doanh số nhóm...</p> : group.error ? <div className="alert alert-danger mt-3" role="alert">{group.error.message}</div> : group.data && <div className="analysis-group-result"><h4>{group.data.groupName}</h4><p>{group.data.orderCount} đơn · {group.data.quantitySold} sản phẩm · {money(group.data.revenue)}</p><div className="table-responsive"><table className="table report-ranking-table"><thead><tr><th>Nhân viên</th><th>Đơn</th><th>Số lượng bán</th><th>Doanh thu</th></tr></thead><tbody>{group.data.members.map(s => <tr key={s.userId}><td>{s.fullName} ({s.userId})</td><td>{s.orderCount}</td><td>{s.quantitySold}</td><td>{money(s.revenue)}</td></tr>)}</tbody></table></div></div>)}
    </section>
  </main>;
}
