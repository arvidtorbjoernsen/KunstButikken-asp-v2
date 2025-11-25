"use client";

import { useKeycloak } from "@/features/auth/lib/keycloak";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import Link from "next/link";
import { useRouter } from "next/navigation";

export default function SessionDebugPage() {
  const { keycloak } = useKeycloak();
  const router = useRouter();

  const authenticated = keycloak?.authenticated ?? false;
  const tokenParsed = keycloak?.tokenParsed;

  const roles: string[] = (() => {
    const realmRoles = tokenParsed?.realm_access?.roles;
    return Array.isArray(realmRoles) ? realmRoles.filter((x): x is string => typeof x === "string") : [];
  })();

  const hasAdmin = roles.some((r) => /\badmin\b/i.test(r));

  const handleLogin = () => {
    keycloak?.login({ prompt: 'login' });
  };

  const handleLogout = () => {
    router.push('/auth/logout');
  };

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h4" gutterBottom>Session Debug</Typography>

      <Box sx={{ mb: 3 }}>
        <Typography variant="body1" gutterBottom>
          Authenticated: <Chip label={String(authenticated)} color={authenticated ? "success" : "default"} size="small" />
        </Typography>
      </Box>

      <Stack direction="row" spacing={2} sx={{ mb: 4 }}>
        <Button
          variant="contained"
          onClick={handleLogin}
          disabled={authenticated}
        >
          Sign in (Keycloak)
        </Button>
        <Button
          variant="outlined"
          onClick={handleLogout}
          disabled={!authenticated}
        >
          Sign out
        </Button>
      </Stack>

      <Typography variant="h5" gutterBottom sx={{ mt: 4 }}>
        Token Data (Raw)
      </Typography>
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
          mb: 4,
        }}
      >
        {JSON.stringify(tokenParsed, null, 2)}
      </Box>

      <Typography variant="h5" gutterBottom>
        Derived Values
      </Typography>
      <Box sx={{ mb: 2 }}>
        <Typography variant="body1">
          Roles: <Box component="code" sx={{
            backgroundColor: (theme) => theme.palette.mode === 'dark' ? 'grey.800' : 'grey.200',
            px: 1,
            py: 0.5,
            borderRadius: 0.5,
            fontFamily: "monospace",
          }}>
            {JSON.stringify(roles)}
          </Box>
        </Typography>
      </Box>
      <Box sx={{ mb: 4 }}>
        <Typography variant="body1">
          Admin detected: <strong>{String(Boolean(hasAdmin))}</strong>
        </Typography>
      </Box>

      <Typography variant="body2" color="text.secondary">
        Open <Link href="/" style={{ color: 'inherit' }}>app home</Link> to see the navbar. If you are signed in as an admin, the Admin menu item should appear in the avatar menu.
      </Typography>
    </Container>
  );
}
