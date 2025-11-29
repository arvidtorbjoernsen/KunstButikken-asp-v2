import { inject, injectable } from 'tsyringe';
import type { IAuctionRepository, UpdateAuctionPayload } from '@/application/interfaces/IAuctionRepository';
import { DI_TOKENS } from '@/infrastructure/di/tokens';

@injectable()
export class UpdateAuction {
  constructor(@inject(DI_TOKENS.AuctionRepository) private readonly repo: IAuctionRepository) {}

  execute(id: string, payload: UpdateAuctionPayload): Promise<void> {
    return this.repo.updateAuction(id, payload);
  }
}

