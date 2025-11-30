import { inject, injectable } from 'tsyringe';
import type { IAuctionRepository } from '@/application/interfaces/IAuctionRepository';
import { DI_TOKENS } from '@/infrastructure/di/tokens';
import type { CurrentUserContext } from '@/application/security/types';
import { ensureRole } from '@/application/security/ensureRole';
import type { UiAuction } from '@/features/auction/types/auction';

export type GetSellerAuctionsInput = {
  user: CurrentUserContext;
};

export type GetSellerAuctionsOutput = {
  mine: UiAuction[];
  others: UiAuction[];
};

@injectable()
export class GetSellerAuctions {
  constructor(@inject(DI_TOKENS.AuctionRepository) private readonly repo: IAuctionRepository) {}

  async execute({ user }: GetSellerAuctionsInput): Promise<GetSellerAuctionsOutput> {
    ensureRole(user, ['seller']);

    const mine = await this.repo.getMine(true, user.token);
    const others = (await this.repo.getAuctions('Open')).filter(a => a.sellerId !== user.id);

    return { mine, others };
  }
}
