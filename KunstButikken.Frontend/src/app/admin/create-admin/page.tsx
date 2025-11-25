"use client";

import React, { useState } from "react";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import TextField from "@mui/material/TextField";
import Button from "@mui/material/Button";
import Alert from "@mui/material/Alert";
import Link from "next/link";
import { apiFetch } from "@/shared/api/api";

export default function CreateAdminPage() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    setError(null);
    try {
      await apiFetch("USER", "/api/admin/profiles/make-admin-by-email", {
        method: "POST",
        body: JSON.stringify({ email })
      });
      setMessage(`Successfully promoted ${email} to admin (profile updated).`);
      setEmail("");
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : String(err);
      setError(message || "Failed to promote user to admin");
    } finally {
      setLoading(false);
    }
  }

  return (
    <Container sx={{ py: 6 }}>
      <Typography variant="h4" gutterBottom>Create Admin</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Promote an existing user to admin by entering their email.
      </Typography>
      {message && <Alert severity="success" sx={{ mb: 2 }}>{message}</Alert>}
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      <Box component="form" onSubmit={submit} sx={{ maxWidth: 500 }}>
        <TextField
          label="User Email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          fullWidth
          required
          sx={{ mb: 2 }}
        />
        <Button type="submit" variant="contained" disabled={loading}>
          {loading ? "Processing..." : "Promote to Admin"}
        </Button>
      </Box>
      <Box sx={{ mt: 3 }}>
        <Link href="/admin">← Back to Admin Dashboard</Link>
      </Box>
    </Container>
  );
}
