import { getGatewayBase } from '@/shared/config';

export interface GatewaySessionResponse {
  sessionId: string;
  userId: string;
  roles: string[];
  expiresAt: string;
}

export async function exchangeCodeForGatewaySession(code: string, redirectUri: string, signal?: AbortSignal): Promise<GatewaySessionResponse> {
  const response = await fetch(`${getGatewayBase()}/auth/session`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ code, redirectUri }),
    signal,
  });

  if (!response.ok) {
    const text = await response.text().catch(() => '');
    throw new Error(`Failed to exchange auth code (${response.status}): ${text || response.statusText}`);
  }

  return (await response.json()) as GatewaySessionResponse;
}

