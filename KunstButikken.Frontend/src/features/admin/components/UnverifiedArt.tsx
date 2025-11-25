"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { parseResponse } from '@/shared/api';
import { useKeycloak } from "@/features/auth/lib/keycloak";
import CheckIcon from "@mui/icons-material/Check";
import CloseIcon from "@mui/icons-material/Close";
import { default as Alert, AlertProps, default as MuiAlert } from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import Snackbar from '@mui/material/Snackbar';
import Typography from "@mui/material/Typography";
import React, { useEffect, useState } from "react";

const InnerAlert = React.forwardRef<HTMLDivElement, AlertProps>(function Alert(props, ref) {
  return <MuiAlert elevation={6} ref={ref} variant="filled" {...props} />;
});

type ArtItem = {
  id: string;
  titleNb?: string;
  titleEn?: string;
  artist?: string;
  descriptionNb?: string;
  descriptionEn?: string;
};

export default function UnverifiedArt() {
  const { keycloak } = useKeycloak();
  const { t } = useTranslations();
  const [rejectDialogOpen, setRejectDialogOpen] = useState(false);
  const [rejectTarget, setRejectTarget] = useState<{ id: string; label: string } | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [list, setList] = useState<ArtItem[]>([]);

  // Snackbar
  const [snackOpen, setSnackOpen] = useState(false);
  const [snackMsg, setSnackMsg] = useState("");
  const [snackSeverity, setSnackSeverity] = useState<AlertProps['severity']>('success');

  const fetchList = async () => {
    setLoading(true);
    setError(null);
    try {
      const token = keycloak?.token;
      const headers: Record<string, string> = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }
      
      const res = await fetch(`/api/art/unverified`, { 
        credentials: "include",
        headers 
      });
      if (res.status === 401) {
        setError("Unauthorized — please sign in as an admin.");
        setList([]);
      } else if (res.status === 403) {
        setError("Forbidden — you need admin privileges.");
        setList([]);
      } else if (!res.ok) {
        setError(`Failed to load: ${res.status} ${res.statusText}`);
      } else {
        const data = await parseResponse<unknown[]>(res);
        const arr = Array.isArray(data) ? data : [];
        const normalized = arr.map((it: unknown) => {
          const obj = (it as Record<string, unknown>) || {};
          return ({
            id: String(obj.id ?? ''),
            titleNb: (obj.titleNb ?? undefined) as string | undefined,
            titleEn: (obj.titleEn ?? undefined) as string | undefined,
            artist: (obj.artist ?? obj.creator ?? undefined) as string | undefined,
            descriptionNb: (obj.descriptionNb ?? undefined) as string | undefined,
            descriptionEn: (obj.descriptionEn ?? undefined) as string | undefined,
          } as ArtItem);
        });
        setList(normalized);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!keycloak?.authenticated) return;
    void fetchList();
  }, [keycloak?.authenticated]);

  const showSnack = (msg: string, severity: AlertProps['severity'] = 'success') => {
    setSnackMsg(msg);
    setSnackSeverity(severity);
    setSnackOpen(true);
  };

  const setVerified = async (id: string, verified: boolean) => {
    try {
      const token = keycloak?.token;
      const headers: Record<string, string> = { "Content-Type": "application/json" };
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }
      
      const res = await fetch(`/api/art/${id}/verify`, {
        method: "POST",
        headers,
        body: JSON.stringify({ verified }),
        credentials: "include",
      });
      if (res.status === 401) {
        setError("Unauthorized — please sign in as an admin.");
        showSnack('Unauthorized — please sign in as an admin.', 'error');
        return;
      }
      if (res.status === 403) {
        setError("Forbidden — you need admin privileges.");
        showSnack('Forbidden — you need admin privileges.', 'error');
        return;
      }
      if (!res.ok) {
        const txt = `Failed to update: ${res.status} ${res.statusText}`;
        setError(txt);
        showSnack(txt, 'error');
        return;
      }
      setList((s) => s.filter((x) => x.id !== id));
      showSnack(verified ? 'Marked as verified.' : 'Rejected art.', 'success');
    } catch (err) {
      const txt = err instanceof Error ? err.message : String(err);
      setError(txt);
      showSnack(txt, 'error');
    }
  };

  const openRejectDialog = (id: string, label: string) => {
    setRejectTarget({ id, label });
    setRejectDialogOpen(true);
  };

  const closeRejectDialog = () => {
    setRejectDialogOpen(false);
    setRejectTarget(null);
  };

  const confirmReject = async () => {
    if (!rejectTarget) return;
    await setVerified(rejectTarget.id, false);
    closeRejectDialog();
  };

  const handleSnackClose = (_event: React.SyntheticEvent | Event, reason?: string) => {
    if (reason === 'clickaway') return;
    setSnackOpen(false);
  };

  if (!keycloak) return <CircularProgress />;

  if (!keycloak.authenticated) {
    return (
      <Box sx={{ p: 4 }}>
        <Typography variant="h6">Admin — Unverified art</Typography>
        <Alert severity="warning" sx={{ mt: 2, mb: 2 }}>
          You must be signed in as an admin to view this page.
        </Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 4 }}>
      <Typography variant="h5" gutterBottom>
        Unverified art
      </Typography>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {loading ? (
        <CircularProgress />
      ) : list.length === 0 ? (
        <Alert severity="info">No unverified art found.</Alert>
      ) : (
        <List>
          {list.map((s) => (
            <ListItem
              key={s.id}
              divider
              secondaryAction={(
                <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', mr: 1 }}>
                  <IconButton edge="end" aria-label={t("aria.verify")} title={t("admin.verify")} onClick={() => void setVerified(s.id, true)}>
                    <CheckIcon color="success" />
                  </IconButton>
                  <IconButton edge="end" aria-label={t("aria.reject")} title={t("admin.reject")} onClick={() => openRejectDialog(s.id, s.titleEn || s.titleNb || '(no title)')}>
                    <CloseIcon color="error" />
                  </IconButton>
                </Box>
              )}
            >
              <ListItemText primary={s.titleEn || s.titleNb || "(no title)"} secondary={s.artist ?? s.descriptionEn ?? s.descriptionNb} />
            </ListItem>
          ))}
        </List>
      )}

      {/* Confirm reject dialog */}
      <Dialog open={rejectDialogOpen} onClose={closeRejectDialog} aria-labelledby="reject-art-dialog-title">
        <DialogTitle id="reject-art-dialog-title">Confirm action</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to mark "{rejectTarget?.label ?? ''}" as not verified? This will remove it from the unverified list.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeRejectDialog}>Cancel</Button>
          <Button color="error" onClick={() => void confirmReject()} autoFocus>
            Reject
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={snackOpen} autoHideDuration={6000} onClose={handleSnackClose}>
        <InnerAlert onClose={handleSnackClose} severity={snackSeverity} sx={{ width: '100%' }}>
          {snackMsg}
        </InnerAlert>
      </Snackbar>
    </Box>
  );
}
