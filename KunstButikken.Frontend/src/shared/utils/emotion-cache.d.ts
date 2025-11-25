declare module '@emotion/cache' {
  import type { EmotionCache } from '@emotion/react';
  export interface Options {
    key?: string;
    prepend?: boolean;
  }
  export default function createCache(options?: Options): EmotionCache;
}

