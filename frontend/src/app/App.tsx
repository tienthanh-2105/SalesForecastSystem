import { Navigate, Outlet, Route, Routes, useLocation } from "react-router-dom";
import { useAuth } from "../features/auth/AuthProvider";
import { LoginPage } from "../features/auth/LoginPage";
import { Layout } from "../components/Layout";
import { ManagementPage } from "../features/ManagementPage";
import { allowed, home, sections } from "./config";
import type { Section } from "../types";
function Protected() {
  const { session, ready } = useAuth();
  const location = useLocation();
  if (!ready) return <div className="data-state">Đang kiểm tra phiên...</div>;
  return session ? (
    <Outlet />
  ) : (
    <Navigate
      to="/login"
      replace
      state={{ from: location.pathname + location.search }}
    />
  );
}
function Access({ section }: { section: Section }) {
  const { session } = useAuth();
  return session && allowed(session.user.role, section) ? (
    <ManagementPage key={section} section={section} />
  ) : (
    <Navigate to="/forbidden" replace />
  );
}
function Home() {
  const { session } = useAuth();
  return <Navigate replace to={session ? home(session.user.role) : "/login"} />;
}
export function App() {
  const { ready } = useAuth();
  if (!ready) return <div className="data-state">Đang kiểm tra phiên...</div>;
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<Protected />}>
        <Route element={<Layout />}>
          <Route index element={<Home />} />
          {(Object.keys(sections) as Section[]).map((section) => (
            <Route
              key={section}
              path={`/${section}`}
              element={<Access section={section} />}
            />
          ))}
          <Route
            path="/forbidden"
            element={
              <div className="alert alert-warning">
                Tài khoản không có quyền truy cập trang này.
              </div>
            }
          />
          <Route
            path="*"
            element={
              <div className="alert alert-warning">Không tìm thấy trang.</div>
            }
          />
        </Route>
      </Route>
    </Routes>
  );
}
