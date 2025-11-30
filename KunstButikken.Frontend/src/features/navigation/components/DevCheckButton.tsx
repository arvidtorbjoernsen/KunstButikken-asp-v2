'use client';

import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import { useKeycloak } from '@/features/auth/lib/keycloak';
import BugReportIcon from '@mui/icons-material/BugReport';
import RefreshIcon from '@mui/icons-material/Refresh';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';

export default function DevCheckButton() {
  const [open, setOpen] = useState(false);
  const [homeDebug, setHomeDebug] = useState<any | null>(null);
  const { t } = useTranslations();
  const { keycloak, authenticated, isSeller, isBuyer, isAdmin, getUsername } = useKeycloak();
  const router = useRouter();

  // Only show in development
  if (process.env.NODE_ENV === 'production') {
    return null;
  }

  const tokenParsed = keycloak?.tokenParsed;

  // Extract relevant user information
  const userId = tokenParsed?.['sub'];
  const email = tokenParsed?.['email'];
  const username = getUsername?.() || tokenParsed?.['preferred_username'];
  const name = tokenParsed?.['name'];
  const givenName = tokenParsed?.['given_name'];
  const familyName = tokenParsed?.['family_name'];

  // Extract roles
  const realmRoles = tokenParsed?.['realm_access']?.['roles'] || [];
  const resourceAccess = tokenParsed?.['resource_access'] || {};

  // Session info
  const sessionState = tokenParsed?.['session_state'];
  const issuedAt = tokenParsed?.['iat']
    ? new Date(tokenParsed['iat'] * 1000).toLocaleString()
    : 'N/A';
  const expiresAt = tokenParsed?.['exp']
    ? new Date(tokenParsed['exp'] * 1000).toLocaleString()
    : 'N/A';
  const issuer = tokenParsed?.['iss'];

  const handleOpen = () => {
    setOpen(true);
  };

  const handleClose = () => {
    setOpen(false);
    // Removed buttonRef.current?.focus(); - Material-UI Dialog handles this automatically
  };

  const handleForceRefresh = () => {
    router.push('/auth/logout');
  };

  useEffect(() => {
    if (process.env.NODE_ENV === 'production') {
      return;
    }
    if (typeof window === 'undefined') {
      return;
    }
    const handler = (evt: Event) => {
      const detail = (evt as CustomEvent).detail;
      setHomeDebug(detail ?? (window as any).__KB_HOME_DEBUG__ ?? null);
    };
    window.addEventListener('kb-home-debug', handler);
    const initial = (window as any).__KB_HOME_DEBUG__;
    if (initial) {
      setHomeDebug(initial);
    }
    return () => window.removeEventListener('kb-home-debug', handler);
  }, []);

  return (
    <>
      <Tooltip title={t('nav.devCheck')}>
        <IconButton
          // Removed ref={buttonRef}
          aria-label={t('nav.devCheck')}
          onClick={handleOpen}
          size="small"
          color="warning"
          sx={{
            border: '1px dashed',
            borderColor: 'warning.main',
            opacity: 0.7,
            '&:hover': { opacity: 1 },
          }}
        >
          <BugReportIcon fontSize="small" />
        </IconButton>
      </Tooltip>

      <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
        <DialogTitle>
          Dev Check: Session & User Info
          {authenticated && (
            <Typography variant="caption" color="success.main" sx={{ ml: 2 }}>
              ✓ Authenticated
            </Typography>
          )}
          {!authenticated && (
            <Typography variant="caption" color="error.main" sx={{ ml: 2 }}>
              ✗ Not Authenticated
            </Typography>
          )}
        </DialogTitle>
        <DialogContent>
          {!authenticated && (
            <Typography color="error" sx={{ mb: 2 }}>
              No Keycloak session found. User is not logged in.
            </Typography>
          )}

          {authenticated && (
            <Box>
              {/* User Information */}
              <Box sx={{ mb: 3 }}>
                <Typography variant="h6" gutterBottom>
                  User Information:
                </Typography>
                <Box
                  sx={{
                    p: 2,
                    bgcolor: 'background.paper',
                    borderRadius: 1,
                    border: '1px solid',
                    borderColor: 'divider',
                  }}
                >
                  <Typography variant="body2">
                    <strong>User ID (sub):</strong> {userId || 'N/A'}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Username:</strong> {username || 'N/A'}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Email:</strong> {email || 'N/A'}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Display Name:</strong> {name || 'N/A'}
                  </Typography>
                  {givenName && (
                    <Typography variant="body2">
                      <strong>Given Name:</strong> {givenName}
                    </Typography>
                  )}
                  {familyName && (
                    <Typography variant="body2">
                      <strong>Family Name:</strong> {familyName}
                    </Typography>
                  )}
                </Box>
              </Box>

              {/* Roles & Permissions */}
              <Box sx={{ mb: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Roles & Permissions:
                </Typography>
                <Box
                  sx={{
                    p: 2,
                    bgcolor: 'background.paper',
                    borderRadius: 1,
                    border: '1px solid',
                    borderColor: 'divider',
                  }}
                >
                  <Typography variant="body2">
                    <strong>Is Seller:</strong> {isSeller ? '✅ Yes' : '❌ No'}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Is Buyer:</strong> {isBuyer ? '✅ Yes' : '❌ No'}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Is Admin:</strong> {isAdmin ? '✅ Yes' : '❌ No'}
                  </Typography>
                  <Typography variant="body2" sx={{ mt: 1 }}>
                    <strong>Realm Roles:</strong>
                  </Typography>
                  <Box component="ul" sx={{ mt: 0.5, mb: 1, pl: 3 }}>
                    {realmRoles.length > 0 ? (
                      realmRoles.map((role: string) => (
                        <li key={role}>
                          <Typography variant="body2">{role}</Typography>
                        </li>
                      ))
                    ) : (
                      <Typography variant="body2" color="text.secondary">
                        No realm roles
                      </Typography>
                    )}
                  </Box>
                  {Object.keys(resourceAccess).length > 0 && (
                    <>
                      <Typography variant="body2" sx={{ mt: 1 }}>
                        <strong>Resource/Client Roles:</strong>
                      </Typography>
                      {Object.entries(resourceAccess).map(([client, data]) => (
                        <Box key={client} sx={{ ml: 2, mt: 0.5 }}>
                          <Typography variant="body2">
                            <strong>{client}:</strong> {Array.isArray(data?.roles) ? data.roles.join(', ') : 'none'}
                          </Typography>
                        </Box>
                      ))}
                    </>
                  )}
                </Box>
              </Box>

              {/* Session Details */}
              <Box sx={{ mb: 3 }}>
                <Typography variant="h6" gutterBottom>
                  Session Details:
                </Typography>
                <Box
                  sx={{
                    p: 2,
                    bgcolor: 'background.paper',
                    borderRadius: 1,
                    border: '1px solid',
                    borderColor: 'divider',
                  }}
                >
                  <Typography variant="body2">
                    <strong>Session State:</strong> {sessionState || 'N/A'}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Issued At:</strong> {issuedAt}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Expires At:</strong> {expiresAt}
                  </Typography>
                  <Typography variant="body2">
                    <strong>Issuer:</strong> {issuer || 'N/A'}
                  </Typography>
                </Box>
              </Box>

              {/* Raw Token Data */}
              <Box sx={{ mb: 2 }}>
                <Typography variant="h6" gutterBottom>
                  Raw Token Data:
                </Typography>
                <Box sx={{ mb: 1, p: 1, bgcolor: 'info.light', borderRadius: 1 }}>
                  <Typography variant="caption" color="info.dark">
                    <strong>Available token keys:</strong>{' '}
                    {Object.keys(tokenParsed || {}).join(', ') || 'none'}
                  </Typography>
                </Box>
                <Box
                  component="pre"
                  sx={{
                    backgroundColor: theme =>
                      theme.palette.mode === 'dark' ? 'grey.900' : 'grey.100',
                    color: theme => theme.palette.text.primary,
                    padding: 2,
                    borderRadius: 1,
                    overflow: 'auto',
                    fontSize: '0.75rem',
                    fontFamily: 'monospace',
                    maxHeight: '30vh',
                  }}
                >
                  {JSON.stringify(tokenParsed, null, 2)}
                </Box>
              </Box>

              {/* Homepage Seller Debug - New Section */}
              {authenticated && homeDebug && (
                <Box sx={{ mb: 3 }}>
                  <Typography variant="h6" gutterBottom>
                    Homepage Seller Debug:
                  </Typography>
                  <Box
                    sx={{
                      p: 2,
                      bgcolor: 'background.paper',
                      borderRadius: 1,
                      border: '1px solid',
                      borderColor: 'divider',
                    }}
                  >
                    <Typography variant="body2">
                      <strong>Is Seller:</strong> {homeDebug.isSeller ? 'Yes' : 'No'}
                    </Typography>
                    <Typography variant="body2">
                      <strong>Has Seller Data:</strong> {homeDebug.hasSellerData ? 'Yes' : 'No'}
                    </Typography>
                    <Typography variant="body2">
                      <strong>Allow Seller View:</strong> {homeDebug.allowSellerView ? 'Yes' : 'No'}
                    </Typography>
                    <Typography variant="body2">
                      <strong>View Mode:</strong> {homeDebug.viewMode}
                    </Typography>
                    <Typography variant="body2">
                      <strong>Seller Art Count:</strong> {homeDebug.sellerArtCount}
                    </Typography>
                    <Typography variant="body2">
                      <strong>Seller Featured Count:</strong> {homeDebug.sellerFeaturedCount}
                    </Typography>
                    <Typography variant="body2">
                      <strong>Seller Auctions Count:</strong> {homeDebug.sellerAuctionsCount}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Updated: {homeDebug.timestamp}
                    </Typography>
                  </Box>
                  <Button
                    component={Link}
                    href="/debug/session-data"
                    onClick={handleClose}
                    color="info"
                    variant="text"
                    sx={{ mt: 1 }}
                  >
                    View Full Session Data
                  </Button>
                </Box>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          {authenticated && (
            <>
              <Button
                component={Link}
                href="/debug/session-data"
                onClick={handleClose}
                color="info"
                variant="text"
              >
                View Full Session Data
              </Button>
              <Button
                onClick={handleForceRefresh}
                startIcon={<RefreshIcon />}
                color="warning"
                variant="outlined"
              >
                Force Refresh Session
              </Button>
            </>
          )}
          <Button onClick={handleClose} variant="contained">
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
