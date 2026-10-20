import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, expect, it, vi } from "vitest";
import { ExcelActions } from "../features/excel/ExcelActions";
import { ReportPage } from "../features/reports/ReportPage";
import { SalesAnalysisPage } from "../features/reports/SalesAnalysisPage";
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
it("shows revenue charts and preserves unknown FIFO cost in the report list", async () => {
  const fetcher = vi.fn(async (url: string) => {
    if (url.includes("sales-trend")) return new Response(JSON.stringify([{ periodStart: "2026-10-01", revenue: 100, quantitySold: 2, orderCount: 1 }]));
    if (url.includes("/api/categories")) return new Response(JSON.stringify([]));
    if (url.includes("/api/warehouses")) return new Response(JSON.stringify({ items: [], totalPages: 0 }));
    if (url.includes("/api/products")) return new Response(JSON.stringify({ items: [], totalPages: 0 }));
    return new Response(JSON.stringify({ costMethod: "FIFO", totals: { revenue: 100, orderCount: 1, quantitySold: 2, costOfGoodsSold: null, grossProfit: null, missingCostQuantity: 2 }, periods: [{ periodStart: "2026-10-01", totals: { revenue: 100, orderCount: 1, quantitySold: 2, costOfGoodsSold: null, grossProfit: null, missingCostQuantity: 2 } }] }));
  });
  vi.stubGlobal("fetch", fetcher); wrap(<ReportPage />);
  await screen.findByText(/Thiếu giá vốn/);
  expect(screen.getByRole("img", { name: "Biểu đồ cột doanh thu theo thời gian" })).toBeInTheDocument();
  expect(screen.getByRole("img", { name: "Biểu đồ tròn cơ cấu doanh thu" })).toBeInTheDocument();
  expect(screen.getAllByText("Chưa xác định").length).toBeGreaterThanOrEqual(2);
});

it("filters sales analysis and staff group with the page-specific date and catalog filters", async () => {
  const fetcher = vi.fn(async (url: string) => {
    if (url.includes("sales-by-group")) return new Response(JSON.stringify({ groupName: "Nhóm bán hàng", revenue: 0, quantitySold: 0, orderCount: 0, members: [] }));
    if (url.includes("top-products")) return new Response(JSON.stringify([{ productId: 1, sku: "SKU-1", productName: "Sản phẩm A", quantitySold: 2, orderCount: 1, revenue: 100 }]));
    if (url.includes("sales-by-staff")) return new Response(JSON.stringify([{ userId: 1, fullName: "Nhân viên A", quantitySold: 2, orderCount: 1, revenue: 100 }]));
    if (url.includes("/api/categories")) return new Response(JSON.stringify([]));
    if (url.includes("/api/warehouses") || url.includes("/api/products")) return new Response(JSON.stringify({ items: [], totalPages: 0 }));
    return new Response(JSON.stringify([]));
  });
  vi.stubGlobal("fetch", fetcher); wrap(<SalesAnalysisPage />);
  expect(await screen.findByText("Sản phẩm A")).toBeInTheDocument();
  expect((await screen.findAllByText("Nhân viên A")).length).toBeGreaterThan(0);
  fireEvent.click(screen.getByText("Chọn nhân viên theo tên"));
  fireEvent.click(screen.getByLabelText("Nhân viên A"));
  fireEvent.click(screen.getByText("Xem doanh số nhóm"));
  await waitFor(() => expect(fetcher.mock.calls.some(([url]) => url.includes("sales-by-group"))).toBe(true));
  const url = fetcher.mock.calls.find(([url]) => url.includes("sales-by-group"))![0];
  const query = new URL(url, "http://test").searchParams;
  expect(query.getAll("StaffUserIds")).toEqual(["1"]); expect(query.has("StaffUserId")).toBe(false);
});
