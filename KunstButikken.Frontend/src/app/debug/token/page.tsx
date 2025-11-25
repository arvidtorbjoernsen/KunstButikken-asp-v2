"use client";

import { useKeycloak } from "@/features/auth/lib/keycloak";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";

export default function TokenDebugPage() {
  const { authenticated, loading, keycloak } = useKeycloak();

  if (loading) {
    return (
      <Container sx={{ py: 4 }}>
        <Typography variant="h4" gutterBottom>Token Debug</Typography>
        <Alert severity="info">Loading Keycloak...</Alert>
      </Container>
    );
  }

  const accessToken = keycloak?.token;
  const idToken = keycloak?.idToken;
  const refreshToken = keycloak?.refreshToken;

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h4" gutterBottom>Keycloak Token Debug</Typography>

      <Box sx={{ mb: 2 }}>
        <Alert severity={authenticated ? "success" : "error"}>
          Authenticated: {authenticated ? "Yes" : "No"}
        </Alert>
      </Box>

      {authenticated && (
        <>
          <Box sx={{ mb: 2 }}>
            <Alert severity={accessToken ? "success" : "error"}>
              Access Token exists: {accessToken ? "Yes" : "No"}
            </Alert>
          </Box>

          {accessToken && (
            <Box sx={{ mb: 2 }}>
              <Typography variant="body2">
                Token length: {accessToken.length}
              </Typography>
              <Typography variant="body2" sx={{ wordBreak: "break-all" }}>
                Token preview: {accessToken.substring(0, 50)}...
              </Typography>
            </Box>
          )}

          <Box sx={{ mb: 2 }}>
            <Typography variant="h6" gutterBottom>Keycloak Data:</Typography>
            <Box
              component="pre"
              sx={{
                backgroundColor: (theme) => theme.palette.mode === 'dark' ? 'grey.900' : 'grey.100',
                color: (theme) => theme.palette.text.primary,
                padding: 2,
                borderRadius: 1,
                overflow: "auto",
                fontSize: "0.875rem",
                fontFamily: "monospace",
              }}
            >
              {JSON.stringify(
                {
                  authenticated,
                  tokenParsed: keycloak?.tokenParsed,
                  realmAccess: keycloak?.realmAccess,
                  resourceAccess: keycloak?.resourceAccess,
                  hasAccessToken: !!accessToken,
                  hasIdToken: !!idToken,
                  hasRefreshToken: !!refreshToken,
                },
                null,
                2
              )}
            </Box>
          </Box>
        </>
      )}
    </Container>
  );
}
