import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../../lib/api";
import { date, localDate, money } from "../../lib/format";
import { allPages } from "../../lib/api";
import { readSession } from "../auth/session";
import type { Category, Product as CatalogProduct, Warehouse } from "../../types";

interface Totals { orderCount: number; quantitySold: number; revenue: number; costOfGoodsSold: number | null; grossProfit: number | null; missingCostQuantity: number }
interface Profit { costMethod: string; totals: Totals; periods: { periodStart: string; totals: Totals }[] }
interface TrendPoint { periodStart: string; revenue: number; quantitySold: number; orderCount: number }

const chartColors = ["#2764c5", "#17a589", "#ef9b27", "#df5948", "#7856b8", "#1597ad", "#de6897", "#758495"];
const periodDate = (value: string, period: string) => new Date(value).toLocaleDateString("vi-VN", period === "Month" ? { month: "2-digit", year: "2-digit" } : { day: "2-digit", month: "2-digit" });

function RevenueCharts({ points, period }: { points: TrendPoint[]; period: string }) {
  const width = 940, height = 320, left = 90, right = 16, top = 34, bottom = 48;
  const plotWidth = width - left - right, plotHeight = height - top - bottom;
  const maxRevenue = Math.max(0, ...points.map(p => p.revenue));
  const tickStep = maxRevenue >= 50_000_000 ? 50_000_000 : maxRevenue >= 5_000_000 ? 5_000_000 : maxRevenue >= 500_000 ? 500_000 : maxRevenue >= 50_000 ? 50_000 : 5_000;
  const max = Math.max(tickStep, Math.ceil(maxRevenue / tickStep) * tickStep);
  const step = plotWidth / Math.max(points.length, 1), barWidth = Math.min(42, step * .62);
  const ticks = Array.from({ length: Math.floor(max / tickStep) + 1 }, (_, i) => i * tickStep);
  const total = points.reduce((sum, p) => sum + p.revenue, 0);
  let angle = -Math.PI / 2;
  const slices = points.filter(p => p.revenue > 0).map((p, i) => {
    const start = angle, delta = p.revenue / total * Math.PI * 2; angle += delta;
    const x1 = 118 + 88 * Math.cos(start), y1 = 112 + 88 * Math.sin(start);
    const x2 = 118 + 88 * Math.cos(angle), y2 = 112 + 88 * Math.sin(angle);
    const large = delta > Math.PI ? 1 : 0;
    return { point: p, color: chartColors[i % chartColors.length], path: `M 118 112 L ${x1} ${y1} A 88 88 0 ${large} 1 ${x2} ${y2} Z` };
  });
  return <div className="row g-3 mb-4">
    <div className="col-xl-8"><section className="card report-card h-100"><header className="report-card-heading"><div><h3>Doanh thu theo thời gian</h3><p>Các đơn hàng đã hoàn tất</p></div><span className="report-chart-icon" aria-hidden="true">▥</span></header>
      {!points.length ? <p className="report-empty">Không có dữ liệu trong khoảng thời gian đã chọn.</p> : <div className="chart-scroll"><svg className="revenue-chart" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Biểu đồ cột doanh thu theo thời gian">
        <text transform={`translate(18 ${top + plotHeight / 2}) rotate(-90)`} textAnchor="middle" className="chart-axis-title">Tổng doanh thu</text>
        {ticks.map(value => { const y = top + plotHeight * (1 - value / max); return <g key={value}><line x1={left} x2={width - right} y1={y} y2={y} stroke="#e7edf3" /><text x={left - 10} y={y + 4} textAnchor="end" className="chart-axis-label">{value.toLocaleString("vi-VN")}</text></g>; })}
        {points.map((p, i) => { const barHeight = p.revenue / max * plotHeight, x = left + i * step + (step - barWidth) / 2, barTop = top + plotHeight - barHeight; return <g key={p.periodStart}><title>{periodDate(p.periodStart, period)}: {money(p.revenue)}</title><rect x={x} y={barTop} width={barWidth} height={Math.max(barHeight, 1)} fill="#0d6efd" /><text x={x + barWidth / 2} y={Math.max(top - 8, barTop - 7)} textAnchor="middle" className="chart-value-label">{money(p.revenue)}</text><text x={x + barWidth / 2} y={height - 20} textAnchor="middle" className="chart-axis-label">{periodDate(p.periodStart, period)}</text></g>; })}
      </svg></div>}
    </section></div>
    <div className="col-xl-4"><section className="card report-card h-100"><header className="report-card-heading"><div><h3>Cơ cấu doanh thu</h3><p>Tỷ trọng theo {period === "Day" ? "ngày" : period === "Week" ? "tuần" : "tháng"}</p></div><span className="report-chart-icon" aria-hidden="true">◔</span></header>
      {!slices.length ? <p className="report-empty">Chưa có doanh thu để phân bổ.</p> : <><div className="pie-chart-wrap"><svg viewBox="0 0 236 224" role="img" aria-label="Biểu đồ tròn cơ cấu doanh thu">{slices.map(({ point, color, path }) => <path key={point.periodStart} d={path} fill={color}><title>{periodDate(point.periodStart, period)}: {money(point.revenue)} ({(point.revenue / total * 100).toFixed(1)}%)</title></path>)}</svg></div><ul className="chart-legend">{slices.map(({ point, color }) => <li key={point.periodStart}><span style={{ backgroundColor: color }} />{periodDate(point.periodStart, period)} · {(point.revenue / total * 100).toFixed(1)}%</li>)}</ul></>}
    </section></div>
  </div>;
}

