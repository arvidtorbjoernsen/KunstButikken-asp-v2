"use client";

import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { useEffect, useState } from "react";

export default function ClearSessionPage() {
  const [clearing, setClearing] = useState(false);
  const [step, setStep] = useState(0);

  useEffect(() => {
    if (step === 1) {
      // Step 1: Clear localStorage
      try {
        localStorage.clear();
        console.log("✅ Cleared localStorage");
      } catch (e) {
        console.error("Failed to clear localStorage:", e);
      }

      // Step 2: Clear sessionStorage
      try {
        sessionStorage.clear();
        console.log("✅ Cleared sessionStorage");
      } catch (e) {
        console.error("Failed to clear sessionStorage:", e);
      }

      setStep(2);
    } else if (step === 2) {
      // Step 3: Redirect to proper logout flow which handles Keycloak
      setTimeout(() => {
        window.location.href = '/auth/logout';
      }, 1000);
    }
  }, [step]);

  const handleClear = () => {
    setClearing(true);
    setStep(1);
  };

  if (clearing) {
    return (
      <Container sx={{ py: 8, textAlign: "center" }}>
        <CircularProgress size={60} sx={{ mb: 3 }} />
        <Typography variant="h5" gutterBottom>
          Clearing Session...
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {step === 1 && "Clearing browser storage..."}
          {step === 2 && "Redirecting to full logout..."}
        </Typography>
      </Container>
    );
  }

  return (
    <Container sx={{ py: 8, maxWidth: "md" }}>
      <Typography variant="h4" gutterBottom>
        Clear Session & Cookies
      </Typography>

      <Alert severity="warning" sx={{ mb: 4 }}>
        <Typography variant="body2" gutterBottom>
          <strong>Use this if you're experiencing session issues!</strong>
        </Typography>
        <Typography variant="caption">
          This will completely clear your browser session, sign you out, and redirect to the home page.
          You'll need to sign in again.
        </Typography>
      </Alert>

      <Box sx={{ mb: 4 }}>
        <Typography variant="h6" gutterBottom>
          What this does:
        </Typography>
        <Typography component="div" variant="body2" sx={{ ml: 2 }}>
          • Clears browser localStorage<br />
          • Clears browser sessionStorage<br />
          • Logs out from Keycloak<br />
          • Redirects to home page
        </Typography>
      </Box>

      <Alert severity="info" sx={{ mb: 4 }}>
        <Typography variant="body2">
          <strong>Why you might need this:</strong><br />
          If you signed in before the authentication fixes were applied, your session might contain
          old/invalid data. This tool completely resets your session so you can sign in fresh.
        </Typography>
      </Alert>

      <Box sx={{ mt: 4 }}>
        <Button
          variant="contained"
          color="error"
          size="large"
          onClick={handleClear}
          disabled={clearing}
        >
          Clear Session & Sign Out
        </Button>
      </Box>

      <Box sx={{ mt: 4 }}>
        <Typography variant="caption" color="text.secondary">
          After clicking, you'll be redirected to the home page. You can then sign in again to get a fresh session.
        </Typography>
      </Box>
    </Container>
  );
}
