import { Fragment, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../features/auth/AuthProvider";
import { allowed, roleLabels, sections } from "../app/config";
import type { Section } from "../types";
export function Layout() {
  const { session, logout, remaining } = useAuth();
  const [open, setOpen] = useState(false);
  const section = useLocation().pathname.split("/")[1] as Section;
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
              <Fragment key={key}>
                {key !== "products" && key !== "customers" && key !== "purchases" && (
                  <span
                    className={`admin-nav-label ${key === "categories" ? "fs-6" : ""}`}
                  >
                    {key === "categories"
                      ? "QUẢN LÝ SẢN PHẨM"
                      : key === "warehouses"
                        ? "QUẢN LÝ KHO"
                        : key === "orders"
                          ? "BÁN HÀNG"
                          : "HỆ THỐNG"}
                  </span>
                )}
                <NavLink
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
              </Fragment>
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
            <div className="topbar-user">
              <strong className="topbar-user-name">{session.user.fullName}</strong>
              <span className="topbar-email">{session.user.email}</span>
            </div>
            <span className="topbar-role">{roleLabels[session.user.role]}</span>
            <button
              className="btn btn-outline-danger btn-sm"
              onClick={() => logout()}
            >
              <i className="bi bi-box-arrow-right me-2" />
              Đăng xuất
            </button>
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
    </main>
  );
}
