import '@testing-library/jest-dom';
import 'cross-fetch/polyfill';
import React from 'react';
import { jest } from '@jest/globals';

const LinkMock = ({ children, href, ...props }: { children: React.ReactNode; href: string | { pathname?: string } }) =>
  React.createElement('a', { href: typeof href === 'string' ? href : href?.pathname ?? '#', ...props }, children);
LinkMock.displayName = 'NextLinkMock';

jest.mock('next/link', () => ({
  __esModule: true,
  default: LinkMock,
}));

jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    prefetch: jest.fn(),
    back: jest.fn(),
    forward: jest.fn(),
  }),
}));

const ensureIntersectionObserver = (): void => {
  if (typeof global.IntersectionObserver !== 'undefined') {
    return;
  }

  global.IntersectionObserver = class implements IntersectionObserver {
    readonly root: Element | Document | null = null;
    readonly rootMargin = '';
    readonly thresholds: ReadonlyArray<number> = [];

    observe(): void {}
    unobserve(): void {}
    disconnect(): void {}
    takeRecords(): IntersectionObserverEntry[] {
      return [];
    }
  };
};

export const applyGlobalTestSetup = (): void => {
  ensureIntersectionObserver();
};

applyGlobalTestSetup();
