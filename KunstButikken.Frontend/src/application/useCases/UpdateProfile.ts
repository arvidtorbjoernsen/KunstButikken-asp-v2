import { inject, injectable } from 'tsyringe';
import type { IProfileRepository } from '@/application/interfaces/IProfileRepository';
import { DI_TOKENS } from '@/infrastructure/di/tokens';
import type { UserProfile } from '@/features/profile/types/profile';

@injectable()
export class UpdateProfile {
  constructor(@inject(DI_TOKENS.ProfileRepository) private readonly repo: IProfileRepository) {}

  execute(profile: UserProfile): Promise<UserProfile> {
    return this.repo.updateProfile(profile);
  }
}

