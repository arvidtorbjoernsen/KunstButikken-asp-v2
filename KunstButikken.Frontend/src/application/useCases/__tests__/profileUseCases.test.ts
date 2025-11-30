import 'reflect-metadata';
import { GetProfile } from '@/application/useCases/GetProfile';
import { UpdateProfile } from '@/application/useCases/UpdateProfile';
import { PromoteAdmin } from '@/application/useCases/PromoteAdmin';
import type { IProfileRepository } from '@/application/interfaces/IProfileRepository';
import type { UserProfile } from '@/features/profile/types/profile';

describe('Profile use cases', () => {
  const sampleProfile: UserProfile = {
    id: 'profile-1',
    userId: 'user-1',
    displayName: 'Test User',
    fullName: 'Testy Tester',
    email: 'test@example.com',
    isSeller: true,
    isAdmin: false,
  };

  it('GetProfile returns repository result', async () => {
    const repo: IProfileRepository = {
      getProfile: jest.fn().mockResolvedValue(sampleProfile),
      updateProfile: jest.fn(),
      promoteToAdmin: jest.fn(),
    };
    const useCase = new GetProfile(repo);

    await expect(useCase.execute()).resolves.toEqual(sampleProfile);
    expect(repo.getProfile).toHaveBeenCalled();
  });

  it('UpdateProfile forwards payload', async () => {
    const repo: IProfileRepository = {
      getProfile: jest.fn(),
      updateProfile: jest.fn().mockResolvedValue(sampleProfile),
      promoteToAdmin: jest.fn(),
    };
    const useCase = new UpdateProfile(repo);

    await expect(useCase.execute(sampleProfile)).resolves.toEqual(sampleProfile);
    expect(repo.updateProfile).toHaveBeenCalledWith(sampleProfile);
  });

  it('PromoteAdmin calls repository', async () => {
    const repo: IProfileRepository = {
      getProfile: jest.fn(),
      updateProfile: jest.fn(),
      promoteToAdmin: jest.fn().mockResolvedValue(undefined),
    };
    const useCase = new PromoteAdmin(repo);

    await expect(useCase.execute('admin@example.com')).resolves.toBeUndefined();
    expect(repo.promoteToAdmin).toHaveBeenCalledWith('admin@example.com');
  });
});

