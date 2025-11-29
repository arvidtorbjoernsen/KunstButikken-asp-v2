import { inject, injectable } from 'tsyringe';
import type { UiArt } from '@/features/art/types/art';
import type { IArtRepository } from '@/application/interfaces/IArtRepository';
import { DI_TOKENS } from '@/infrastructure/di/tokens';

@injectable()
export class GetAllArt {
  constructor(@inject(DI_TOKENS.ArtRepository) private readonly artRepo: IArtRepository) {}

  execute(): Promise<UiArt[]> {
    return this.artRepo.getAll();
  }
}
