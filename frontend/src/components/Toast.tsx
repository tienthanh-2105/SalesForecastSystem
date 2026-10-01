import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
const Context = createContext<(message: string) => void>(() => {});
export function ToastProvider({ children }: { children: ReactNode }) {
  const [message, setMessage] = useState("");
  useEffect(() => {
    if (!message) return;
    const timer = setTimeout(() => setMessage(""), 4500);
    return () => clearTimeout(timer);
  }, [message]);
  return (
    <Context.Provider value={setMessage}>
      {children}
      {message && (
        <div className="react-toast" role="status">
          {message}
        </div>
      )}
    </Context.Provider>
  );
}
export const useToast = () => useContext(Context);
