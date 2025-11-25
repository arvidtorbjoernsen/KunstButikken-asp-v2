'use client';

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { apiFetch } from '@/shared/api/api';
import { UiAuction } from '@/features/auction/types/auction';
import {
  Container,
  Typography,
  TextField,
  Button,
  Box,
  Paper,
  Alert,
} from '@mui/material';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';

const schema = z.object({
  startsAt: z.date(),
  endsAt: z.date(),
  startingPrice: z.number().min(0),
  reservePrice: z.number().min(0).optional(),
});

type FormData = z.infer<typeof schema>;

export default function EditAuctionPage({ params }: { params: { id: string } }) {
  const router = useRouter();
  const { t } = useTranslations();
  const [auction, setAuction] = useState<UiAuction | null>(null);
  const [error, setError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  useEffect(() => {
    const fetchAuction = async () => {
      try {
        const data = await apiFetch<UiAuction>('AUCTION', `/${params.id}`);
        setAuction(data);
        reset({
          startsAt: new Date(data.startsAt),
          endsAt: new Date(data.endsAt),
          startingPrice: data.startingPrice,
          reservePrice: data.reservePrice,
        });
      } catch (err) {
        setError(t('auction.failedToFetchAuctionData'));
      }
    };
    fetchAuction();
  }, [params.id, reset, t]);

  const onSubmit = async (data: FormData) => {
    try {
      await apiFetch('AUCTION', `/${params.id}`, {
        method: 'PUT',
        body: JSON.stringify(data),
      });
      router.push(`/art/auctions/${params.id}`);
    } catch (err: any) {
      setError(err.message || t('auction.failedToUpdateAuction'));
    }
  };

  if (!auction) {
    return <Typography>{t('home.loading')}</Typography>;
  }

  return (
    <Container maxWidth="md" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          {t('auction.editAuction')}
        </Typography>
        {error && <Alert severity="error">{error}</Alert>}
        <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ mt: 4 }}>
          <Controller
            name="startsAt"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label={t('auction.startDate')}
                type="datetime-local"
                fullWidth
                margin="normal"
                InputLabelProps={{ shrink: true }}
                error={!!errors.startsAt}
                helperText={errors.startsAt?.message}
                value={field.value ? new Date(field.value).toISOString().slice(0, 16) : ''}
                onChange={(e) => field.onChange(new Date(e.target.value))}
              />
            )}
          />
          <Controller
            name="endsAt"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label={t('auction.endDate')}
                type="datetime-local"
                fullWidth
                margin="normal"
                InputLabelProps={{ shrink: true }}
                error={!!errors.endsAt}
                helperText={errors.endsAt?.message}
                value={field.value ? new Date(field.value).toISOString().slice(0, 16) : ''}
                onChange={(e) => field.onChange(new Date(e.target.value))}
              />
            )}
          />
          <Controller
            name="startingPrice"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label={t('auction.startingPriceLabel')}
                type="number"
                fullWidth
                margin="normal"
                error={!!errors.startingPrice}
                helperText={errors.startingPrice?.message}
                onChange={(e) => field.onChange(parseFloat(e.target.value))}
              />
            )}
          />
          <Controller
            name="reservePrice"
            control={control}
            render={({ field }) => (
              <TextField
                {...field}
                label={t('auction.reservePriceLabel')}
                type="number"
                fullWidth
                margin="normal"
                error={!!errors.reservePrice}
                helperText={errors.reservePrice?.message}
                onChange={(e) => field.onChange(parseFloat(e.target.value))}
              />
            )}
          />
          <Button type="submit" variant="contained" color="primary" sx={{ mt: 3 }}>
            {t('auction.saveChanges')}
          </Button>
        </Box>
      </Paper>
    </Container>
  );
}
