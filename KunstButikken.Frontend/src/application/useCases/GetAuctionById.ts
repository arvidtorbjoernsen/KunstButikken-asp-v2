import { inject, injectable } from 'tsyringe';
import type { IAuctionRepository } from '@/application/interfaces/IAuctionRepository';
import { DI_TOKENS } from '@/infrastructure/di/tokens';
import type { UiAuction } from '@/features/auction/types/auction';

@injectable()
export class GetAuctionById {
  constructor(@inject(DI_TOKENS.AuctionRepository) private readonly repo: IAuctionRepository) {}

  execute(id: string): Promise<UiAuction | null> {
    if (!id) return Promise.resolve(null);
    return this.repo.getById(id);
  }
}
