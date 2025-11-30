import 'reflect-metadata';
import { inject, injectable } from 'tsyringe';
import type { IProfileRepository } from '@/application/interfaces/IProfileRepository';
import type { ApiClient } from '@/infrastructure/http/types';
import { DI_TOKENS } from '@/infrastructure/di/tokens';
import type { UserProfile } from '@/features/profile/types/profile';
import { buildServiceUrl } from '@/shared/config';

@injectable()
export class ProfileRepository implements IProfileRepository {
  constructor(@inject(DI_TOKENS.ApiClient) private readonly apiClient: ApiClient) {}

  async getProfile(): Promise<UserProfile> {
    const url = buildServiceUrl('USER', '/me');
    return this.apiClient.get<UserProfile>(url);
  }

  async updateProfile(profile: UserProfile): Promise<UserProfile> {
    const url = buildServiceUrl('USER');
    return this.apiClient.put<UserProfile>(url, profile);
  }

  async promoteToAdmin(email: string): Promise<void> {
    const url = buildServiceUrl('ADMIN', '/profiles/make-admin-by-email');
    await this.apiClient.post(url, { email });
  }
}
