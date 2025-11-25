// Server component for SSR of auctions list
export default async function AuctionsPage() {
  const Client = (await import('./page.client')).default;
  return <Client />;
}
