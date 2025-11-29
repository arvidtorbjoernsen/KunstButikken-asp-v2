import type { UserProfile } from '@/features/profile/types/profile';

export interface IProfileRepository {
  getProfile(): Promise<UserProfile>;
  updateProfile(profile: UserProfile): Promise<UserProfile>;
  promoteToAdmin(email: string): Promise<void>;
}

