import { inject, injectable } from 'tsyringe';
import { DI_TOKENS } from '@/infrastructure/di/tokens';
import type { IAuctionRepository } from '@/application/interfaces/IAuctionRepository';
import type { UiAuction } from '@/features/auction/types/auction';

@injectable()
export class GetAuctions {
  constructor(@inject(DI_TOKENS.AuctionRepository) private readonly repo: IAuctionRepository) {}

  execute(status?: 'Open' | 'Closed' | 'Draft'): Promise<UiAuction[]> {
    return this.repo.getAuctions(status);
  }
}

