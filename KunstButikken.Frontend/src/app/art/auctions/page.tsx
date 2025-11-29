import { getContainer } from '@/infrastructure/di/container';
import { GetAuctions } from '@/application/useCases/GetAuctions';

export const revalidate = 30;

export default async function AuctionsPage() {
  const container = getContainer();
  const getAuctionsUseCase = container.resolve(GetAuctions);
  const systemAuctions = await getAuctionsUseCase.execute('Open');
  const ClientPage = (await import('./page.client')).default;
  return <ClientPage initialAuctions={systemAuctions} />;
}
