'use client';

import React, { useEffect, useMemo } from 'react';
import { Provider, useDispatch, useSelector } from 'react-redux';
import { RootState, store } from '@/shared/state/store';
import { SerializableUiAuction, setAuction, setError } from '@/features/auction/state/auctionSlice';
import Typography from '@mui/material/Typography';
import Divider from '@mui/material/Divider';
import Alert from '@mui/material/Alert';
import Paper from '@mui/material/Paper';
import Button from '@mui/material/Button';
import Link from 'next/link';
import type { UiAuction } from '@/features/auction/types/auction';
import { apiFetch } from '@/shared/api/api';
import * as signalR from '@microsoft/signalr';
import PlaceBidForm from '@/features/auction/components/PlaceBidForm';
import { useKeycloak } from '@/features/auth/lib/keycloak';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import { getGatewayBase } from '@/shared/config';

type ServerAuction = {
  id: string;
  artId: string;
  sellerId: string;
  startsAt: string;
  endsAt: string;
  startingPrice: number;
  reservePrice?: number | null;
  status: 'Draft' | 'Open' | 'Closed' | 0 | 1 | 2;
  bids: { id: string; amount: number; placedAt: string }[];
  winningBid?: number | null;
  winnerId?: string | null;
};

function map(a: ServerAuction): UiAuction {
  const startsAt = new Date(a.startsAt);
  const endsAt = new Date(a.endsAt);
  const highestBid = a.bids?.length ? Math.max(...a.bids.map(b => b.amount)) : undefined;
  const now = Date.now();
  const timeLeftMs = endsAt.getTime() - now;
  const hasStarted = startsAt.getTime() <= now;
  const hasEnded = timeLeftMs <= 0;
  const statusNorm = ((): 'Draft' | 'Open' | 'Closed' => {
    if (typeof a.status === 'number') {
      if (a.status === 1) return 'Open';
      if (a.status === 2) return 'Closed';
      return 'Draft';
    }
    return a.status;
  })();
  const isOpen = statusNorm === 'Open' && hasStarted && !hasEnded;
  const isClosed = statusNorm === 'Closed' || hasEnded;
  const reservePrice = a.reservePrice ?? undefined;
  const reserveMet = reservePrice == null || (highestBid != null && highestBid >= reservePrice);

  return {
    id: a.id,
    artId: a.artId,
    sellerId: a.sellerId,
    startsAt,
    endsAt,
    startingPrice: a.startingPrice,
    reservePrice,
    status: statusNorm,
    bidsCount: a.bids?.length ?? 0,
    highestBid,
    isOpen,
    isClosed,
    reserveMet,
    timeLeftMs,
    winningBid: a.winningBid,
    winnerId: a.winnerId,
  };
}

function useCountdown(target: Date | null | undefined) {
  const [now, setNow] = React.useState<number>(() => Date.now());
  const { t } = useTranslations();

  React.useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);

  if (!target) {
    return '...';
  }

  const ms = target.getTime() - now;
  if (ms <= 0) return t('auction.expired');

  const totalSeconds = Math.floor(ms / 1000);
  const d = Math.floor(totalSeconds / 86400);
  const h = Math.floor((totalSeconds % 86400) / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;

  if (d > 0) return `${d}d ${h}h ${m}m`;
  if (h > 0) return `${h}h ${m}m ${s}s`;
  return `${m}m ${s}s`;
}

// Helper function for currency formatting
function formatCurrencyNOK(n?: number) {
  if (n == null) return '—';
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'NOK' }).format(n);
  } catch {
    return `Kr ${n}`;
  }
}

// Small presentational helper to reduce complexity in AuctionView
function AuctionStatusBlock({
                              auction,
                              t,
                              isOwner,
                            }: {
  auction: UiAuction;
  t: any;
  isOwner: boolean;
}) {
  const formatDateTime = (date: Date) =>
    date.toLocaleString(undefined, {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      timeZoneName: 'short',
    });

  const startCountdown = useCountdown(auction?.startsAt);
  const endCountdown = useCountdown(auction?.endsAt);

  const now = Date.now();
  const hasStarted = auction.startsAt.getTime() <= now;

  return (
    <Paper sx={{ p: 2, mb: 2 }}>
      <Typography variant='subtitle1' sx={{ mb: 2 }}>
        {t('auction.liveStatus')}
      </Typography>
      {/* error is rendered by parent if present */}
      <Typography variant='body2' color='text.secondary'>
        {t('auction.status')}: {auction.status} | {t('auction.openStatus')}:{' '}
        {auction.isOpen ? t('common.yes') : t('common.no')}
      </Typography>
      <Typography variant='body2' color='text.secondary'>
        {t('auction.highestBid')}:{' '}
        {auction.highestBid != null ? formatCurrencyNOK(auction.highestBid) : '—'} |{' '}
        {t('auction.bids')}: {auction.bidsCount}
      </Typography>
      <Typography variant='body2' color='text.secondary'>
        {t('auction.reserve')}:{' '}
        {auction.reserveMet ? t('auction.reserveMetStatus') : t('auction.reserveNotMetStatus')}
        {isOwner &&
          auction.reservePrice &&
          ` (${t('auction.setReservePrice')}: ${formatCurrencyNOK(auction.reservePrice)})`}
      </Typography>

      {hasStarted ? (
        <Typography variant='body2' color='text.secondary' sx={{ mb: 2 }}>
          {t('auction.started')}: {formatDateTime(auction.startsAt)}
        </Typography>
      ) : (
        <Typography variant='body2' color='text.secondary' sx={{ mb: 2 }}>
          {t('auction.startsIn')}: {startCountdown} ({formatDateTime(auction.startsAt)})
        </Typography>
      )}

      {!auction.isClosed && (
        <Typography variant='body2' color='text.secondary' sx={{ mb: 2 }}>
          {t('auction.endsInCountdown')}: {endCountdown}
          {!hasStarted && ` (${formatDateTime(auction.endsAt)})`}
        </Typography>
      )}

      {auction.isClosed && auction.winningBid && auction.winnerId && (
        <Typography variant='body2' color='text.secondary' sx={{ mb: 2 }}>
          {t('auction.winner')}: {auction.winnerId} ({t('auction.winningBid')}:{' '}
          {formatCurrencyNOK(auction.winningBid)})
        </Typography>
      )}

      {isOwner && auction.bidsCount === 0 && (
        <Button
          component={Link}
          href={`/art/auctions/${auction.id}/edit`}
          variant='outlined'
          sx={{ mt: 2 }}
        >
          {t('auction.editAuction')}
        </Button>
      )}

      <Divider sx={{ my: 2 }} />
      <Typography variant='caption' color='text.secondary'>
        {t('auction.liveViaSignalR')}
      </Typography>
    </Paper>
  );
}

