import { useState } from "react";
import { useForm } from "react-hook-form";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "./AuthProvider";
import { home } from "../../app/config";
import { ApiError } from "../../lib/api";
interface Fields {
  email: string;
  password: string;
  remember: boolean;
}
export function LoginPage() {
  const auth = useAuth();
  const location = useLocation();
  const [visible, setVisible] = useState(false);
  const [error, setError] = useState("");
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<Fields>();
  const requested = (location.state as { from?: string } | null)?.from;
  if (auth.session)
    return (
      <Navigate
        replace
        to={
          requested &&
          /^\/(categories|products|warehouses|orders|users)(\?|$)/.test(
            requested,
          )
            ? requested
            : home(auth.session.user.role)
        }
      />
    );
  return (
    <main className="login-page">
      <div className="login-shell">
        <section className="login-visual">
          <div className="visual-grid" />
          <div className="visual-orb visual-orb-one" />
          <div className="visual-orb visual-orb-two" />
          <div className="visual-content">
            <div className="brand brand-light">
              <span className="brand-mark">
                <i className="bi bi-graph-up-arrow" />
              </span>
              Sales Forecast
            </div>
            <div className="visual-copy">
              <div className="welcome-title">WELCOME</div>
              <h1>HỆ THỐNG DỰ BÁO NHU CẦU BÁN HÀNG</h1>
              <h2>
                Nắm bắt dữ liệu.
                <br />
                Chủ động tương lai.
              </h2>
              <div className="benefit-list">
                <div className="benefit-item">
                  <span>
                    <i className="bi bi-check2" />
                  </span>
                  Quản lý dữ liệu tập trung và chính xác
                </div>
                <div className="benefit-item">
                  <span>
                    <i className="bi bi-check2" />
                  </span>
                  Báo cáo trực quan theo thời gian thực
                </div>
              </div>
            </div>
            <p className="visual-footer">
              Sales Forecast System · ASP.NET Core 8
            </p>
          </div>
        </section>
        <section className="login-form-side">
          <form
            className="login-card"
            onSubmit={handleSubmit(async (values) => {
              setError("");
              try {
                await auth.login(
                  values.email.trim(),
                  values.password,
                  values.remember,
                );
              } catch (e) {
                setError(
                  e instanceof ApiError && e.status === 401
                    ? "Email hoặc mật khẩu không chính xác."
                    : (e as Error).message,
                );
              }
            })}
          >
            <header className="login-header text-center">
              <h1>Đăng nhập</h1>
            </header>
            {(error || auth.message) && (
              <div role="alert" className="alert app-alert">
                {error || auth.message}
              </div>
            )}
            <div className="form-group">
              <label htmlFor="email" className="form-label">
                Email đăng nhập
              </label>
              <div className="field-wrap">
                <i className="bi bi-envelope field-icon" />
                <input
                  id="email"
                  type="email"
                  autoComplete="username"
                  className="form-control"
                  maxLength={100}
                  {...register("email", {
                    required: "Vui lòng nhập email.",
                    pattern: {
                      value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                      message: "Email không hợp lệ.",
                    },
                  })}
                />
              </div>
              {errors.email && (
                <small className="text-danger">{errors.email.message}</small>
              )}
            </div>
            <div className="form-group">
              <label htmlFor="password" className="form-label">
                Mật khẩu
              </label>
              <div className="field-wrap">
                <i className="bi bi-lock field-icon" />
                <input
                  id="password"
                  type={visible ? "text" : "password"}
                  autoComplete="current-password"
                  className="form-control"
                  {...register("password", {
                    required: "Vui lòng nhập mật khẩu.",
                  })}
                />
                <button
                  type="button"
                  className="password-toggle"
                  aria-label={visible ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                  onClick={() => setVisible(!visible)}
                >
                  <i className={`bi bi-eye${visible ? "-slash" : ""}`} />
                </button>
              </div>
              {errors.password && (
                <small className="text-danger">{errors.password.message}</small>
              )}
            </div>
            <div className="login-options">
              <label>
                <input type="checkbox" {...register("remember")} /> Ghi nhớ đăng
                nhập
              </label>
            </div>
            <button className="btn btn-brand w-100" disabled={isSubmitting}>
              {isSubmitting ? "Đang đăng nhập..." : "Đăng nhập"}
            </button>
          </form>
        </section>
      </div>
    </main>
  );
}
