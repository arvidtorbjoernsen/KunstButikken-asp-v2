import { inject, injectable } from 'tsyringe';
import type { IArtRepository } from '@/application/interfaces/IArtRepository';
import type { UiArt } from '@/features/art/types/art';
import { DI_TOKENS } from '@/infrastructure/di/tokens';

@injectable()
export class GetFeaturedArt {
  constructor(@inject(DI_TOKENS.ArtRepository) private readonly artRepo: IArtRepository) {}

  execute(limit?: number): Promise<UiArt[]> {
    return this.artRepo.getFeatured(limit);
  }
}
