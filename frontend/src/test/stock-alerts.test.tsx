import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, expect, it, vi } from "vitest";
import { StockAlerts } from "../features/reports/StockAlerts";

afterEach(() => { cleanup(); vi.unstubAllGlobals(); vi.useRealTimers(); });
HTMLDialogElement.prototype.showModal = function () { this.setAttribute("open", ""); };
HTMLDialogElement.prototype.close = function () { this.removeAttribute("open"); };

function mount() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={client}><StockAlerts /></QueryClientProvider>);
  return client;
}

it("shows stock warnings in a dialog, auto closes, and reminds after 30 minutes", async () => {
  vi.useFakeTimers({ shouldAdvanceTime: true });
  vi.setSystemTime(new Date("2026-10-08T01:00:00Z"));
  const items = [{ productId: 1, sku: "HOT", productName: "Sản phẩm A", quantityOnHand: 0, minimumStockLevel: 5, quantitySold: 20, isBestSeller: true, isOutOfStock: true }];
  vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify({ checkedAtUtc: "2026-10-08T01:00:00Z", items }))));
  const client = mount();
  expect(await screen.findByRole("dialog", { name: "Cảnh báo tồn kho" })).toBeInTheDocument();
  expect(screen.getByText("Bán chạy")).toBeInTheDocument();
  expect(screen.getByText("Hết hàng")).toBeInTheDocument();

  await act(async () => { await vi.advanceTimersByTimeAsync(15_000); });
  expect(screen.queryByRole("dialog", { name: "Cảnh báo tồn kho" })).not.toBeInTheDocument();
  await act(async () => { await vi.advanceTimersByTimeAsync(29 * 60_000); });
  expect(screen.queryByRole("dialog", { name: "Cảnh báo tồn kho" })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /Thông báo/ }));
  fireEvent.click(screen.getByRole("button", { name: "Xem cảnh báo" }));
  expect(screen.getByRole("dialog", { name: "Cảnh báo tồn kho" })).toBeInTheDocument();
  client.clear();
});

it("does not remind after the next check clears the warnings", async () => {
  vi.useFakeTimers({ shouldAdvanceTime: true });
  vi.setSystemTime(new Date("2026-10-08T01:00:00Z"));
  let items: typeof stockWarning[] = [stockWarning];
  vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify({ checkedAtUtc: new Date().toISOString(), items }))));
  const client = mount();
  expect(await screen.findByRole("dialog", { name: "Cảnh báo tồn kho" })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Đóng" }));
  items = [];
  await act(async () => { await vi.advanceTimersByTimeAsync(60_000); });
  expect(screen.queryByRole("dialog", { name: "Cảnh báo tồn kho" })).not.toBeInTheDocument();
  await act(async () => { await vi.advanceTimersByTimeAsync(30 * 60_000); });
  expect(screen.queryByRole("dialog", { name: "Cảnh báo tồn kho" })).not.toBeInTheDocument();
  client.clear();
});

const stockWarning = { productId: 1, sku: "HOT", productName: "Sản phẩm A", quantityOnHand: 0, minimumStockLevel: 5, quantitySold: 20, isBestSeller: true, isOutOfStock: true };

it("reports a failed check instead of claiming stock is healthy", async () => {
  vi.stubGlobal("fetch", vi.fn(async () => new Response("{}", { status: 500 })));
  const client = mount();
  fireEvent.click(screen.getByRole("button", { name: "Thông báo" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Không thể cập nhật cảnh báo");
  expect(screen.queryByRole("dialog", { name: "Cảnh báo tồn kho" })).not.toBeInTheDocument();
  client.clear();
});
