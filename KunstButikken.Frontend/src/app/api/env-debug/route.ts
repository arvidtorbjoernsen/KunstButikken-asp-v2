import { NextResponse } from 'next/server';
import { getGatewayBase } from '@/shared/config';

export async function GET() {
  return NextResponse.json({
    KEYCLOAK_ISSUER: process.env.KEYCLOAK_ISSUER || 'NOT SET',
    NEXT_PUBLIC_API_USER: process.env.NEXT_PUBLIC_API_USER || 'NOT SET',
    NEXT_PUBLIC_API_AUTH: process.env.NEXT_PUBLIC_API_AUTH || 'NOT SET',
    NEXTAUTH_URL: process.env.NEXTAUTH_URL || 'NOT SET',
    NEXT_PUBLIC_API_GATEWAY: getGatewayBase(),
  });
}
