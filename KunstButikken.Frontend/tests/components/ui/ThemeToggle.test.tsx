import React from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

// Provide a mutable mock that the jest.mock factory will read at render time.
let currentMode: 'light' | 'dark' = 'light';
const mockToggle = jest.fn();

// ThemeToggle imports `useColorMode` from '@/shared/providers/ThemeProvider'
// Mock that exact module so the component receives our test double.
jest.mock('@/shared/providers/ThemeProvider', () => {
  return {
    __esModule: true,
    useColorMode: () => ({ mode: currentMode, toggleMode: mockToggle }),
  };
});

// Import the component after the mock is defined so the mock is applied.
import ThemeToggle from '@/features/ui/components/ThemeToggle';

describe('ThemeToggle', () => {
  beforeEach(() => {
    mockToggle.mockClear();
    currentMode = 'light';
  });

  it('renders the toggle button and calls toggleMode on click', async () => {
    render(<ThemeToggle />);

    const btn = screen.getByRole('button', { name: /aria.toggleTheme/i });
    expect(btn).toBeInTheDocument();

    await userEvent.click(btn);
    expect(mockToggle).toHaveBeenCalledTimes(1);
  });

  it('renders differently when mode is dark (snapshot)', () => {
    currentMode = 'dark';
    const { container } = render(<ThemeToggle />);
    // When dark mode is active the Brightness7Icon should be rendered
    expect(container.querySelector('[data-testid="Brightness7Icon"]')).toBeInTheDocument();
  });
});
