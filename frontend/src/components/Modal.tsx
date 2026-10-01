import { useEffect, useRef, type ReactNode } from "react";
export function Modal({
  title,
  onClose,
  children,
  wide = false,
  busy = false,
}: {
  title: string;
  onClose: () => void;
  children: ReactNode;
  wide?: boolean;
  busy?: boolean;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const dialog = ref.current!;
    dialog.showModal();
    return () => dialog.close();
  }, []);
  return (
    <dialog
      ref={ref}
      aria-label={title}
      onClick={(event) => {
        if (busy || event.target !== ref.current) return;
        const bounds = ref.current!.getBoundingClientRect();
        if (
          event.clientX < bounds.left ||
          event.clientX > bounds.right ||
          event.clientY < bounds.top ||
          event.clientY > bounds.bottom
        )
          onClose();
      }}
      className={`react-modal ${wide ? "wide" : ""}`}
      onCancel={(e) => {
        e.preventDefault();
        if (!busy) onClose();
      }}
    >
      <header className="d-flex justify-content-between align-items-center p-3 border-bottom">
        <h2 className="fs-5 m-0">{title}</h2>
        <button
          type="button"
          className="btn-close"
          aria-label="Đóng"
          disabled={busy}
          onClick={onClose}
        />
      </header>
      <div className="p-3">{children}</div>
    </dialog>
  );
}
