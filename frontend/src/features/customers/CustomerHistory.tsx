import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { Modal } from "../../components/Modal";
import { StatusBadge } from "../../components/StatusBadge";
import { api } from "../../lib/api";
import { date } from "../../lib/format";
import type { Customer, CustomerOrder, Page } from "../../types";

export function CustomerHistory({ customer, onClose }: { customer: Customer; onClose: () => void }) {
  const [page, setPage] = useState(1);
  const history = useQuery({
    queryKey: ["customer-orders", customer.customerId, page],
    queryFn: ({ signal }) => api<Page<CustomerOrder>>(`/api/customers/${customer.customerId}/orders?page=${page}&pageSize=10`, { signal }),
  });
  return <Modal title={`Lịch sử đơn hàng - ${customer.fullName}`} onClose={onClose}>
    {history.isPending ? <p role="status">Đang tải lịch sử...</p> : history.error ? <div role="alert" className="alert alert-danger">{history.error.message}<button className="btn btn-outline-danger ms-2" onClick={() => void history.refetch()}>Thử lại</button></div> : <>
      {!history.data?.items.length ? <p>Khách hàng chưa có đơn hàng.</p> : <div className="table-responsive"><table className="table align-middle"><thead><tr><th>Mã đơn</th><th>Ngày đặt</th><th>Trạng thái đơn hàng</th></tr></thead><tbody>{history.data.items.map(order => <tr key={order.salesOrderId}><td><Link to={`/orders?search=${encodeURIComponent(order.orderNumber)}`} onClick={onClose}>{order.orderNumber}</Link></td><td>{date(order.orderDate)}</td><td><StatusBadge status={order.status} /></td></tr>)}</tbody></table></div>}
      <div className="d-flex justify-content-between align-items-center"><small>{history.data?.totalItems || 0} đơn hàng · Trang {page}/{Math.max(1, history.data?.totalPages || 1)}</small><div><button className="btn btn-light border" aria-label="Trang lịch sử trước" disabled={page <= 1 || history.isFetching} onClick={() => setPage(page - 1)}>‹</button><button className="btn btn-light border" aria-label="Trang lịch sử sau" disabled={page >= (history.data?.totalPages || 1) || history.isFetching} onClick={() => setPage(page + 1)}>›</button></div></div>
    </>}
  </Modal>;
}
