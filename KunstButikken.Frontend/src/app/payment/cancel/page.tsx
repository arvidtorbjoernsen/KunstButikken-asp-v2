import { Container, Typography, Button, Box } from '@mui/material';
import Link from 'next/link';

export default function CancelPage() {
  return (
    <Container sx={{ py: 6 }}>
      <Box>
        <Typography variant="h4" component="h1" gutterBottom>
          Payment cancelled
        </Typography>
        <Typography variant="body1" sx={{ mb: 2 }}>
          You have cancelled the payment. No charges were made.
        </Typography>
        <Link href="/">
          <Button variant="contained">Back to home</Button>
        </Link>
      </Box>
    </Container>
  );
}
