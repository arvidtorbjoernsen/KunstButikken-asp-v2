import 'reflect-metadata';
import { GetFeaturedArt } from '@/application/useCases/GetFeaturedArt';
import { GetAllArt } from '@/application/useCases/GetAllArt';
import { GetArtById } from '@/application/useCases/GetArtById';
import type { IArtRepository } from '@/application/interfaces/IArtRepository';
import type { UiArt } from '@/features/art/types/art';

describe('Art use cases', () => {
  const sampleArt: UiArt = {
    id: 'art-1',
    titleNb: 'Tittel',
    titleEn: 'Title',
    price: 500,
  };

  it('GetFeaturedArt delegates to repository', async () => {
    const repo: IArtRepository = {
      getFeatured: jest.fn().mockResolvedValue([sampleArt]),
      getAll: jest.fn(),
      getById: jest.fn(),
    };
    const useCase = new GetFeaturedArt(repo);

    await expect(useCase.execute(3)).resolves.toEqual([sampleArt]);
    expect(repo.getFeatured).toHaveBeenCalledWith(3);
  });

  it('GetAllArt delegates to repository', async () => {
    const repo: IArtRepository = {
      getFeatured: jest.fn(),
      getAll: jest.fn().mockResolvedValue([sampleArt]),
      getById: jest.fn(),
    };
    const useCase = new GetAllArt(repo);

    await expect(useCase.execute()).resolves.toEqual([sampleArt]);
    expect(repo.getAll).toHaveBeenCalled();
  });

  it('GetArtById delegates to repository', async () => {
    const repo: IArtRepository = {
      getFeatured: jest.fn(),
      getAll: jest.fn(),
      getById: jest.fn().mockResolvedValue(sampleArt),
    };
    const useCase = new GetArtById(repo);

    await expect(useCase.execute('art-1')).resolves.toEqual(sampleArt);
    expect(repo.getById).toHaveBeenCalledWith('art-1');
  });
});