function AuctionView() {
  const { auction: serializableAuction, error } = useSelector((state: RootState) => state.auction);
  const { keycloak } = useKeycloak();
  const { t } = useTranslations();

  const auction = useMemo(() => {
    if (!serializableAuction) return null;
    return {
      ...serializableAuction,
      startsAt: new Date(serializableAuction.startsAt),
      endsAt: new Date(serializableAuction.endsAt),
    };
  }, [serializableAuction]);

  if (!auction) {
    return <Typography>{t('home.loading')}</Typography>;
  }

  const isOwner = keycloak?.tokenParsed?.sub === auction.sellerId;

  return (
    <>
      {error && (
        <Alert severity='warning' sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      <AuctionStatusBlock auction={auction} t={t} isOwner={isOwner} />
      <Paper sx={{ p: 2 }}>
        <PlaceBidForm
          auctionId={auction.id}
          currentHighest={auction.highestBid}
          startingPrice={auction.startingPrice}
          isOpen={auction.isOpen}
          startsAt={auction.startsAt}
          endsAt={auction.endsAt}
          status={auction.status}
          auctionSellerId={auction.sellerId}
        />
      </Paper>
    </>
  );
}

function AuctionProvider({ auctionId, initial }: { auctionId: string; initial: UiAuction }) {
  const dispatch = useDispatch();

  const dispatchAuction = (auction: UiAuction) => {
    const serializablePayload: SerializableUiAuction = {
      ...auction,
      startsAt: auction.startsAt.toISOString(),
      endsAt: auction.endsAt.toISOString(),
    };
    dispatch(setAuction(serializablePayload));
  };

  useEffect(() => {
    dispatchAuction(initial);
  }, [dispatch, initial]);

  useEffect(() => {
    let active = true;
    const interval = setInterval(async () => {
      try {
        const data = await apiFetch<ServerAuction>(
          'AUCTION',
          `/${auctionId}`,
          { method: 'GET' },
          false,
        );
        if (!active) return;
        dispatchAuction(map(data));
      } catch (e) {
        if (!active) return;
        dispatch(setError('Failed to refresh auction'));
      }
    }, 8000);
    return () => {
      active = false;
      clearInterval(interval);
    };
  }, [auctionId, dispatch]);

  useEffect(() => {
    let connection: signalR.HubConnection | null = null;
    const gatewayBase = getGatewayBase();
    if (!gatewayBase) {
      dispatch(setError('Live hub URL not configured'));
      return;
    }
    const hubUrl = `${gatewayBase}/hubs/auctions`;

    async function start() {
      connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl as string, {
          // Use accessTokenFactory to supply Bearer token for hubs protected by JWT
          accessTokenFactory: async (): Promise<string> => {
            try {
              const { getKeycloakToken } = await import('@/features/auth/lib/keycloak-client');
              const token = getKeycloakToken?.();
              return token ?? '';
            } catch (e) {
              return '';
            }
          },
          // Prefer WebSockets transport; fallback handled by SignalR automatically if necessary
          transport: signalR.HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => 3000 })
        .build();
      connection.onclose(err => {
        if (err) dispatch(setError('Live connection lost, retrying...'));
      });
      connection.onreconnecting(() => dispatch(setError('Reconnecting to live updates...')));
      connection.onreconnected(() => dispatch(setError(null)));
      connection.on('auctionUpdated', (a: ServerAuction) => {
        if (a && a.id === auctionId) dispatchAuction(map(a));
      });
      connection.on('auctionClosed', (a: ServerAuction) => {
        if (a && a.id === auctionId) dispatchAuction(map(a));
      });
      try {
        await connection.start();
        await connection.invoke('JoinAuction', auctionId);
        dispatch(setError(null));
      } catch (err: any) {
        dispatch(setError(`Live connection failed: ${err?.message || 'unknown error'}`));
      }
    }

    start();
    return () => {
      (async () => {
        try {
          await connection?.invoke('LeaveAuction', auctionId);
        } catch {
        }
        try {
          await connection?.stop();
        } catch {
        }
      })();
    };
  }, [auctionId, dispatch]);

  return <AuctionView />;
}

export default function AuctionLiveClient({
                                            auctionId,
                                            initial,
                                          }: {
  auctionId: string;
  initial: UiAuction;
}) {
  return (
    <Provider store={store}>
      <AuctionProvider auctionId={auctionId} initial={initial} />
    </Provider>
  );
}
