import { getGatewayBase, getServicePath, buildServiceUrl } from '../config';

describe('config helpers', () => {
  test('getGatewayBase fallback and buildServiceUrl behavior', () => {
    const orig = process.env.NEXT_PUBLIC_API_GATEWAY;
    delete process.env.NEXT_PUBLIC_API_GATEWAY;
    expect(getGatewayBase()).toMatch(/localhost/);

    // test buildServiceUrl and service path trimming
    process.env.NEXT_PUBLIC_API_GATEWAY = 'https://api.example.com/';
    const url = buildServiceUrl('ART', 'list');
    expect(url).toBe('https://api.example.com/api/art/list');

    // path with leading slash handled
    const url2 = buildServiceUrl('AUCTION', '/123');
    expect(url2).toBe('https://api.example.com/api/auctions/123');

    // empty path returns base
    const url3 = buildServiceUrl('AUTH', '');
    expect(url3).toBe('https://api.example.com/auth');

    // restore
    process.env.NEXT_PUBLIC_API_GATEWAY = orig;
  });

  test('getServicePath returns correct mapped path', () => {
    expect(getServicePath('ART')).toBe('/api/art');
    expect(getServicePath('AUTH')).toBe('/auth');
  });
});
