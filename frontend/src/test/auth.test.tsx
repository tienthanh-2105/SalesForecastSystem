import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, expect, it, vi } from "vitest";
import { AuthProvider, useAuth } from "../features/auth/AuthProvider";
import { IDLE_MS, REFRESH_MS, saveSession } from "../features/auth/session";
import { configureApi } from "../lib/api";
import type { Session } from "../types";
afterEach(() => {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  localStorage.clear();
  sessionStorage.clear();
  configureApi("");
});
function Probe() {
  const auth = useAuth();
  return (
    <>
      <span>
        {auth.ready
          ? auth.session
            ? "authenticated"
            : "logged-out"
          : "loading"}
      </span>
      <span>{auth.remaining}</span>
      <button onClick={() => auth.logout()}>logout</button>
    </>
  );
}
function setup() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const view = render(
    <QueryClientProvider client={client}>
      <AuthProvider>
        <Probe />
      </AuthProvider>
    </QueryClientProvider>,
  );
  return { client, ...view };
}
const initial = (): Session => ({
  accessToken: "token",
  expiresAt: Date.now() + 3600000,
  user: { userId: 1, email: "a@b.c", fullName: "A", role: "Admin" },
  rememberMe: false,
  lastActivityAt: Date.now(),
  lastRefreshAt: Date.now(),
});
it("restore, cảnh báo 60 giây, idle logout, cache được xóa", async () => {
  vi.useFakeTimers();
  const s = initial();
  saveSession(s);
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(new Response(JSON.stringify(initial().user))),
  );
  const { client } = setup();
  client.setQueryData(["private"], "secret");
  await act(async () => {});
  expect(screen.getByText("authenticated")).toBeInTheDocument();
  await act(async () => vi.advanceTimersByTime(IDLE_MS - 60000));
  expect(screen.getByText("60000")).toBeInTheDocument();
  await act(async () => vi.advanceTimersByTime(60000));
  expect(screen.getByText("logged-out")).toBeInTheDocument();
  expect(client.getQueryData(["private"])).toBeUndefined();
});
it("refresh single-flight, activity không bị reset bởi response", async () => {
  vi.useFakeTimers();
  saveSession(initial());
  let resolveRefresh!: (r: Response) => void;
  const fetcher = vi
    .fn()
    .mockResolvedValueOnce(new Response(JSON.stringify(initial().user)))
    .mockImplementationOnce(
      () =>
        new Promise<Response>((resolve) => {
          resolveRefresh = resolve;
        }),
    );
  vi.stubGlobal("fetch", fetcher);
  setup();
  await act(async () => {});
  await act(async () => vi.advanceTimersByTime(REFRESH_MS));
  fireEvent.keyDown(window, { key: "a" });
  fireEvent.keyDown(window, { key: "b" });
  expect(fetcher).toHaveBeenCalledTimes(2);
  await act(async () => {
    resolveRefresh(
      new Response(
        JSON.stringify({
          accessToken: "new",
          expiresAt: new Date(Date.now() + 3600000).toISOString(),
          user: initial().user,
        }),
      ),
    );
  });
  expect(screen.getByText(String(IDLE_MS))).toBeInTheDocument();
});
it("dọn timer/listener khi unmount", async () => {
  vi.useFakeTimers();
  saveSession(initial());
  const fetcher = vi
    .fn()
    .mockResolvedValue(new Response(JSON.stringify(initial().user)));
  vi.stubGlobal("fetch", fetcher);
  const clear = vi.spyOn(window, "clearInterval");
  const { unmount, client } = setup();
  await act(async () => {});
  unmount();
  client.clear();
  expect(clear).toHaveBeenCalled();
  fireEvent.keyDown(window, { key: "a" });
  expect(fetcher).toHaveBeenCalledOnce();
});
