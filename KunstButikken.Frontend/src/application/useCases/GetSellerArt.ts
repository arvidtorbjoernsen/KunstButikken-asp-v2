import { inject, injectable } from 'tsyringe';
import type { IArtRepository } from '@/application/interfaces/IArtRepository';
import { DI_TOKENS } from '@/infrastructure/di/tokens';
import type { CurrentUserContext } from '@/application/security/types';
import { ensureRole } from '@/application/security/ensureRole';
import type { UiArt } from '@/features/art/types/art';

export type GetSellerArtInput = {
  user: CurrentUserContext;
  includeUnverified?: boolean;
};

@injectable()
export class GetSellerArt {
  constructor(@inject(DI_TOKENS.ArtRepository) private readonly repo: IArtRepository) {}

  async execute({ user, includeUnverified = true }: GetSellerArtInput): Promise<UiArt[]> {
    ensureRole(user, ['seller']);
    return this.repo.getMine(includeUnverified, user.token);
  }
}
