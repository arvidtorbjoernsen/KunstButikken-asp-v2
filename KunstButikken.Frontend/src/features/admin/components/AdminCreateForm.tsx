"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { parseResponse } from '@/shared/api';
import { useKeycloak } from "@/features/auth/lib/keycloak";
import PersonAddAlt1Icon from '@mui/icons-material/PersonAddAlt1';
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from '@mui/material/CircularProgress';
import TextField from "@mui/material/TextField";
import ToggleButton from '@mui/material/ToggleButton';
import ToggleButtonGroup from '@mui/material/ToggleButtonGroup';
import Typography from "@mui/material/Typography";
import React, { useEffect, useState } from "react";

export type AdminCreateFormProps = {
  // Optional initial mode. Defaults to 'invite'.
  initialMode?: 'invite' | 'create';
};

export default function AdminCreateForm({ initialMode = 'invite' }: AdminCreateFormProps) {
  const { keycloak, authenticated, loading: kcLoading } = useKeycloak();
  const { t } = useTranslations();
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [mode, setMode] = useState<'invite' | 'create'>(initialMode);
  const [emailError, setEmailError] = useState<string | null>(null);

  useEffect(() => {
    if (!email) {
      setEmailError(null);
      return;
    }
    const trimmed = email.trim();
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!re.test(trimmed)) {
      setEmailError('Please enter a valid email address');
    } else {
      setEmailError(null);
    }
  }, [email]);

  const createAdmin = async (e: React.FormEvent) => {
    e.preventDefault();
    setMessage(null);
    if (!email || emailError) {
      setMessage('Please enter a valid email before submitting.');
      return;
    }
    setLoading(true);
    try {
      const res = await fetch("/api/admin/create", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: email.trim(), mode }),
        credentials: "include",
      });
      if (!res.ok) {
        const json = await res.text();
        setMessage(`Failed: ${res.status} ${res.statusText} - ${json}`);
      } else {
        let txt = 'Admin user created / invited successfully.';
        try {
          const data = await parseResponse<Record<string, unknown>>(res);
          if (data && typeof (data as Record<string, unknown>).message === 'string') {
            txt = (data as Record<string, unknown>).message as string;
          }
        } catch {}
        setMessage(txt);
        setEmail("");
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      setMessage(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box component="section" sx={{ mb: 4 }}>
      <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <PersonAddAlt1Icon sx={{ mr: 1, verticalAlign: 'middle' }} /> Create / Invite admin
      </Typography>

      {(kcLoading || !keycloak) && <Alert severity="info">Loading session...</Alert>}
      {keycloak && !authenticated && (
        <Alert severity="warning">You must sign in as an admin to use this form.</Alert>
      )}

      <Box component="form" onSubmit={createAdmin} sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1, maxWidth: 640 }}>
        <ToggleButtonGroup
          value={mode}
          exclusive
          onChange={(e, v) => { if (v) setMode(v); }}
          aria-label={t("aria.mode")}
          size="small"
        >
          <ToggleButton value="invite" aria-label={t("aria.invite")}>{t("aria.invite")}</ToggleButton>
          <ToggleButton value="create" aria-label={t("aria.create")}>{t("aria.create")}</ToggleButton>
        </ToggleButtonGroup>

        <TextField
          label="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          size="small"
          error={Boolean(emailError)}
          helperText={emailError ?? 'Enter the user email to invite or create as admin.'}
          fullWidth
        />

        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <Button
            type="submit"
            variant="contained"
            disabled={loading || !authenticated || Boolean(emailError) || !email.trim()}
          >
            {loading ? <CircularProgress size={20} /> : (mode === 'invite' ? 'Send invite' : 'Create admin')}
          </Button>
          <Button type="button" variant="outlined" onClick={() => setEmail('')} disabled={loading}>Clear</Button>
        </Box>

        {message && <Alert severity="info">{message}</Alert>}
      </Box>
    </Box>
  );
}
