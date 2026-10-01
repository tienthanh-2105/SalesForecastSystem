import { afterEach, describe, expect, it, vi } from "vitest";
import { allPages, api, ApiError, configureApi } from "../lib/api";
afterEach(() => {
  vi.unstubAllGlobals();
  configureApi("");
});
describe("API client", () => {
  it("401 của request phiên cũ không đăng xuất phiên mới", async () => {
    const callback = vi.fn();
    let finish!: (response: Response) => void;
    vi.stubGlobal(
      "fetch",
      vi.fn().mockImplementation(
        () =>
          new Promise<Response>((resolve) => {
            finish = resolve;
          }),
      ),
    );
    configureApi("old", callback);
    const request = api("/api/products");
    configureApi("new", callback);
    finish(new Response("{}", { status: 401 }));
    await expect(request).rejects.toThrow();
    expect(callback).not.toHaveBeenCalled();
  });
  it("gắn token và gửi JSON", async () => {
    const fetcher = vi.fn().mockResolvedValue(new Response("{}"));
    vi.stubGlobal("fetch", fetcher);
    configureApi("abc");
    await api("/api/products", { method: "POST", body: "{}" });
    const headers = fetcher.mock.calls[0][1].headers as Headers;
    expect(headers.get("Authorization")).toBe("Bearer abc");
    expect(headers.get("Content-Type")).toBe("application/json");
  });
  it("không gắn Content-Type cho FormData và xử lý 204", async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetcher);
    expect(
      await api("/api/products/images", {
        method: "POST",
        body: new FormData(),
      }),
    ).toBeNull();
    expect(fetcher.mock.calls[0][1].headers.has("Content-Type")).toBe(false);
  });
  it("đọc validation và không logout khi 403", async () => {
    const callback = vi.fn();
    configureApi("abc", callback);
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          new Response(
            JSON.stringify({ errors: { name: ["Tên bắt buộc."] } }),
            { status: 403 },
          ),
        ),
    );
    await expect(api("/api/products")).rejects.toThrow("Tên bắt buộc.");
    expect(callback).not.toHaveBeenCalled();
  });
  it("401 kết thúc session nhưng login sai không gọi callback", async () => {
    const callback = vi.fn();
    configureApi("abc", callback);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response("{}", { status: 401 })),
    );
    await expect(api("/api/auth/login")).rejects.toBeInstanceOf(ApiError);
    expect(callback).not.toHaveBeenCalled();
    await expect(api("/api/auth/me")).rejects.toThrow();
    expect(callback).toHaveBeenCalledOnce();
  });
  it("truyền lỗi mạng", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new Error("Mất kết nối")));
    await expect(api("/api/products")).rejects.toThrow("Mất kết nối");
  });
  it("tải đủ selection qua nhiều trang", async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ items: [1], totalPages: 2 })),
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ items: [2], totalPages: 2 })),
      );
    vi.stubGlobal("fetch", fetcher);
    expect(await allPages("/api/products?isActive=true")).toEqual([1, 2]);
    expect(fetcher.mock.calls[1][0]).toContain("page=2");
  });
});
