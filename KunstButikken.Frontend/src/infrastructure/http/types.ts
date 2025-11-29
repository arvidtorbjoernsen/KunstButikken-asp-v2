export interface ApiClient {
  get<T>(url: string, options?: RequestInit): Promise<T>;
  post<T>(url: string, body?: unknown, options?: RequestInit): Promise<T>;
  put<T>(url: string, body?: unknown, options?: RequestInit): Promise<T>;
  delete<T>(url: string, options?: RequestInit): Promise<T>;
  patch<T>(url: string, body?: unknown, options?: RequestInit): Promise<T>;
}

