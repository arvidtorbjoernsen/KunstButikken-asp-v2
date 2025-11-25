'use client';

import React, { useState } from 'react';
import Box from '@mui/material/Box';
import TextField from '@mui/material/TextField';
import Button from '@mui/material/Button';
import Alert from '@mui/material/Alert';
import Typography from '@mui/material/Typography';
import { apiFetch } from '@/shared/api/api';
import { useKeycloak } from '@/features/auth/lib/keycloak';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';

// Helper function for currency formatting
function formatCurrencyNOK(n?: number) {
  if (n == null) return '—';
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'NOK' }).format(n);
  } catch {
    return `Kr ${n}`;
  }
}

export default function PlaceBidForm({
  auctionId,
  currentHighest,
  startingPrice,
  isOpen,
  startsAt,
  auctionSellerId, // New prop
}: {
  auctionId: string;
  currentHighest?: number;
  startingPrice: number;
  isOpen: boolean;
  startsAt?: Date;
  endsAt?: Date;
  status?: string;
  auctionSellerId: string; // New prop
}) {
  const { authenticated, isBuyer, isSeller, login, keycloak } = useKeycloak();
  const { t } = useTranslations();
  const [amount, setAmount] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const minBid = currentHighest != null ? currentHighest + 0.01 : startingPrice;
  const now = Date.now();
  const hasStarted = startsAt ? startsAt.getTime() <= now : true;

  const currentUserId = keycloak?.tokenParsed?.sub;
  const isAuctionOwner = authenticated && currentUserId === auctionSellerId;

  if (!authenticated) {
    return (
      <Box>
        <Typography variant="subtitle1" sx={{ mb: 2 }}>
          {t('auction.placeBid')}
        </Typography>
        <Alert severity="info" sx={{ mb: 2 }}>
          {t('auction.signInToBid')}
        </Alert>
        <Button variant="contained" fullWidth onClick={login}>
          {t('nav.signin')}
        </Button>
      </Box>
    );
  }

  if (isAuctionOwner) {
    return (
      <Alert severity="info" sx={{ mb: 2 }}>
        {t('auction.cannotBidOnOwnAuction') ?? "You cannot place a bid on your own auction."}
      </Alert>
    );
  }

  if (!isBuyer && !isSeller) {
    return <Alert severity="warning">{t('auction.onlyBuyersAndSellers')}</Alert>;
  }

  // If the auction is not open, show the correct message.
  if (!isOpen) {
    // Case 1: The auction has not started yet.
    if (!hasStarted && startsAt) {
      const date = startsAt.toLocaleDateString(undefined, {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      });
      const time = startsAt.toLocaleTimeString(undefined, {
        hour: '2-digit',
        minute: '2-digit',
        timeZoneName: 'short',
      });
      return (
        <Alert severity="info">
          This auction hasn't started yet. It starts on {date} at {time}.
        </Alert>
      );
    }
    
    // Case 2: The auction is unavailable for any other reason (ended, closed, etc.).
    return <Alert severity="info">{t('auction.auctionClosed')}</Alert>;
  }

  // If we reach here, the auction is open and the bid form can be displayed.
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    const bidAmount = parseFloat(amount);
    if (isNaN(bidAmount) || bidAmount <= 0) {
      setError(t('auction.enterValidAmount'));
      return;
    }

    if (bidAmount <= minBid) {
      setError(`${t('auction.bidMustBeGreater')} ${formatCurrencyNOK(minBid)}`);
      return;
    }

    setLoading(true);
    try {
      await apiFetch('AUCTION', `/${auctionId}/bid`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(bidAmount),
      });
      setSuccess(t('auction.bidPlaced'));
      setAmount('');
    } catch (err: any) {
      setError(err?.message || t('auction.failedToPlaceBid'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit}>
      <Typography variant="subtitle1" sx={{ mb: 2 }}>
        {t('auction.placeBid')}
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}
      {success && (
        <Alert severity="success" sx={{ mb: 2 }}>
          {success}
        </Alert>
      )}

      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
        {t('auction.minBid')}: {formatCurrencyNOK(minBid)}
      </Typography>

      <TextField
        fullWidth
        type="number"
        label={t('auction.yourBidAmount')}
        value={amount}
        onChange={e => setAmount(e.target.value)}
        inputProps={{ step: '0.01', min: minBid }}
        disabled={loading}
        sx={{ mb: 2 }}
      />

      <Button type="submit" variant="contained" fullWidth disabled={loading || !amount}>
        {loading ? t('auction.placingBid') : t('auction.placeBid')}
      </Button>
    </Box>
  );
}
