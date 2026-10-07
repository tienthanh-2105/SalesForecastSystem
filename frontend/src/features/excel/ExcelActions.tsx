import { useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Modal } from "../../components/Modal";
import { useToast } from "../../components/Toast";
import { api } from "../../lib/api";
import { schemas, MAX_FILE_SIZE, type ExcelSection } from "./schema";
import { exportResource } from "./service";
import { downloadWorkbook } from "./workbook";

interface ImportResult { totalRows: number; validRows: number; isValid: boolean; createdCount: number; errors: { row: number; column: string; message: string }[] }
export function ExcelActions({ section, params, total, disabled }: { section: ExcelSection; params: URLSearchParams; total: number; disabled: boolean }) {
  const [open, setOpen] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [result, setResult] = useState<ImportResult | null>(null);
  const [busy, setBusy] = useState(false);
  const running = useRef(false);
  const [error, setError] = useState("");
  const toast = useToast();
  const client = useQueryClient();
  const schema = schemas[section];
  const resource = `/api/${section === "products" ? "products" : "sales"}`;
  async function run(task: () => Promise<void>) {
    if (running.current) return;
    running.current = true; setBusy(true); setError("");
    try { await task(); } catch (e) { setError(e instanceof Error ? e.message : "Không xử lý được file."); }
    finally { running.current = false; setBusy(false); }
  }
  async function download(template: boolean) {
    const path = template ? `${resource}/template` : exportResource(section, params).replace("?", "/export?");
    const blob = await api<Blob>(path, {}, true);
    downloadWorkbook(await blob.arrayBuffer(), `${template ? "Mau_" : ""}${schema.sheet}.xlsx`);
    toast(template ? "Đã tải file mẫu." : "Đã xuất file Excel.");
  }
  async function upload(preview: boolean) {
    setResult(null);
    if (!file || !/\.xlsx$/i.test(file.name) || !file.size) throw new Error("Hãy chọn file .xlsx không rỗng.");
    if (file.size > MAX_FILE_SIZE) throw new Error("File vượt quá 5 MB.");
    const body = new FormData(); body.append("file", file);
    const response = await api<ImportResult>(`${resource}/import${preview ? "/preview" : ""}`, { method: "POST", body });
    setResult(response);
    if (!preview && response.isValid) {
      await client.invalidateQueries({ queryKey: ["list", section] });
      toast(`Đã nhập ${response.createdCount} ${schema.label}.`); setOpen(false); setFile(null);
    }
  }
  return <>
    <div className="excel-toolbar" aria-label={`Công cụ Excel ${schema.label}`}>
      <span className="excel-label"><i className="bi bi-file-earmark-spreadsheet" /> Excel</span>
      <div className="excel-actions">
        <button className="btn btn-light border" disabled={busy} onClick={() => { setOpen(true); setFile(null); setResult(null); setError(""); }}>Nhập Excel</button>
        <button className="btn btn-light border" disabled={busy || disabled || !total} onClick={() => void run(() => download(false))}>Xuất Excel</button>
        <button className="btn btn-outline-primary" disabled={busy} onClick={() => void run(() => download(true))}>Tải file mẫu</button>
      </div>
    </div>
    {error && !open && <div className="alert alert-danger" role="alert">{error}</div>}
    {open && <Modal title={`Nhập Excel ${schema.label}`} busy={busy} onClose={() => setOpen(false)} wide>
      <p>Kiểm tra file trước khi lưu. Toàn bộ file phải hợp lệ; hệ thống kiểm tra lại khi nhập và không ghi đè dữ liệu trùng.</p>
      <label htmlFor="excel-upload" className="form-label">File Excel (.xlsx)</label>
      <input id="excel-upload" className="form-control" type="file" accept=".xlsx" disabled={busy} onChange={e => { setFile(e.target.files?.[0] || null); setResult(null); setError(""); }} />
      <p className="small text-secondary mt-2">Tối đa 5 MB, 2000 dòng. Sheet {schema.sheet}, tiêu đề dòng 1. Đơn mới chỉ nhận trạng thái Nháp.</p>
      <details><summary>Quy định cột</summary><div className="table-responsive"><table className="table table-sm"><thead><tr><th>Cột</th><th>Quy định</th></tr></thead><tbody>{schema.columns.map(c => <tr key={c.key}><th>{c.label}{c.required ? " *" : ""}</th><td>{c.help}</td></tr>)}</tbody></table></div></details>
      {error && <div className="alert alert-danger mt-3" role="alert">{error}</div>}
      {busy && <p role="status">Đang xử lý file...</p>}
      {result && <div className="mt-3">
        <p role="status">{result.totalRows} dòng dữ liệu · {result.validRows} dòng hợp lệ · {result.errors.length} lỗi</p>
        {result.isValid ? <div className="alert alert-success">File hợp lệ. Có thể nhập vào hệ thống.</div> : <div className="excel-preview table-responsive"><table className="table table-bordered"><thead><tr><th>Dòng Excel</th><th>Cột</th><th>Lỗi</th></tr></thead><tbody>{result.errors.map((e, i) => <tr key={i}><td>{e.row}</td><td>{e.column}</td><td>{e.message}</td></tr>)}</tbody></table></div>}
      </div>}
      <div className="excel-modal-footer"><button className="btn btn-light border" disabled={busy} onClick={() => setOpen(false)}>Đóng</button><button className="btn btn-primary" disabled={busy || !file} onClick={() => void run(() => upload(true))}>Kiểm tra file</button><button className="btn btn-brand" disabled={busy || !result?.isValid || !result.totalRows} onClick={() => void run(() => upload(false))}>Nhập vào hệ thống</button></div>
    </Modal>}
  </>;
}
