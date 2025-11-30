import { parseResponse } from '@/shared/api';
import type { ApiAuction } from '../types/auction';

const gateway =
  process.env.NEXT_PUBLIC_API_GATEWAY ||
  process.env.NEXT_PUBLIC_API_BASE_URL ||
  'http://localhost:5000';

export async function getAuctionsServer(status?: 'Open' | 'Closed' | 'Draft'): Promise<ApiAuction[]> {
  const base = gateway.replace(/\/$/, '');
  const qs = status ? `?status=${encodeURIComponent(status)}` : '';
  const url = `${base}/api/auctions${qs}`;
  const res = await fetch(url, { next: { revalidate: 15 } });
  if (!res.ok) return [];
  const data = await parseResponse<ApiAuction[]>(res);
  return Array.isArray(data) ? data : [];
}

export async function getAuctionByIdServer(id: string): Promise<ApiAuction | null> {
  const base = gateway.replace(/\/$/, '');
  const url = `${base}/api/auctions/${encodeURIComponent(id)}`;
  const res = await fetch(url, { next: { revalidate: 5 } });
  if (!res.ok) return null;
  const data = await parseResponse<ApiAuction>(res);
  return data?.id ? data : null;
}
