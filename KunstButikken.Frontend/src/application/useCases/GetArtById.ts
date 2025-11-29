import { inject, injectable } from 'tsyringe';
import type { IArtRepository } from '@/application/interfaces/IArtRepository';
import { DI_TOKENS } from '@/infrastructure/di/tokens';
import type { UiArt } from '@/features/art/types/art';

@injectable()
export class GetArtById {
  constructor(@inject(DI_TOKENS.ArtRepository) private readonly repo: IArtRepository) {}

  execute(id: string): Promise<UiArt | null> {
    if (!id) return Promise.resolve(null);
    return this.repo.getById(id);
  }
}
