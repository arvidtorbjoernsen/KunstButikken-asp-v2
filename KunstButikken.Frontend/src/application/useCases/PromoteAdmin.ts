import { inject, injectable } from 'tsyringe';
import type { IProfileRepository } from '@/application/interfaces/IProfileRepository';
import { DI_TOKENS } from '@/infrastructure/di/tokens';

@injectable()
export class PromoteAdmin {
  constructor(@inject(DI_TOKENS.ProfileRepository) private readonly repo: IProfileRepository) {}

  execute(email: string): Promise<void> {
    return this.repo.promoteToAdmin(email);
  }
}

