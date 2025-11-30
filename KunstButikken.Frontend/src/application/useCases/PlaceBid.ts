import { inject, injectable } from 'tsyringe';
import type { IAuctionRepository } from '@/application/interfaces/IAuctionRepository';
import { DI_TOKENS } from '@/infrastructure/di/tokens';

@injectable()
export class PlaceBid {
  constructor(@inject(DI_TOKENS.AuctionRepository) private readonly repo: IAuctionRepository) {}

  execute(auctionId: string, amount: number): Promise<void> {
    return this.repo.placeBid(auctionId, amount);
  }
}
