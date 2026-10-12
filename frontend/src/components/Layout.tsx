import { useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../features/auth/AuthProvider";
import { allowed, roleLabels, sections } from "../app/config";
import type { Section } from "../types";
import { StockAlerts } from "../features/reports/StockAlerts";
import { Modal } from "./Modal";
export function Layout() {
  const { session, logout, remaining } = useAuth();
  const [open, setOpen] = useState(false);
  const [accountMenuOpen, setAccountMenuOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const location = useLocation();
  const section = location.pathname.split("/")[1] as Section;
  const [reportsExpanded, setReportsExpanded] = useState(location.pathname.startsWith("/reports"));
  if (!session) return null;
  return (
    <main className={`admin-layout ${open ? "sidebar-open" : ""}`}>
      {open && (
        <button
          className="sidebar-backdrop show"
          aria-label="Đóng menu"
          onClick={() => setOpen(false)}
        />
      )}
      <aside className={`admin-sidebar ${open ? "open" : ""}`}>
        <div className="admin-brand">
          <span className="brand-mark">
            <i className="bi bi-graph-up-arrow" />
          </span>
          <div>
            <strong>Sales Forecast</strong>
            <small>Hệ thống quản trị</small>
          </div>
        </div>
        <nav className="admin-nav" aria-label="Điều hướng quản trị">
          {Object.entries(sections)
            .filter(([key]) => allowed(session.user.role, key))
            .map(([key, config]) => (
              key === "reports" ? <div className="report-nav-group" key={key}>
                <button className={`admin-nav-item report-nav-trigger ${location.pathname.startsWith("/reports") ? "active" : ""}`} aria-expanded={reportsExpanded} onClick={() => setReportsExpanded(value => !value)}>
                  <i aria-hidden="true" className={`bi bi-${config.icon}`} /><span>{config.label}</span><i aria-hidden="true" className={`bi bi-chevron-${reportsExpanded ? "down" : "right"} report-nav-chevron`} />
                </button>
                {reportsExpanded && <div className="report-subnav">
                  <NavLink to="/reports/analysis" onClick={() => setOpen(false)} className={({ isActive }) => `report-subnav-item ${isActive ? "active" : ""}`}>Phân tích bán hàng</NavLink>
                  <NavLink to="/reports" end onClick={() => setOpen(false)} className={({ isActive }) => `report-subnav-item ${isActive ? "active" : ""}`}>Danh sách báo cáo</NavLink>
                </div>}
              </div> :
                <NavLink
                  key={key}
                  aria-label={config.label}
                  to={`/${key}`}
                  onClick={() => setOpen(false)}
                  className={({ isActive }) =>
                    `admin-nav-item ${isActive ? "active" : ""}`
                  }
                >
                  <i aria-hidden="true" className={`bi bi-${config.icon}`} />
                  {config.label}
                </NavLink>
            ))}
        </nav>
      </aside>
      <section className="admin-workspace">
        <header className="admin-topbar">
          <div className="topbar-leading">
            <button
              className="sidebar-toggle"
              aria-label="Mở menu"
              onClick={() => setOpen(!open)}
            >
              <i className="bi bi-list" />
            </button>
            <h1 className="fs-6">
              TRANG QUẢN TRỊ - {sections[section]?.title || "HỆ THỐNG"}
            </h1>
          </div>
          <div className="topbar-actions">
            <StockAlerts />
            <div className="account-menu-wrap">
              <button className="account-menu-trigger" aria-expanded={accountMenuOpen} aria-haspopup="true" onClick={() => setAccountMenuOpen(value => !value)}>
                <span>{session.user.fullName}</span><i className={`bi bi-chevron-${accountMenuOpen ? "up" : "down"}`} aria-hidden="true" />
              </button>
              {accountMenuOpen && <section className="account-menu" aria-label="Tài khoản người dùng">
                <button className="account-menu-item" onClick={() => { setAccountMenuOpen(false); setProfileOpen(true); }}><i className="bi bi-person-vcard" aria-hidden="true" />Thông tin cá nhân</button>
                <button className="account-menu-item account-menu-logout" onClick={() => logout()}><i className="bi bi-box-arrow-right" aria-hidden="true" />Đăng xuất</button>
              </section>}
            </div>
          </div>
        </header>
        <div className="admin-content">
          {remaining <= 60000 && (
            <div className="alert alert-warning" role="alert">
              Phiên sẽ kết thúc sau {Math.ceil(remaining / 1000)} giây nếu không
              hoạt động.
            </div>
          )}
          <Outlet />
        </div>
      </section>
      {profileOpen && <Modal title="Thông tin cá nhân" onClose={() => setProfileOpen(false)}>
        <dl className="profile-details"><dt>Họ tên</dt><dd>{session.user.fullName}</dd><dt>Vai trò</dt><dd>{roleLabels[session.user.role]}</dd><dt>Email</dt><dd>{session.user.email}</dd></dl>
      </Modal>}
    </main>
  );
}
