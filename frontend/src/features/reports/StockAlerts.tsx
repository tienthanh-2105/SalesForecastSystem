import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Modal } from "../../components/Modal";
import { api } from "../../lib/api";

interface StockAlert {
  productId: number; sku: string; productName: string; quantityOnHand: number;
  minimumStockLevel: number; quantitySold: number; isBestSeller: boolean; isOutOfStock: boolean;
}
interface Alerts { checkedAtUtc: string; fromDate: string; toDate: string; items: StockAlert[] }

const AUTO_CLOSE_MS = 15_000;
const REMIND_AFTER_MS = 30 * 60_000;

export function StockAlerts() {
  const [open, setOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(AUTO_CLOSE_MS / 1000);
  const [nextReminderAt, setNextReminderAt] = useState(0);
  const query = useQuery({
    queryKey: ["stock-alerts"],
    queryFn: async ({ signal }) => {
      const data = await api<Alerts>("/api/reports/stock-alerts", { signal });
      if (!data || !Array.isArray(data.items) || !data.checkedAtUtc) throw new Error("Dữ liệu cảnh báo không hợp lệ.");
      return data;
    },
    refetchInterval: 60_000,
    refetchOnWindowFocus: "always",
    staleTime: 0,
  });

  useEffect(() => {
    if (!query.data?.items.length || open || Date.now() < nextReminderAt) return;
    setSecondsLeft(AUTO_CLOSE_MS / 1000);
    setOpen(true);
  }, [query.data?.checkedAtUtc, open, nextReminderAt]);

  useEffect(() => {
    if (!open) return;
    const closesAt = Date.now() + AUTO_CLOSE_MS;
    const tick = () => {
      const remaining = Math.max(0, Math.ceil((closesAt - Date.now()) / 1000));
      setSecondsLeft(remaining);
      if (remaining === 0) {
        setOpen(false);
        setNextReminderAt(Date.now() + REMIND_AFTER_MS);
      }
    };
    const timer = window.setInterval(tick, 250);
    return () => window.clearInterval(timer);
  }, [open]);

  const close = () => {
    setOpen(false);
    setNextReminderAt(Date.now() + REMIND_AFTER_MS);
  };

  return <>
    <div className="notification-menu">
      <button className={`notification-trigger ${notificationsOpen ? "active" : ""}`} aria-label={`Thông báo${query.data?.items.length ? ` (${query.data.items.length})` : ""}`} aria-expanded={notificationsOpen} onClick={() => setNotificationsOpen(value => !value)}>
        <i className="bi bi-bell" aria-hidden="true" /><span>Thông báo</span>{!!query.data?.items.length && <b>{query.data.items.length > 99 ? "99+" : query.data.items.length}</b>}
      </button>
      {notificationsOpen && <section className="notification-popover" aria-label="Thông báo tồn kho">
        <header><div><strong>Thông báo</strong><small>{query.data?.items.length ?? 0} cảnh báo tồn kho</small></div><button aria-label="Đóng thông báo" onClick={() => setNotificationsOpen(false)}>×</button></header>
        {query.isError && <p className="notification-error" role="alert">Không thể cập nhật cảnh báo. {query.error.message}</p>}
        {query.isPending ? <p className="notification-empty">Đang kiểm tra tồn kho...</p> : query.data?.items.length ? <>
          <div className="notification-list">{query.data.items.map(item => <article className="notification-item" key={item.productId}>
            <span className={`notification-status ${item.isOutOfStock ? "out" : "low"}`}><i className={`bi ${item.isOutOfStock ? "bi-exclamation-octagon-fill" : "bi-exclamation-triangle-fill"}`} /></span>
            <div><strong>{item.productName}</strong><small>{item.sku}{item.isBestSeller ? " · Bán chạy" : ""}</small><small>Tồn {item.quantityOnHand} · Ngưỡng {item.minimumStockLevel} · Bán 30 ngày: {item.quantitySold}</small></div>
            <span className={`badge ${item.isOutOfStock ? "text-bg-danger" : "text-bg-warning"}`}>{item.isOutOfStock ? "Hết" : "Thấp"}</span>
          </article>)}</div>
          {query.data.checkedAtUtc && <small className="notification-updated">Cập nhật {new Date(query.data.checkedAtUtc).toLocaleTimeString("vi-VN", { timeZone: "Asia/Ho_Chi_Minh" })}</small>}
          <footer><button className="btn btn-outline-secondary btn-sm" disabled={query.isFetching} onClick={() => void query.refetch()}><i className="bi bi-arrow-clockwise me-1" />Kiểm tra lại</button><button className="btn btn-brand btn-sm" onClick={() => { setNotificationsOpen(false); setNextReminderAt(0); setOpen(true); }}>Xem cảnh báo</button></footer>
        </> : <p className="notification-empty">{query.data ? "Không có cảnh báo tồn kho." : "Chưa có thông báo."}</p>}
      </section>}
    </div>
    {open && query.data?.items.length ? <Modal title="Cảnh báo tồn kho" onClose={close} wide>
      <p role="status" className="text-secondary">Tự đóng sau {secondsLeft} giây · nhắc lại sau 30 phút nếu cảnh báo còn tồn tại.</p>
      <p className="text-danger">{query.data.items.length} sản phẩm cần chú ý · {query.data.items.filter(p => p.isBestSeller).length} sản phẩm bán chạy · {query.data.items.filter(p => p.isOutOfStock).length} sản phẩm hết hàng</p>
      <div className="table-responsive"><table className="table table-sm">
        <thead><tr><th>Sản phẩm</th><th>Tồn kho</th><th>Ngưỡng tối thiểu</th><th>Đã bán / 30 ngày</th><th>Cảnh báo</th></tr></thead>
        <tbody>{query.data.items.map(p => <tr key={p.productId}>
          <td>{p.sku} · {p.productName} {p.isBestSeller && <span className="badge text-bg-warning">Bán chạy</span>}</td>
          <td>{p.quantityOnHand}</td><td>{p.minimumStockLevel}</td><td>{p.quantitySold}</td>
          <td><span className={`badge ${p.isOutOfStock ? "text-bg-danger" : "text-bg-warning"}`}>{p.isOutOfStock ? "Hết hàng" : "Tồn kho thấp"}</span></td>
        </tr>)}</tbody>
      </table></div>
    </Modal> : null}
  </>;
}
