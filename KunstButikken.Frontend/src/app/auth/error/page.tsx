import AuthErrorClient from "@/features/auth/components/AuthErrorClient";

export default async function AuthErrorPage({ searchParams }: { searchParams?: Promise<{ error?: string | string[] }> }) {
  const sp = (await searchParams) ?? {};
  const raw = sp.error;
  const error = Array.isArray(raw) ? raw[0] : raw ?? null;
  return <AuthErrorClient error={error} />;
}
