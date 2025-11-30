import Chip from '@mui/material/Chip';
import type { ChipProps } from '@mui/material/Chip';
import React from 'react';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';

const FeaturedChip: React.FC<ChipProps> = ({ label, ...rest }) => {
  const { t } = useTranslations();
  const resolvedLabel = label ?? t('art.labels.featured');
  return (
    <Chip
      label={resolvedLabel}
      color="secondary"
      size="small"
      sx={{ height: 20, fontSize: '0.7rem', ...(rest.sx || {}) }}
      {...rest}
    />
  );
};

FeaturedChip.displayName = 'FeaturedChip';

export default FeaturedChip;
