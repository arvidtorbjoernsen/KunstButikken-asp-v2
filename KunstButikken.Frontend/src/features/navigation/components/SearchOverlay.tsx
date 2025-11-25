'use client';

import React, { useEffect, useState } from 'react';
import Paper from '@mui/material/Paper';
import IconButton from '@mui/material/IconButton';
import InputBase from '@mui/material/InputBase';
import SearchIcon from '@mui/icons-material/Search';
import CloseIcon from '@mui/icons-material/Close';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '@/shared/state/store';
import { setQuery } from '@/features/search/state/searchSlice';
import Popover from '@mui/material/Popover';
import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';
import Typography from '@mui/material/Typography';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemText from '@mui/material/ListItemText';
import ListItemIcon from '@mui/material/ListItemIcon';
import Avatar from '@mui/material/Avatar';
import NextLink from 'next/link';
import { clearSearchResults, fetchSearchResults } from '@/features/search/state/searchResultsSlice';

interface SearchOverlayProps {
  open: boolean;
  onClose: () => void;
  anchorEl: HTMLElement | null;
}

function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}

export default function SearchOverlay({ open, onClose, anchorEl }: SearchOverlayProps) {
  const { t, locale } = useTranslations();
  const dispatch = useDispatch();
  const query = useSelector((s: RootState) => s.search.query);
  const { results, loading, error } = useSelector((s: RootState) => s.searchResults);

  const debouncedQuery = useDebounce(query, 700);

  useEffect(() => {
    if (debouncedQuery) {
      dispatch(fetchSearchResults(debouncedQuery) as any);
    } else {
      dispatch(clearSearchResults());
    }
  }, [debouncedQuery, dispatch]);

  useEffect(() => {
    if (!open) {
      dispatch(clearSearchResults());
      dispatch(setQuery(''));
    }
  }, [open, dispatch]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onClose();
  };

  return (
    <Popover
      open={open}
      anchorEl={anchorEl}
      onClose={onClose}
      anchorOrigin={{
        vertical: 'bottom',
        horizontal: 'center',
      }}
      transformOrigin={{
        vertical: 'top',
        horizontal: 'center',
      }}
      slotProps={{ // Use slotProps instead of PaperProps
        paper: {
          sx: {
            mt: '4px', // Adjusted margin-top to drop it down slightly (0.5 * 8px = 4px)
            borderRadius: 2,
            boxShadow: 3,
            overflow: 'visible',
            width: '80vw',
            maxWidth: '80vw',
            left: '50% !important',
            transform: 'translateX(-50%) !important',
            bgcolor: 'background.paper',
          },
        },
      }}
    >
      <Paper
        component='form'
        sx={{
          p: '2px 4px',
          display: 'flex',
          alignItems: 'center',
          width: '100%',
        }}
        onSubmit={handleSearchSubmit}
      >
        <IconButton type='submit' sx={{ p: '10px' }} aria-label={t('aria.search')}>
          <SearchIcon />
        </IconButton>
        <InputBase
          sx={{ ml: 1, flex: 1, textOverflow: 'clip' }}
          placeholder={t('home.searchPlaceholder')}
          inputProps={{ 'aria-label': t('aria.search') }}
          value={query}
          onChange={e => dispatch(setQuery(e.target.value))}
          autoFocus
        />
        <IconButton
          color='primary'
          sx={{ p: '10px' }}
          onClick={onClose}
          aria-label={t('aria.close')}
        >
          <CloseIcon />
        </IconButton>
      </Paper>

      {/* Search Results Display */}
      {(loading || results.length > 0 || error) && (
        <Box sx={{ p: 2, maxHeight: 300, overflowY: 'auto' }}>
          {loading && (
            <Box sx={{ display: 'flex', justifyContent: 'center', my: 2 }}>
              <CircularProgress size={24} />
            </Box>
          )}
          {error && (
            <Typography color='error' variant='body2' sx={{ my: 2 }}>
              {error}
            </Typography>
          )}
          {!loading && !error && results.length === 0 && query && (
            <Typography variant='body2' color='text.secondary' sx={{ my: 2 }}>
              {t('home.noResults')}
            </Typography>
          )}
          {!loading && !error && results.length > 0 && (
            <List dense>
              {results.map(art => (
                <ListItem
                  key={art.id}
                  component={NextLink}
                  href={`/art/${art.id}`}
                  onClick={onClose}
                  sx={{ '&:hover': { bgcolor: 'action.hover' }, borderRadius: 1 }}
                >
                  <ListItemIcon>
                    <Avatar
                      src={art.image || '/placeholder.png'}
                      alt={locale === 'nb' ? art.titleNb : art.titleEn}
                    />
                  </ListItemIcon>
                  <ListItemText
                    slotProps={{ // Use slotProps instead of primaryTypographyProps and secondaryTypographyProps
                      primary: {
                        sx: { fontSize: '1rem', fontWeight: 500, color: 'text.primary' },
                      },
                      secondary: {
                        sx: { fontSize: '0.875rem', color: 'text.secondary' },
                      },
                    }}
                    primary={locale === 'nb' ? art.titleNb : art.titleEn}
                    secondary={art.artist || art.sellerDisplayName || t('art.unknownArtist')}
                  />
                </ListItem>
              ))}
            </List>
          )}
        </Box>
      )}
    </Popover>
  );
}
