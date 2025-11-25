"use client";
import React, { useEffect, useState } from "react";
import Container from "@mui/material/Container";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Button from "@mui/material/Button";
import Link from "next/link";

export default function SuccessPage() {
  const [sessionId, setSessionId] = useState<string | null>(null);

  useEffect(() => {
    const url = new URL(window.location.href);
    setSessionId(url.searchParams.get("session_id"));
  }, []);

  return (
    <Container sx={{ py: 6 }}>
      <Box>
        <Typography variant="h4" component="h1" gutterBottom>
          Payment successful
        </Typography>
        <Typography variant="body1" sx={{ mb: 2 }}>
          Thank you! Your payment was processed.
        </Typography>
        {sessionId && (
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Stripe session: <code>{sessionId}</code>
          </Typography>
        )}
        <Link href="/">
          <Button variant="contained">Back to home</Button>
        </Link>
      </Box>
    </Container>
  );
}
