import { afterEach, describe, expect, it, vi } from "vitest";
import { validateImage } from "../features/products/image";
import {
  lineTotal,
  PartialOrderError,
  saveOrder,
  validateItems,
} from "../features/orders/service";
import { allowed, home } from "../app/config";
import {
  IDLE_MS,
  readSession,
  saveSession,
  sessionExpired,
} from "../features/auth/session";
import type { Session } from "../types";
afterEach(() => {
  vi.unstubAllGlobals();
  vi.useRealTimers();
  sessionStorage.clear();
  localStorage.clear();
});
describe("quy tắc dữ liệu", () => {
  it("chặn file sai định dạng và quá 5 MB", () => {
    expect(validateImage(new File(["x"], "a.png", { type: "image/png" }))).toBe(
      "",
    );
    expect(
      validateImage(new File(["x"], "a.svg", { type: "image/svg+xml" })),
    ).toContain("JPEG");
    const big = new File([new Uint8Array(5 * 1024 * 1024 + 1)], "a.jpg", {
      type: "image/jpeg",
    });
    expect(validateImage(big)).toContain("5 MB");
  });
  it("tính tổng và chặn dòng trùng, số lượng sai, giảm giá quá giá trị", () => {
    const item = { productId: 1, quantity: 2, unitPrice: 100, discount: 20 };
    expect(lineTotal(item)).toBe(180);
    expect(validateItems([item])).toBe("");
    expect(validateItems([item, item])).toContain("trùng");
    expect(validateItems([{ ...item, quantity: 0 }])).not.toBe("");
    expect(validateItems([{ ...item, discount: 201 }])).not.toBe("");
  });
  it("lưu lỗi một phần mang ID đơn, không tạo lại", async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ salesOrderId: 42 })))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ message: "Lỗi dòng" }), { status: 400 }),
      );
    vi.stubGlobal("fetch", fetcher);
    try {
      await saveOrder(null, {}, [
        { productId: 1, quantity: 1, unitPrice: 100, discount: 0 },
      ]);
      throw new Error("Expected failure");
    } catch (e) {
      expect(e).toBeInstanceOf(PartialOrderError);
      expect((e as PartialOrderError).orderId).toBe(42);
    }
    expect(fetcher).toHaveBeenCalledTimes(2);
  });
  it("phân quyền và trang mặc định", () => {
    expect(home("Admin")).toBe("/categories");
    expect(home("WarehouseManager")).toBe("/warehouses");
    expect(home("SalesStaff")).toBe("/orders");
    expect(allowed("SalesStaff", "products")).toBe(false);
    expect(allowed("WarehouseManager", "warehouses")).toBe(true);
    expect(allowed("Admin", "users")).toBe(true);
  });
  it("session lưu đúng storage, idle và hạn token", () => {
    vi.useFakeTimers();
    vi.setSystemTime(100000);
    const s: Session = {
      accessToken: "x",
      expiresAt: Date.now() + 2 * IDLE_MS,
      user: { userId: 1, email: "a@b.c", fullName: "A", role: "Admin" },
      rememberMe: false,
      lastActivityAt: Date.now(),
      lastRefreshAt: Date.now(),
    };
    saveSession(s);
    expect(readSession()).toEqual(s);
    expect(localStorage.length).toBe(0);
    vi.advanceTimersByTime(IDLE_MS);
    expect(sessionExpired(s)).toBe(true);
    saveSession({ ...s, rememberMe: true });
    expect(sessionStorage.length).toBe(0);
    expect(localStorage.length).toBe(1);
    saveSession(null);
    expect(readSession()).toBeNull();
  });
});
