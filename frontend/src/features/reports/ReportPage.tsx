import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../../lib/api";
import { date, localDate, money } from "../../lib/format";
interface Totals { orderCount: number; quantitySold: number; revenue: number; costOfGoodsSold: number | null; grossProfit: number | null; missingCostQuantity: number }
interface Profit { costMethod: string; totals: Totals; periods: { periodStart: string; totals: Totals }[] }
interface Staff { userId: number; fullName: string; orderCount: number; quantitySold: number; revenue: number }
interface Group { groupName: string; revenue: number; quantitySold: number; orderCount: number; members: Staff[] }
export function ReportPage() {
  const [from, setFrom] = useState(localDate().slice(0, 7) + "-01");
  const [to, setTo] = useState(localDate());
  const [period, setPeriod] = useState("Day");
  const [warehouse, setWarehouse] = useState("");
  const [product, setProduct] = useState("");
  const [category, setCategory] = useState("");
  const [staffId, setStaffId] = useState("");
  const [ids, setIds] = useState("");
  const [name, setName] = useState("Nhóm bán hàng");
  const [profitQuery, setProfitQuery] = useState(`FromDate=${from}&ToDate=${to}&Period=Day`);
  const [groupQuery, setGroupQuery] = useState<string | null>(null);
  const [error, setError] = useState("");
  const profit = useQuery({ queryKey: ["profit", profitQuery], queryFn: ({ signal }) => api<Profit>(`/api/reports/profit?${profitQuery}`, { signal }) });
  const group = useQuery({ queryKey: ["staff-group", groupQuery], queryFn: ({ signal }) => api<Group>(`/api/reports/sales-by-group?${groupQuery}`, { signal }), enabled: groupQuery !== null });
  const staff = useQuery({ queryKey: ["report-staff"], queryFn: () => api<Staff[]>("/api/reports/sales-by-staff?Top=100") });
  function apply(isGroup: boolean) {
    setError("");
    if (!from || !to || from > to || (Date.parse(to) - Date.parse(from)) / 86400000 > 366) { setError("Chọn khoảng ngày hợp lệ, tối đa 366 ngày."); return; }
    const query = new URLSearchParams({ FromDate: from, ToDate: to });
    for (const [key, value] of [["WarehouseId", warehouse], ["ProductId", product], ["CategoryId", category]]) if (value) query.set(key, value);
    if (isGroup) {
      const members = [...new Set(ids.split(/[\s,;]+/).filter(Boolean))];
      if (!name.trim() || name.trim().length > 100 || !members.length || members.length > 100 || members.some(id => !/^[1-9]\d*$/.test(id) || Number(id) > 2147483647)) { setError("Nhập tên nhóm tối đa 100 ký tự và 1–100 mã nhân viên nguyên dương, cách nhau bằng dấu phẩy."); return; }
      query.set("GroupName", name.trim()); members.forEach(id => query.append("StaffUserIds", id)); setGroupQuery(query.toString());
    } else { query.set("Period", period); if (staffId) query.set("StaffUserId", staffId); setProfitQuery(query.toString()); }
  }
  const currency = (value: number | null) => value === null ? "Chưa xác định" : money(value);
  return <>
    <h2 className="fs-4">Báo cáo lợi nhuận và nhóm nhân viên</h2>
    <p className="text-secondary">Chỉ tính đơn đã hoàn tất. Lợi nhuận gộp = doanh thu sau giảm giá − giá vốn FIFO; chưa trừ chi phí vận hành và thuế.</p>
    <form className="card p-3 mb-4" onSubmit={e => { e.preventDefault(); apply(false); }}>
      <div className="row g-3">
        <div className="col-md-4"><label className="form-label" htmlFor="report-from">Từ ngày</label><input id="report-from" type="date" className="form-control" required value={from} onChange={e => setFrom(e.target.value)} /></div>
        <div className="col-md-4"><label className="form-label" htmlFor="report-to">Đến ngày</label><input id="report-to" type="date" className="form-control" required value={to} onChange={e => setTo(e.target.value)} /></div>
        <div className="col-md-4"><label className="form-label" htmlFor="report-period">Chu kỳ</label><select id="report-period" className="form-select" value={period} onChange={e => setPeriod(e.target.value)}><option value="Day">Ngày</option><option value="Week">Tuần</option><option value="Month">Tháng</option></select></div>
        {[["Mã kho", warehouse, setWarehouse], ["Mã sản phẩm", product, setProduct], ["Mã danh mục", category, setCategory], ["Mã nhân viên (lợi nhuận)", staffId, setStaffId]].map(([label, value, setter], i) => <div className="col-md-3" key={i}><label htmlFor={`report-id-${i}`} className="form-label">{label as string}</label><input id={`report-id-${i}`} className="form-control" type="number" min="1" max="2147483647" step="1" placeholder="Tất cả" value={value as string} onChange={e => (setter as (v: string) => void)(e.target.value)} /></div>)}
      </div>
      <button className="btn btn-brand mt-3 align-self-start" disabled={profit.isFetching}>Xem lợi nhuận</button>
    </form>
    {error && <div className="alert alert-danger" role="alert">{error}</div>}
    {profit.isFetching ? <p role="status">Đang tải lợi nhuận...</p> : profit.error ? <div className="alert alert-danger" role="alert">{profit.error.message}<button className="btn btn-link" onClick={() => void profit.refetch()}>Thử lại</button></div> : profit.data && <>
      {profit.data.totals.missingCostQuantity > 0 && <div className="alert alert-warning">Thiếu giá vốn cho {profit.data.totals.missingCostQuantity} sản phẩm đã bán. Giá vốn và lợi nhuận chưa thể xác định đầy đủ.</div>}
      <div className="row g-3 mb-4">{[["Doanh thu", money(profit.data.totals.revenue)], ["Giá vốn", currency(profit.data.totals.costOfGoodsSold)], ["Lợi nhuận gộp", currency(profit.data.totals.grossProfit)], ["Đơn / số lượng bán", `${profit.data.totals.orderCount} / ${profit.data.totals.quantitySold}`]].map(([label, value]) => <div className="col-sm-6 col-lg-3" key={label}><div className="card p-3 h-100"><small className="text-secondary">{label}</small><strong className="fs-5 mt-2">{value}</strong></div></div>)}</div>
      <div className="card p-3 mb-4"><h3 className="fs-5">Lợi nhuận theo kỳ · {profit.data.costMethod}</h3><div className="table-responsive"><table className="table"><thead><tr><th>Đầu kỳ</th><th>Đơn</th><th>Số lượng</th><th>Doanh thu</th><th>Giá vốn</th><th>Lợi nhuận gộp</th></tr></thead><tbody>{profit.data.periods.map(p => <tr key={p.periodStart}><td>{date(p.periodStart)}</td><td>{p.totals.orderCount}</td><td>{p.totals.quantitySold}</td><td>{money(p.totals.revenue)}</td><td>{currency(p.totals.costOfGoodsSold)}</td><td>{currency(p.totals.grossProfit)}</td></tr>)}</tbody></table></div>{!profit.data.periods.length && <p>Không có đơn hoàn tất trong bộ lọc.</p>}</div>
    </>}
    <form className="card p-3" onSubmit={e => { e.preventDefault(); apply(true); }}>
      <h3 className="fs-5">Doanh số nhóm nhân viên</h3><p className="small text-secondary">Nhóm được chọn cho lần báo cáo này. Áp dụng khoảng ngày, kho, sản phẩm và danh mục ở trên.</p>
      <label className="form-label" htmlFor="group-name">Tên nhóm</label><input id="group-name" className="form-control mb-3" required maxLength={100} value={name} onChange={e => setName(e.target.value)} />
      <label className="form-label" htmlFor="staff-ids">Mã nhân viên, cách nhau bằng dấu phẩy</label><input id="staff-ids" className="form-control" required value={ids} onChange={e => setIds(e.target.value)} placeholder="Ví dụ: 1, 2, 3" />
      {staff.data && <details className="mt-2"><summary>Tra cứu mã nhân viên có doanh số (tối đa 100)</summary><ul>{staff.data.map(s => <li key={s.userId}>{s.userId} · {s.fullName}</li>)}</ul></details>}
      <p className="small text-secondary mt-2">Có thể nhập mã của nhân viên không có doanh số hoặc đã khóa để xem lịch sử.</p>
      <button className="btn btn-brand align-self-start" disabled={group.isFetching}>Xem doanh số nhóm</button>
    </form>
    {groupQuery && (group.isFetching ? <p role="status">Đang tải doanh số nhóm...</p> : group.error ? <div className="alert alert-danger mt-3" role="alert">{group.error.message}<button className="btn btn-link" onClick={() => void group.refetch()}>Thử lại</button></div> : group.data && <div className="card p-3 mt-3"><h3 className="fs-5">{group.data.groupName}</h3><p>{group.data.orderCount} đơn · {group.data.quantitySold} sản phẩm · {money(group.data.revenue)}</p><div className="table-responsive"><table className="table"><thead><tr><th>Nhân viên</th><th>Đơn</th><th>Số lượng bán</th><th>Doanh thu</th></tr></thead><tbody>{group.data.members.map(s => <tr key={s.userId}><td>{s.fullName} ({s.userId})</td><td>{s.orderCount}</td><td>{s.quantitySold}</td><td>{money(s.revenue)}</td></tr>)}</tbody></table></div></div>)}
  </>;
}
