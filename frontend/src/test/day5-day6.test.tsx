import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, expect, it, vi } from "vitest";
import { ExcelActions } from "../features/excel/ExcelActions";
import { ReportPage } from "../features/reports/ReportPage";
import { configureApi } from "../lib/api";
HTMLDialogElement.prototype.showModal = function () { this.setAttribute("open", ""); };
HTMLDialogElement.prototype.close = function () { this.removeAttribute("open"); };
afterEach(() => { cleanup(); vi.unstubAllGlobals(); configureApi(""); });
function wrap(component: React.ReactNode) { return render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>{component}</QueryClientProvider>); }
it("requires backend preview before importing and prevents duplicate submission", async () => {
  const fetcher = vi.fn().mockResolvedValue(new Response(JSON.stringify({ totalRows: 1, validRows: 1, isValid: true, createdCount: 1, errors: [] })));
  vi.stubGlobal("fetch", fetcher); configureApi("jwt");
  wrap(<ExcelActions section="products" params={new URLSearchParams()} total={1} disabled={false} />);
  fireEvent.click(screen.getByText("Nhập Excel"));
  const save = screen.getByText("Nhập vào hệ thống"); expect(save).toBeDisabled();
  fireEvent.change(screen.getByLabelText("File Excel (.xlsx)"), { target: { files: [new File(["xlsx"], "test.xlsx")] } });
  fireEvent.click(screen.getByText("Kiểm tra file"));
  await waitFor(() => expect(save).toBeEnabled());
  expect(fetcher.mock.calls[0][0]).toBe("/api/products/import/preview");
  expect(fetcher.mock.calls[0][1].body).toBeInstanceOf(FormData);
  expect(fetcher.mock.calls[0][1].headers.get("Authorization")).toBe("Bearer jwt");
  fireEvent.click(save); fireEvent.click(save);
  await waitFor(() => expect(fetcher).toHaveBeenCalledTimes(2));
  expect(fetcher.mock.calls[1][0]).toBe("/api/products/import");
});
it("invalidates preview on file change and displays backend row errors", async () => {
  vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({ totalRows: 1, validRows: 0, isValid: false, errors: [{ row: 2, column: "SKU", message: "SKU đã tồn tại" }] }))));
  wrap(<ExcelActions section="products" params={new URLSearchParams()} total={1} disabled={false} />);
  fireEvent.click(screen.getByText("Nhập Excel"));
  fireEvent.change(screen.getByLabelText("File Excel (.xlsx)"), { target: { files: [new File(["xlsx"], "test.xlsx")] } });
  fireEvent.click(screen.getByText("Kiểm tra file"));
  await screen.findByText("SKU đã tồn tại"); expect(screen.getByText("Nhập vào hệ thống")).toBeDisabled();
});
it("keeps missing FIFO cost unknown and sends repeated group member IDs without a single staff filter", async () => {
  const fetcher = vi.fn(async (url: string) => new Response(JSON.stringify(url.includes("sales-by-staff") ? [] : url.includes("sales-by-group") ? { groupName: "Nhóm bán hàng", revenue: 0, quantitySold: 0, orderCount: 0, members: [] } : { costMethod: "FIFO", totals: { revenue: 100, orderCount: 1, quantitySold: 2, costOfGoodsSold: null, grossProfit: null, missingCostQuantity: 2 }, periods: [] })));
  vi.stubGlobal("fetch", fetcher); wrap(<ReportPage />);
  await screen.findByText(/Thiếu giá vốn/); expect(screen.getAllByText("Chưa xác định")).toHaveLength(2);
  fireEvent.change(screen.getByLabelText("Mã nhân viên, cách nhau bằng dấu phẩy"), { target: { value: "1,2,1" } });
  fireEvent.click(screen.getByText("Xem doanh số nhóm"));
  await waitFor(() => expect(fetcher.mock.calls.some(([url]) => url.includes("sales-by-group"))).toBe(true));
  const url = fetcher.mock.calls.find(([url]) => url.includes("sales-by-group"))![0];
  const query = new URL(url, "http://test").searchParams;
  expect(query.getAll("StaffUserIds")).toEqual(["1", "2"]); expect(query.has("StaffUserId")).toBe(false);
});
