import { apiClient } from '@/shared/api/api-client';
import type { ApiClient } from './types';

export function createApiClient(): ApiClient {
  return apiClient;
}
