import { renderHook } from '@testing-library/react';
import { KeycloakProvider, useKeycloak } from '../keycloak';

describe('KeycloakProvider', () => {
  test('placeholder test (provider renders)', () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => <KeycloakProvider>{children}</KeycloakProvider>;
    const { result } = renderHook(() => useKeycloak(), { wrapper });
    expect(result.current.loading).toBe(false);
  });
});
