"use client";

import 'reflect-metadata';

import { createContext, useContext, useRef } from 'react';
import type { ReactNode } from 'react';
import type { DependencyContainer } from 'tsyringe';
import { getContainer, registerInfrastructureServices } from '@/infrastructure/di/container';

const ContainerContext = createContext<DependencyContainer | null>(null);

export default function DiProvider({ children }: { children: ReactNode }) {
  const containerRef = useRef<DependencyContainer | null>(null);

  if (!containerRef.current) {
    registerInfrastructureServices();
    containerRef.current = getContainer();
  }

  return (
    <ContainerContext.Provider value={containerRef.current}>
      {children}
    </ContainerContext.Provider>
  );
}

export function useContainer() {
  const container = useContext(ContainerContext);
  if (!container) {
    throw new Error('DI container not available; wrap tree with <DiProvider>.');
  }
  return container;
}
