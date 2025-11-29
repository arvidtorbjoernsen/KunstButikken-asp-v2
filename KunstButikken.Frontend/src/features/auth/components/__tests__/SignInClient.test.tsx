import { render, screen, waitFor } from '@testing-library/react';
import SignInClient from '../SignInClient';

jest.mock('@/features/auth/lib/keycloak', () => ({
  useKeycloak: () => ({ keycloak: null })
}));
jest.mock('next/navigation', () => ({
  useRouter: () => ({ replace: jest.fn(), push: jest.fn() })
}));
jest.mock('@/features/auth/lib/gateway-session', () => ({
  exchangeCodeForGatewaySession: jest.fn(() => Promise.resolve({ sessionId: '1', userId: 'u', roles: [], expiresAt: new Date().toISOString() }))
}));

const { exchangeCodeForGatewaySession } = jest.requireMock('@/features/auth/lib/gateway-session');

describe('SignInClient', () => {
  it('shows establishing session message when code present', async () => {
    render(<SignInClient code="abc" redirectTo="/profile" />);
    expect(screen.getByText(/Establishing secure session/i)).toBeInTheDocument();
    await waitFor(() => expect(exchangeCodeForGatewaySession).toHaveBeenCalled());
  });

  it('shows checking auth when no keycloak', () => {
    render(<SignInClient />);
    expect(screen.getByText(/Checking authentication status/i)).toBeInTheDocument();
  });
});
