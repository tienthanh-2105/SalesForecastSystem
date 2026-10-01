import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { api, configureApi, send } from "../../lib/api";
import type { LoginResponse, Session, User } from "../../types";
import {
  IDLE_MS,
  REFRESH_MS,
  readSession,
  saveSession,
  sessionExpired,
} from "./session";
interface Auth {
  session: Session | null;
  ready: boolean;
  remaining: number;
  message: string;
  login: (email: string, password: string, remember: boolean) => Promise<void>;
  logout: (reason?: string, notify?: boolean) => void;
}
const Context = createContext<Auth | null>(null);
export function AuthProvider({ children }: { children: ReactNode }) {
  const client = useQueryClient();
  const [session, setSession] = useState<Session | null>(null);
  const [ready, setReady] = useState(false);
  const [message, setMessage] = useState("");
  const [remaining, setRemaining] = useState(IDLE_MS);
  const current = useRef<Session | null>(null);
  const refresh = useRef(false);
  const generation = useRef(0);
  const update = useCallback((next: Session | null) => {
    current.current = next;
    saveSession(next);
    setSession(next);
    configureApi(next?.accessToken || "", () =>
      logoutRef.current("expired", false),
    );
  }, []);
  const logout = useCallback(
    (reason = "manual", notify = true) => {
      const token = current.current?.accessToken;
      generation.current++;
      update(null);
      client.clear();
      setMessage(
        reason === "manual"
          ? "Bạn đã đăng xuất thành công."
          : reason === "idle"
            ? "Phiên đã hết do không hoạt động. Vui lòng đăng nhập lại."
            : "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
      );
      if (notify && token)
        void api("/api/auth/logout", {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
        }).catch(() => {});
    },
    [client, update],
  );
  const logoutRef = useRef(logout);
  useEffect(() => {
    logoutRef.current = logout;
  }, [logout]);
  useEffect(() => {
    let active = true;
    const saved = readSession();
    if (!saved || sessionExpired(saved)) {
      update(null);
      setReady(true);
      return;
    }
    configureApi(saved.accessToken);
    void api<User>("/api/auth/me")
      .then((user) => {
        if (active) update({ ...saved, user });
      })
      .catch(() => {
        if (active) update(null);
      })
      .finally(() => {
        if (active) setReady(true);
      });
    return () => {
      active = false;
    };
  }, [update]);
  useEffect(() => {
    if (!session) return;
    let lastWrite = 0;
    const tick = () => {
      const value = current.current;
      if (!value) return;
      setRemaining(Math.max(0, IDLE_MS - (Date.now() - value.lastActivityAt)));
      if (sessionExpired(value))
        logoutRef.current(
          Date.now() - value.lastActivityAt >= IDLE_MS ? "idle" : "expired",
        );
    };
    const activity = () => {
      const value = current.current;
      if (!value || sessionExpired(value)) {
        tick();
        return;
      }
      value.lastActivityAt = Date.now();
      setRemaining(IDLE_MS);
      if (Date.now() - lastWrite > 1000) {
        saveSession(value);
        lastWrite = Date.now();
      }
      if (Date.now() - value.lastRefreshAt >= REFRESH_MS && !refresh.current) {
        refresh.current = true;
        const version = generation.current;
        void send<LoginResponse>("/api/auth/refresh", "POST")
          .then((result) => {
            if (version !== generation.current || !current.current) return;
            update({
              ...current.current,
              accessToken: result.accessToken,
              expiresAt: new Date(result.expiresAt).getTime(),
              user: result.user,
              lastRefreshAt: Date.now(),
            });
          })
          .catch(() => {})
          .finally(() => {
            refresh.current = false;
          });
      }
    };
    const events = [
      "pointerdown",
      "keydown",
      "mousemove",
      "touchstart",
      "scroll",
    ];
    events.forEach((event) =>
      window.addEventListener(event, activity, { passive: true }),
    );
    document.addEventListener("visibilitychange", tick);
    const timer = window.setInterval(tick, 1000);
    tick();
    return () => {
      clearInterval(timer);
      document.removeEventListener("visibilitychange", tick);
      events.forEach((event) => window.removeEventListener(event, activity));
    };
  }, [session, update]);
  const login = async (
    email: string,
    password: string,
    rememberMe: boolean,
  ) => {
    const result = await send<LoginResponse>("/api/auth/login", "POST", {
      email,
      password,
    });
    generation.current++;
    client.clear();
    setMessage("");
    update({
      ...result,
      expiresAt: new Date(result.expiresAt).getTime(),
      rememberMe,
      lastActivityAt: Date.now(),
      lastRefreshAt: Date.now(),
    });
  };
  return (
    <Context.Provider
      value={{ session, ready, remaining, message, login, logout }}
    >
      {children}
    </Context.Provider>
  );
}
export function useAuth() {
  const value = useContext(Context);
  if (!value) throw new Error("AuthProvider required");
  return value;
}
