"use client";

import type { LocalSession } from "@/features/auth/types/auth";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";

interface SessionDebugClientProps {
  session: LocalSession | null;
}

export default function SessionDebugClient({ session }: SessionDebugClientProps) {
  if (!session?.user) {
    return <Alert severity="info">No session data available</Alert>;
  }

  const user = session.user ?? {};

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h4" gutterBottom>
        Session Debug Information
      </Typography>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            User Information
          </Typography>
          <Stack spacing={1}>
            {user.name ? (
              <Typography>
                <strong>Name:</strong> {user.name as string}
              </Typography>
            ) : null}
            {user.email ? (
              <Typography>
                <strong>Email:</strong> {user.email as string}
              </Typography>
            ) : null}
            {user.sub ? (
              <Typography>
                <strong>User ID:</strong> {user.sub as string}
              </Typography>
            ) : null}
            {Array.isArray(user.roles) && user.roles.length > 0 ? (
              <Typography>
                <strong>Roles:</strong> {user.roles.join(", ")}
              </Typography>
            ) : null}
            <Typography>
              <strong>Admin:</strong> {user.isAdmin ? "Yes" : "No"}
            </Typography>
            <Typography>
              <strong>Seller:</strong> {user.isSeller ? "Yes" : "No"}
            </Typography>
            <Typography>
              <strong>Buyer:</strong> {user.isBuyer ? "Yes" : "No"}
            </Typography>
          </Stack>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Raw Session Data
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
            }}
          >
            {JSON.stringify(session, null, 2)}
          </Box>
        </CardContent>
      </Card>
    </Container>
  );
}
