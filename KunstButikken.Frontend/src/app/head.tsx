import React from 'react';
import { metadata } from './layout';

export default function Head() {
  return (
    <>
      <title>{String(metadata.title)}</title>
      <meta name="description" content={metadata.description ?? ''} />
    </>
  );
}
