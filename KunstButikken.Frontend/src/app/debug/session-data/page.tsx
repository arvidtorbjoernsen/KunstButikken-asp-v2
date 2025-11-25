"use client";

import { useKeycloak } from "@/features/auth/lib/keycloak";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import SessionDebugClient from "./SessionDebugClient";

export default function SessionDataPage() {
  const { keycloak, loading } = useKeycloak();
  const router = useRouter();

  useEffect(() => {
    if (!loading && !keycloak?.authenticated) {
      router.push("/auth/signin");
    }
  }, [loading, keycloak?.authenticated, router]);

  if (loading || !keycloak?.authenticated) {
    return null;
  }

  // Convert Keycloak to session-like format
  const session = {
    user: {
      id: keycloak.tokenParsed?.sub,
      name: keycloak.tokenParsed?.name,
      email: keycloak.tokenParsed?.email,
      ...keycloak.tokenParsed,
    }
  };

  return <SessionDebugClient session={session} />;
}
