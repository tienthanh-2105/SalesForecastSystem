let token = "";
let unauthorized: (() => void) | undefined;
export function configureApi(value: string, onUnauthorized?: () => void) {
  token = value;
  unauthorized = onUnauthorized;
}
export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}
export async function api<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers);
  if (token && !headers.has("Authorization"))
    headers.set("Authorization", `Bearer ${token}`);
  if (options.body && !(options.body instanceof FormData))
    headers.set("Content-Type", "application/json");
  const response = await fetch(path, {
    ...options,
    headers,
    cache: "no-store",
  });
  const data =
    response.status === 204 ? null : await response.json().catch(() => null);
  if (!response.ok) {
    if (
      response.status === 401 &&
      path !== "/api/auth/login" &&
      path !== "/api/auth/logout" &&
      token &&
      headers.get("Authorization") === `Bearer ${token}`
    )
      unauthorized?.();
    const message = data?.errors
      ? Object.values(data.errors).flat().join(" ")
      : data?.detail || data?.title || data?.message;
    throw new ApiError(
      response.status,
      message || `Yêu cầu thất bại (${response.status}).`,
    );
  }
  return data as T;
}
export const send = <T>(path: string, method: string, payload?: unknown) =>
  api<T>(path, {
    method,
    body: payload === undefined ? undefined : JSON.stringify(payload),
  });
export async function allPages<T>(resource: string): Promise<T[]> {
  const result: T[] = [];
  for (let page = 1; ; page++) {
    const data = await api<import("../types").Page<T>>(
      `${resource}${resource.includes("?") ? "&" : "?"}page=${page}&pageSize=100`,
    );
    result.push(...data.items);
    if (page >= data.totalPages || !data.items.length) return result;
  }
}
