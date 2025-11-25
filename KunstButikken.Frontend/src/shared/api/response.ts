/**
 * Shared response parsing helper
 * Prefer JSON parsing; fall back to text for non-JSON responses.
 */
export async function parseResponse<T>(res: Response): Promise<T> {
  const contentType = res.headers.get('Content-Type') || '';
  if (contentType.includes('application/json')) {
    try {
      return (await res.json()) as T;
    } catch {
      throw new Error('Failed to parse JSON response');
    }
  }
  const text = await res.text();
  return text as unknown as T;
}
