import 'reflect-metadata';
import { ProfileRepository } from '@/infrastructure/repositories/ProfileRepository';
import type { ApiClient } from '@/infrastructure/http/types';
import type { UserProfile } from '@/features/profile/types/profile';

describe('ProfileRepository', () => {
  const baseUrl = 'http://gateway.test';
  let apiClient: jest.Mocked<ApiClient>;

  beforeEach(() => {
    process.env.NEXT_PUBLIC_API_GATEWAY = baseUrl;
    apiClient = {
      get: jest.fn(),
      post: jest.fn(),
      put: jest.fn(),
      delete: jest.fn(),
      patch: jest.fn(),
    } as unknown as jest.Mocked<ApiClient>;
  });

  it('fetches profile via apiClient', async () => {
    const profile: UserProfile = {
      id: 'p1',
      displayName: 'Demo',
      fullName: 'Demo User',
      email: 'demo@example.com',
      isSeller: false,
      isAdmin: false,
    };
    apiClient.get.mockResolvedValue(profile);
    const repo = new ProfileRepository(apiClient);

    await expect(repo.getProfile()).resolves.toEqual(profile);
    expect(apiClient.get).toHaveBeenCalledWith(`${baseUrl}/api/profile/me`);
  });

  it('updates profile via apiClient.put', async () => {
    const profile = { displayName: 'New', fullName: 'Name', email: 'n@example.com', isSeller: true, isAdmin: false } as UserProfile;
    apiClient.put.mockResolvedValue(profile);
    const repo = new ProfileRepository(apiClient);

    await expect(repo.updateProfile(profile)).resolves.toEqual(profile);
    expect(apiClient.put).toHaveBeenCalledWith(`${baseUrl}/api/profile`, profile);
  });

  it('promotes admin via apiClient.post', async () => {
    apiClient.post.mockResolvedValue(undefined);
    const repo = new ProfileRepository(apiClient);

    await expect(repo.promoteToAdmin('user@example.com')).resolves.toBeUndefined();
    expect(apiClient.post).toHaveBeenCalledWith(`${baseUrl}/api/admin/profiles/make-admin-by-email`, { email: 'user@example.com' });
  });
});
