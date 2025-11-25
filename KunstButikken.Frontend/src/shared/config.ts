// Minimal centralized frontend config helpers for AuthGateway

export type ServiceName = 'AUTH' | 'USER' | 'ART' | 'AUCTION' | 'PAYMENT' | 'ADMIN';

export function getGatewayBase(): string {
  return (process.env.NEXT_PUBLIC_API_GATEWAY || 'http://localhost:5000').replace(/\/$/, '');
}

const servicePathMap: Record<ServiceName, string> = {
  AUTH: '/auth',
  USER: '/api/profile',
  ART: '/api/art',
  AUCTION: '/api/auctions',
  PAYMENT: '/api/payments',
  ADMIN: '/api/admin',
};

export function getServicePath(service: ServiceName): string {
  return servicePathMap[service];
}

export function buildServiceUrl(service: ServiceName, path = ''): string {
  const base = getGatewayBase();
  const svcPath = getServicePath(service).replace(/\/$/, '');
  const cleanPath = path ? (path.startsWith('/') ? path : `/${path}`) : '';
  return `${base}${svcPath}${cleanPath}`;
}