export function ReportPage() {
  const role = readSession()?.user?.role;
  const [from, setFrom] = useState(localDate().slice(0, 7) + "-01");
  const [to, setTo] = useState(localDate());
  const [period, setPeriod] = useState("Day");
  const [warehouse, setWarehouse] = useState("");
  const [product, setProduct] = useState("");
  const [category, setCategory] = useState("");
  const [staffId] = useState("");
  const [profitQuery, setProfitQuery] = useState(`FromDate=${from}&ToDate=${to}&Period=Day`);
  const [trendQuery, setTrendQuery] = useState(`FromDate=${from}&ToDate=${to}&Period=Day`);
  const [error, setError] = useState("");
  const warehouses = useQuery({ queryKey: ["report-warehouses"], queryFn: () => allPages<Warehouse>("/api/warehouses?isActive=true") });
  const categories = useQuery({ queryKey: ["report-categories"], queryFn: () => api<Category[]>("/api/categories") });
  const catalogProducts = useQuery({ queryKey: ["report-products"], queryFn: () => allPages<CatalogProduct>("/api/products?isActive=true&sortBy=Name&sortDirection=Asc") });
  const profit = useQuery({ queryKey: ["profit", profitQuery], queryFn: ({ signal }) => api<Profit>(`/api/reports/profit?${profitQuery}`, { signal }) });
  const trend = useQuery({ queryKey: ["sales-trend", trendQuery], queryFn: ({ signal }) => api<TrendPoint[]>(`/api/reports/sales-trend?${trendQuery}`, { signal }) });

  function apply() {
    setError("");
    if (!from || !to || from > to || (Date.parse(to) - Date.parse(from)) / 86400000 > 366) { setError("Chọn khoảng ngày hợp lệ, tối đa 366 ngày."); return; }
    const query = new URLSearchParams({ FromDate: from, ToDate: to });
    for (const [key, value] of [["WarehouseId", warehouse], ["ProductId", product], ["CategoryId", category]]) if (value) query.set(key, value);
    query.set("Period", period);
    if (staffId) query.set("StaffUserId", staffId);
    setProfitQuery(query.toString());
    setTrendQuery(query.toString());
  }
  const currency = (value: number | null) => value === null ? "Chưa xác định" : money(value);
  const totals = profit.data?.totals;
  const periods = profit.data?.periods ?? [];
  const average = (value: number) => periods.length ? money(value / periods.length) : money(0);
  const percent = (value: number) => totals?.revenue ? `${(value / totals.revenue * 100).toFixed(1)}%` : "0.0%";

  return <main className="report-page">
    <form className="report-filter-bar" onSubmit={e => { e.preventDefault(); apply(); }}>
      <label className="visually-hidden" htmlFor="report-period">Chu kỳ</label><select id="report-period" className="form-select report-period-select" value={period} onChange={e => setPeriod(e.target.value)}><option value="Day">Theo ngày</option><option value="Week">Theo tuần</option><option value="Month">Theo tháng</option></select>
      <div className="report-date-field"><label className="visually-hidden" htmlFor="report-from">Từ ngày</label><input id="report-from" type="date" className="form-control" required value={from} onChange={e => setFrom(e.target.value)} /><span>đến</span><label className="visually-hidden" htmlFor="report-to">Đến ngày</label><input id="report-to" type="date" className="form-control" required value={to} onChange={e => setTo(e.target.value)} /></div>
      <label className="visually-hidden" htmlFor="report-warehouse">Kho</label><select id="report-warehouse" aria-label="Kho" className="form-select report-filter-input" value={warehouse} onChange={e => setWarehouse(e.target.value)}><option value="">Tất cả kho</option>{(Array.isArray(warehouses.data) ? warehouses.data : []).map(w => <option key={w.warehouseId} value={w.warehouseId}>{w.name}</option>)}</select>
      <label className="visually-hidden" htmlFor="report-product">Sản phẩm</label><select id="report-product" aria-label="Sản phẩm" className="form-select report-filter-input" value={product} onChange={e => setProduct(e.target.value)}><option value="">Tất cả sản phẩm</option>{(Array.isArray(catalogProducts.data) ? catalogProducts.data : []).map(p => <option key={p.productId} value={p.productId}>{p.name} · {p.sku}</option>)}</select>
      <label className="visually-hidden" htmlFor="report-category">Danh mục</label><select id="report-category" aria-label="Danh mục" className="form-select report-filter-input" value={category} onChange={e => setCategory(e.target.value)}><option value="">Tất cả danh mục</option>{(Array.isArray(categories.data) ? categories.data : []).filter(c => c.isActive).map(c => <option key={c.categoryId} value={c.categoryId}>{c.name}</option>)}</select>
      <button className="btn btn-brand report-filter-button" aria-label="Lọc báo cáo" title="Lọc báo cáo" disabled={profit.isFetching || trend.isFetching}><i className="bi bi-funnel-fill" aria-hidden="true" /></button>
    </form>
    {error && <div className="alert alert-danger m-3" role="alert">{error}</div>}
    {profit.isFetching ? <p className="p-4" role="status">Đang tải báo cáo...</p> : profit.error ? <div className="alert alert-danger m-3" role="alert">{profit.error.message}<button className="btn btn-link" onClick={() => void profit.refetch()}>Thử lại</button></div> : totals && <>
      {totals.missingCostQuantity > 0 && <div className="alert alert-warning m-3">Thiếu giá vốn cho {totals.missingCostQuantity} sản phẩm đã bán; lợi nhuận gộp chưa thể xác định đầy đủ.</div>}
      <div className="report-kpis">{[["Doanh thu", money(totals.revenue), "bi bi-currency-exchange"], ["Lợi nhuận gộp", currency(totals.grossProfit), "bi bi-graph-up-arrow"], ["Đơn thành công", totals.orderCount.toLocaleString("vi-VN"), "bi bi-receipt"], ["Sản phẩm đã bán", totals.quantitySold.toLocaleString("vi-VN"), "bi bi-box-seam"]].map(([label, value, icon]) => <article className="report-kpi" key={label}><span><i className={icon} aria-hidden="true" />{label}</span><strong>{value}</strong></article>)}</div>
      {trend.isFetching ? <p className="px-3" role="status">Đang tải biểu đồ...</p> : trend.error ? <div className="alert alert-danger mx-3" role="alert">Không tải được biểu đồ: {trend.error.message}</div> : Array.isArray(trend.data) && <RevenueCharts points={trend.data} period={period} />}
      <section className="card report-table-card"><div className="report-table-toolbar">{role === "Admin" && <button type="button" className="btn btn-primary btn-sm" onClick={() => window.print()}><i className="bi bi-printer me-1" />In báo cáo</button>}<span className="small text-secondary">{date(from)} – {date(to)} · Giá vốn theo {profit.data?.costMethod}</span></div>
        <div className="table-responsive"><table className="table report-data-table"><thead><tr><th>#</th><th>Thời gian</th><th>Đơn thành công</th><th>Số lượng bán</th><th>Doanh thu</th><th>Giá vốn</th><th>Lợi nhuận</th></tr></thead><tbody>
          <tr className="report-summary-row"><th colSpan={2}>Tổng</th><td>{totals.orderCount.toLocaleString("vi-VN")}</td><td>{totals.quantitySold.toLocaleString("vi-VN")}</td><td>{money(totals.revenue)}</td><td>{totals.costOfGoodsSold === null ? "Chưa xác định" : money(totals.costOfGoodsSold)}</td><td>{totals.grossProfit === null ? "Chưa xác định" : money(totals.grossProfit)}</td></tr>
          <tr className="report-summary-row"><th colSpan={2}>Trung bình / kỳ</th><td>{average(totals.orderCount)}</td><td>{average(totals.quantitySold)}</td><td>{average(totals.revenue)}</td><td>{totals.costOfGoodsSold === null ? "Chưa xác định" : average(totals.costOfGoodsSold)}</td><td>{totals.grossProfit === null ? "Chưa xác định" : average(totals.grossProfit)}</td></tr>
          <tr className="report-summary-row"><th colSpan={2}>Tỷ lệ / tổng</th><td>100%</td><td>100%</td><td>100%</td><td>{totals.costOfGoodsSold === null ? "—" : percent(totals.costOfGoodsSold)}</td><td>{totals.grossProfit === null ? "—" : percent(totals.grossProfit)}</td></tr>
          {periods.map((p, i) => <tr key={p.periodStart}><td>{i + 1}</td><td>{date(p.periodStart)}</td><td>{p.totals.orderCount.toLocaleString("vi-VN")}</td><td>{p.totals.quantitySold.toLocaleString("vi-VN")}</td><td>{money(p.totals.revenue)}</td><td>{currency(p.totals.costOfGoodsSold)}</td><td>{currency(p.totals.grossProfit)}</td></tr>)}
        </tbody></table></div>{!periods.length && <p className="report-empty">Không có đơn hoàn tất trong bộ lọc.</p>}
      </section>
    </>}
  </main>;
}
