"use client";

import React from "react";
import { useKeycloak } from "@/features/auth/lib/keycloak";
import UnverifiedSeller from "@/features/admin/components/UnverifiedSeller";
import UnverifiedArt from "@/features/admin/components/UnverifiedArt";
import AdminCreateForm from "@/features/admin/components/AdminCreateForm";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Divider from "@mui/material/Divider";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Alert from "@mui/material/Alert";
import { useRouter } from "next/navigation";

export default function AdminPage() {
  const { authenticated, loading, isAdmin, login } = useKeycloak();
  const router = useRouter();
  const userIsAdmin = authenticated && isAdmin; // Corrected: isAdmin is a boolean, not a function

  if (loading) {
    return (
      <main>
        <Box sx={{ p: 4, display: "flex", justifyContent: "center", alignItems: "center", minHeight: "50vh" }}>
          <CircularProgress />
        </Box>
      </main>
    );
  }

  if (!authenticated) {
    return (
      <main>
        <Box sx={{ p: 4, maxWidth: 600, mx: "auto", mt: 8 }}>
          <Alert severity="warning" sx={{ mb: 2 }}>
            You must be logged in to access the admin panel.
          </Alert>
          <Button variant="contained" onClick={login} fullWidth>
            Log In
          </Button>
        </Box>
      </main>
    );
  }

  if (!userIsAdmin) {
    return (
      <main>
        <Box sx={{ p: 4, maxWidth: 600, mx: "auto", mt: 8 }}>
          <Alert severity="error" sx={{ mb: 2 }}>
            Access Denied: You do not have admin privileges.
          </Alert>
          <Button variant="contained" onClick={() => router.push("/")} fullWidth>
            Return to Home
          </Button>
        </Box>
      </main>
    );
  }

  return (
    <main>
      <Box sx={{ p: 4 }}>
        <Typography variant="h4" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          Admin Console
        </Typography>


        <AdminCreateForm />

        <Divider sx={{ my: 2 }} />

        <UnverifiedSeller />

        <Divider sx={{ my: 2 }} />

        <UnverifiedArt />
      </Box>
    </main>
  );
}
