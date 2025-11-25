"use client";

import { useKeycloak } from "@/features/auth/lib/keycloak";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import ProfileClient from "./ProfileClient";

export default function MePage() {
  const { authenticated, loading } = useKeycloak();
  const router = useRouter();

  useEffect(() => {
    if (!loading && !authenticated) {
      router.push("/auth/signin");
    }
  }, [loading, authenticated, router]);

  // Show loading while Keycloak initializes
  if (loading) {
    return (
      <Container sx={{ py: 4, maxWidth: 'lg' }}>
        <Box sx={{ display: "flex", flexDirection: "column", alignItems: "center", py: 8 }}>
          <CircularProgress size={50} sx={{ mb: 2 }} />
          <Typography>Initializing...</Typography>
        </Box>
      </Container>
    );
  }

  // Don't render anything if redirecting to signin
  if (!authenticated) {
    return null;
  }

  return (
    <Container sx={{ py: 4, maxWidth: 'lg' }}>
      <ProfileClient />
    </Container>
  );
}
