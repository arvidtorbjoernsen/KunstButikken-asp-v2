'use client';

import React from 'react';
import InputBase from '@mui/material/InputBase';
import Button from '@mui/material/Button';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '@/shared/state/store';
import { setQuery } from '@/features/search/state/searchSlice';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';

export default function NavbarSearch() {
  const { t } = useTranslations();
  const dispatch = useDispatch();
  const query = useSelector((s: RootState) => s.search.query);

  return (
    <form
      onSubmit={e => {
        e.preventDefault();
      }}
    >
      <InputBase
        placeholder={t('home.searchPlaceholder')}
        inputProps={{ 'aria-label': t('aria.search') }}
        value={query}
        onChange={e => dispatch(setQuery(e.target.value))}
      />
      <Button type="submit">{t('nav.search')}</Button>
    </form>
  );
}
