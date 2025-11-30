import 'reflect-metadata';
import { container, DependencyContainer } from 'tsyringe';
import type { ApiClient } from '@/infrastructure/http/types';
import { createApiClient } from '@/infrastructure/http/apiClientFactory';
import { DI_TOKENS } from './tokens';
import { ArtRepository } from '@/infrastructure/repositories/ArtRepository';
import type { IArtRepository } from '@/application/interfaces/IArtRepository';
import { AuctionRepository } from '@/infrastructure/repositories/AuctionRepository';
import type { IAuctionRepository } from '@/application/interfaces/IAuctionRepository';
import { ProfileRepository } from '@/infrastructure/repositories/ProfileRepository';
import type { IProfileRepository } from '@/application/interfaces/IProfileRepository';

function registerServices(target: DependencyContainer) {
  if (!target.isRegistered(DI_TOKENS.ApiClient)) {
    target.registerInstance<ApiClient>(DI_TOKENS.ApiClient, createApiClient());
  }
  if (!target.isRegistered(DI_TOKENS.ArtRepository)) {
    target.registerSingleton<IArtRepository>(DI_TOKENS.ArtRepository, ArtRepository);
  }
  if (!target.isRegistered(DI_TOKENS.AuctionRepository)) {
    target.registerSingleton<IAuctionRepository>(DI_TOKENS.AuctionRepository, AuctionRepository);
  }
  if (!target.isRegistered(DI_TOKENS.ProfileRepository)) {
    target.registerSingleton<IProfileRepository>(DI_TOKENS.ProfileRepository, ProfileRepository);
  }
}

export function registerInfrastructureServices() {
  registerServices(container);
}

export function getContainer(): DependencyContainer {
  registerInfrastructureServices();
  return container;
}

export function createRequestScope(): DependencyContainer {
  const scope = container.createChildContainer();
  registerServices(scope);
  return scope;
}
