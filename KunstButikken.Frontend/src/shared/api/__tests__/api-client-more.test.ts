import { setKeycloakTokenGetter, apiFetch, apiClient } from '@/shared/api/api-client';

const origFetch = global.fetch;
beforeEach(() => {
  jest.resetModules();
  // @ts-ignore
  global.fetch = jest.fn();
});
afterEach(() => {
  // @ts-ignore
  global.fetch = origFetch;
});

describe('api-client additional', () => {
  test('post with undefined body still sets content-type and no body stringified', async () => {
    setKeycloakTokenGetter(() => 'tok');
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: true, status: 201, json: async () => ({ ok: true }), headers: new Headers({ 'content-type': 'application/json' }) });
    const res = await apiClient.post('/api/foo', undefined as any);
    expect(res).toEqual({ ok: true });
    const call = (global.fetch as jest.Mock).mock.calls[0];
    const headers = call[1].headers;
    expect(headers.get('Content-Type')).toBe('application/json');
    expect(call[1].body).toBeUndefined();
  });

  test('delete throws when response not ok and uses parseResponse when ok with text', async () => {
    setKeycloakTokenGetter(() => undefined);
    // not ok
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: false, status: 403, statusText: 'Forbidden' });
    await expect(apiClient.delete('/api/x')).rejects.toThrow('HTTP 403: Forbidden');

    // ok text
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: true, status: 200, text: async () => 'gone', headers: new Headers({ 'content-type': 'text/plain' }) });
    const res = await apiClient.delete('/api/x');
    expect(res).toBe('gone');
  });
});

