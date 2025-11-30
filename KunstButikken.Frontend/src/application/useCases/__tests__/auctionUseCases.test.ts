import 'reflect-metadata';
import { PlaceBid } from '@/application/useCases/PlaceBid';
import { UpdateAuction } from '@/application/useCases/UpdateAuction';
import type { IAuctionRepository, UpdateAuctionPayload } from '@/application/interfaces/IAuctionRepository';

describe('Auction use cases', () => {
  it('PlaceBid delegates to repository', async () => {
    const repo: IAuctionRepository = {
      getAuctions: jest.fn(),
      getById: jest.fn(),
      updateAuction: jest.fn(),
      placeBid: jest.fn().mockResolvedValue(undefined),
    };
    const useCase = new PlaceBid(repo);

    await expect(useCase.execute('auction-123', 500)).resolves.toBeUndefined();
    expect(repo.placeBid).toHaveBeenCalledWith('auction-123', 500);
  });

  it('UpdateAuction delegates to repository', async () => {
    const payload: UpdateAuctionPayload = {
      startsAt: new Date().toISOString(),
      endsAt: new Date(Date.now() + 3600_000).toISOString(),
      startingPrice: 1000,
      reservePrice: 1500,
    };
    const repo: IAuctionRepository = {
      getAuctions: jest.fn(),
      getById: jest.fn(),
      updateAuction: jest.fn().mockResolvedValue(undefined),
      placeBid: jest.fn(),
    };
    const useCase = new UpdateAuction(repo);

    await expect(useCase.execute('auction-123', payload)).resolves.toBeUndefined();
    expect(repo.updateAuction).toHaveBeenCalledWith('auction-123', payload);
  });
});
