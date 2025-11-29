import 'reflect-metadata';
import { container } from 'tsyringe';
import type { ApiClient } from '@/infrastructure/http/types';
import { createApiClient } from '@/infrastructure/http/apiClientFactory';
import { DI_TOKENS } from './tokens';
import { ArtRepository } from '@/infrastructure/repositories/ArtRepository';
import type { IArtRepository } from '@/application/interfaces/IArtRepository';
import { AuctionRepository } from '@/infrastructure/repositories/AuctionRepository';
import type { IAuctionRepository } from '@/application/interfaces/IAuctionRepository';
import { ProfileRepository } from '@/infrastructure/repositories/ProfileRepository';
import type { IProfileRepository } from '@/application/interfaces/IProfileRepository';

export function registerInfrastructureServices() {
  if (!container.isRegistered(DI_TOKENS.ApiClient)) {
    container.registerInstance<ApiClient>(DI_TOKENS.ApiClient, createApiClient());
  }
  if (!container.isRegistered(DI_TOKENS.ArtRepository)) {
    container.registerSingleton<IArtRepository>(DI_TOKENS.ArtRepository, ArtRepository);
  }
  if (!container.isRegistered(DI_TOKENS.AuctionRepository)) {
    container.registerSingleton<IAuctionRepository>(DI_TOKENS.AuctionRepository, AuctionRepository);
  }
  if (!container.isRegistered(DI_TOKENS.ProfileRepository)) {
    container.registerSingleton<IProfileRepository>(DI_TOKENS.ProfileRepository, ProfileRepository);
  }
}

export function getContainer() {
  registerInfrastructureServices();
  return container;
}
