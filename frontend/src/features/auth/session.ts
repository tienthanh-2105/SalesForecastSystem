import type { Session } from "../../types";
export const STORAGE_KEY = "salesForecast.session";
export const IDLE_MS = 15 * 60 * 1000;
export const REFRESH_MS = 5 * 60 * 1000;
export function readSession(): Session | null {
  try {
    const value = JSON.parse(
      sessionStorage.getItem(STORAGE_KEY) ||
        localStorage.getItem(STORAGE_KEY) ||
        "null",
    );
    return value?.accessToken &&
      value?.user &&
      Number.isFinite(value.expiresAt) &&
      Number.isFinite(value.lastActivityAt)
      ? value
      : null;
  } catch {
    return null;
  }
}
export function saveSession(session: Session | null) {
  sessionStorage.removeItem(STORAGE_KEY);
  localStorage.removeItem(STORAGE_KEY);
  if (session)
    (session.rememberMe ? localStorage : sessionStorage).setItem(
      STORAGE_KEY,
      JSON.stringify(session),
    );
}
export const sessionExpired = (session: Session, now = Date.now()) =>
  now >= session.expiresAt || now - session.lastActivityAt >= IDLE_MS;
