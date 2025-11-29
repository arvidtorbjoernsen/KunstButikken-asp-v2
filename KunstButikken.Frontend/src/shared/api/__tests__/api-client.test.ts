import { setKeycloakTokenGetter, apiFetch, apiClient } from '@/shared/api/api-client';
import { getGatewayBase } from '@/shared/config';
import { parseResponse } from '@/shared/api/response';

jest.mock('@/shared/api/response');

describe('api-client', () => {
  const origFetch = global.fetch;
  beforeEach(() => {
    jest.resetModules();
    // @ts-ignore
    global.fetch = jest.fn();
    (parseResponse as jest.Mock).mockImplementation(async (r: any) => {
      if (r.json) return r.json();
      if (r.text) return r.text();
      return {};
    });
  });
  afterEach(() => {
    // @ts-ignore
    global.fetch = origFetch;
    (parseResponse as jest.Mock).mockReset();
  });

  test('apiFetch attaches token for API requests', async () => {
    const gateway = getGatewayBase();
    const url = `${gateway}/api/some`;
    setKeycloakTokenGetter(() => 'abc123');

    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: true, status: 200, json: async () => ({ ok: true }), text: async () => '{}' });

    await apiFetch(url, { headers: { 'X-Custom': 'v' } });

    expect(global.fetch).toHaveBeenCalled();
    const calledArgs = (global.fetch as jest.Mock).mock.calls[0];
    const passedHeaders = calledArgs[1].headers;
    expect(passedHeaders.get('Authorization')).toBe('Bearer abc123');
    expect(passedHeaders.get('X-Custom')).toBe('v');
  });

  test('apiFetch skips token for external requests', async () => {
    setKeycloakTokenGetter(() => 'abc123');
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: true, status: 200, json: async () => ({}) });

    await apiFetch('https://example.com/resource');
    const passedHeaders = (global.fetch as jest.Mock).mock.calls[0][1].headers;
    expect(passedHeaders.get('Authorization')).toBeNull();
  });

  test('apiClient.get throws on non-ok response and returns parsed body on ok', async () => {
    setKeycloakTokenGetter(() => undefined);

    // non-ok
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: false, status: 500, statusText: 'Err' });
    await expect(apiClient.get('/api/x')).rejects.toThrow('HTTP 500: Err');

    // ok
    const jsonBody = { a: 1 };
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: true, status: 200, json: async () => jsonBody, headers: new Headers({ 'content-type': 'application/json' }) });
    const res = await apiClient.get('/api/x');
    expect(res).toEqual(jsonBody);
  });

  test('apiClient.post/put/patch attach content-type and body and parse response', async () => {
    setKeycloakTokenGetter(() => 'tok');
    const gateway = getGatewayBase();
    const url = `${gateway}/api/resource`;

    const body = { foo: 'bar' };

    // Prepare fetch mock for post
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: true, status: 201, json: async () => ({ id: 1 }), headers: new Headers({ 'content-type': 'application/json' }) });
    const p = await apiClient.post(url, body);
    expect(p).toEqual({ id: 1 });
    const postCall = (global.fetch as jest.Mock).mock.calls[0];
    const postHeaders = postCall[1].headers;
    expect(postHeaders.get('Content-Type')).toBe('application/json');
    expect(postCall[1].body).toBe(JSON.stringify(body));

    // Put
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ ok: true }), headers: new Headers({ 'content-type': 'application/json' }) });
    const putRes = await apiClient.put(url, body);
    expect(putRes).toEqual({ ok: true });

    // Patch
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ patched: true }), headers: new Headers({ 'content-type': 'application/json' }) });
    const patchRes = await apiClient.patch(url, { patched: true });
    expect(patchRes).toEqual({ patched: true });
  });

  test('apiClient.delete throws on error and returns parsed on ok', async () => {
    setKeycloakTokenGetter(() => undefined);
    // non-ok delete
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: false, status: 404, statusText: 'Not Found' });
    await expect(apiClient.delete('/api/x')).rejects.toThrow('HTTP 404: Not Found');

    // ok delete with text response
    // @ts-ignore
    global.fetch.mockResolvedValueOnce({ ok: true, status: 200, text: async () => 'gone', headers: new Headers({ 'content-type': 'text/plain' }) });
    (parseResponse as jest.Mock).mockImplementationOnce(async (r: any) => {
      return r.text();
    });
    const res = await apiClient.delete('/api/x');
    expect(res).toBe('gone');
  });

  test('parseResponse throws on invalid json and returns text otherwise', async () => {
    // build response with application/json but json() throws
    const badRes: any = {
      headers: new Headers({ 'Content-Type': 'application/json' }),
      json: async () => { throw new Error('bad'); },
      text: async () => 'plain',
    };

    const realParse = jest.requireActual('@/shared/api/response').parseResponse;
    await expect(realParse(badRes)).rejects.toThrow('Failed to parse JSON response');

    const textRes: any = { headers: new Headers({ 'Content-Type': 'text/plain' }), json: async () => ({}) , text: async () => 'ok' };
    const t = await realParse(textRes);
    expect(t).toBe('ok');
  });
});
