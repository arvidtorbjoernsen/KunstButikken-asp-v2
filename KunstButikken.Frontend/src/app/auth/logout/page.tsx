"use client";

import { useKeycloak } from "@/features/auth/lib/keycloak";
import LogoutClient from "./LogoutClient";

export default function LogoutPage() {
  const { keycloak } = useKeycloak();

  // Build Keycloak logout URL
  const keycloakIssuer = process.env.NEXT_PUBLIC_KEYCLOAK_ISSUER
    || (process.env.NEXT_PUBLIC_KEYCLOAK_BASE_URL
      ? `${process.env.NEXT_PUBLIC_KEYCLOAK_BASE_URL}/realms/${process.env.NEXT_PUBLIC_KEYCLOAK_REALM || 'kunstbutikken'}`
      : null);

  const idToken = keycloak?.idToken;

  return <LogoutClient keycloakIssuer={keycloakIssuer} idToken={idToken || undefined} />;
}
