import { repairBrokenAccounts } from '@/shared/utils/repair-accounts';
import { NextResponse } from 'next/server';

export async function POST() {
  try {
    const result = await repairBrokenAccounts();
    return NextResponse.json(result);
  } catch (error) {
    return NextResponse.json(
      {
        success: false,
        message: error instanceof Error ? error.message : 'Failed to repair accounts',
      },
      { status: 500 }
    );
  }
}

export async function GET() {
  return NextResponse.json({
    message: 'Use POST to repair broken accounts',
    endpoint: '/api/repair-accounts',
  });
}
