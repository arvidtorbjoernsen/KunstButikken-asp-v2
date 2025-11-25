"use server";

// Server actions for authentication
// Note: Auth logic is now client-side via Keycloak provider

export async function logoutAction() {
  // Redirect to logout page which will handle Keycloak logout client-side
  const { redirect } = await import("next/navigation");
  redirect('/auth/logout');
}

// Note: All other profile/token actions have been removed.
// Use client-side Keycloak hooks and API client instead.
// See lib/keycloak.tsx and lib/api-client.ts
