import 'reflect-metadata';
// Tests for DI container registration branches

// Import real tsyringe decorators but mock the container instance methods
const realTsyringe = jest.requireActual('tsyringe');

jest.mock('tsyringe', () => {
  const actual = jest.requireActual('tsyringe');
  return {
    ...actual,
    container: {
      isRegistered: jest.fn(),
      registerInstance: jest.fn(),
      registerSingleton: jest.fn(),
    },
  };
});

jest.mock('@/infrastructure/http/apiClientFactory', () => ({
  createApiClient: jest.fn(() => ({ foo: 'client' })),
}));

import { registerInfrastructureServices } from '../container';
import { container } from 'tsyringe';
import { DI_TOKENS } from '../tokens';

describe('registerInfrastructureServices', () => {
  beforeEach(() => {
    jest.resetModules();
    (container.isRegistered as jest.Mock).mockReset();
    (container.registerInstance as jest.Mock).mockReset();
    (container.registerSingleton as jest.Mock).mockReset();
  });

  test('registers all when none registered', () => {
    (container.isRegistered as jest.Mock).mockReturnValue(false);
    registerInfrastructureServices();
    expect(container.registerInstance).toHaveBeenCalled();
    expect(container.registerSingleton).toHaveBeenCalled();
  });

  test('skips registration when already registered', () => {
    (container.isRegistered as jest.Mock).mockReturnValue(true);
    registerInfrastructureServices();
    expect(container.registerInstance).not.toHaveBeenCalled();
    expect(container.registerSingleton).not.toHaveBeenCalled();
  });
});
