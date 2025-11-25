module.exports = {
  // Use ts-jest preset so TypeScript + JSX are handled correctly
  preset: 'ts-jest',
  testEnvironment: 'jsdom',
  transform: {
    '^.+\\.(ts|tsx)$': ['ts-jest', { tsconfig: 'tsconfig.jest.json' }],
  },
  moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'json'],
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  moduleNameMapper: {
    '^.+\\.(css|less|scss|sass)$': 'identity-obj-proxy',
    '^@/(.*)$': '<rootDir>/src/$1',
  },
  transformIgnorePatterns: ['/node_modules/'],
  // Ignore E2E/playwright tests and Next build output when running Jest unit tests
  testPathIgnorePatterns: [
    '<rootDir>/.next/',
    '<rootDir>/node_modules/',
    '<rootDir>/tests/e2e/',
    '<rootDir>/tests/e2e/.*',
    '<rootDir>/tests/responsive.spec.ts',
  ],
  // Coverage settings
  collectCoverage: true,
  coverageDirectory: '<rootDir>/coverage/jest',
  coverageReporters: ['text', 'lcov', 'cobertura'],
  coveragePathIgnorePatterns: ['<rootDir>/.next/', '<rootDir>/node_modules/', '<rootDir>/public/'],
};
