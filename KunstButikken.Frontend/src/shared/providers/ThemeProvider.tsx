"use client";

import { createAppTheme, type Mode } from "@/features/ui/theme/theme";
import CssBaseline from "@mui/material/CssBaseline";
import { ThemeProvider as MuiThemeProvider } from "@mui/material/styles";
import React, { createContext, useContext, useEffect, useMemo, useState } from "react";

type ColorModeContextValue = {
  mode: Mode;
  toggleMode: () => void;
};

const ColorModeContext = createContext<ColorModeContextValue>({
  mode: "light",
  toggleMode: () => {},
});

export function useColorMode() {
  return useContext(ColorModeContext);
}

/**
 * Material-UI Theme Provider with dark/light mode support
 * Handles theme persistence and initialization to prevent hydration mismatches
 * Similar to Angular's theme handling but uses React context
 */
export default function ThemeProvider({ children }: { children: React.ReactNode }) {
  // Initialize to a deterministic value to match server-rendered HTML and avoid hydration mismatches.
  const [mode, setMode] = useState<Mode>("light");

  // After mount, read persisted preference or system preference and apply it.
  useEffect(() => {
    try {
      const stored = window.localStorage.getItem("mui-mode") as Mode | null;
      if (stored === "light" || stored === "dark") {
        setMode(stored);
        return;
      }

      if (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches) {
        setMode("dark");
      }
    } catch {
      // ignore and keep default
    }
  }, []);

  useEffect(() => {
    try {
      window.localStorage.setItem("mui-mode", mode);
    } catch {}
    try {
      document.documentElement.setAttribute("data-theme", mode);
    } catch {}
  }, [mode]);

  const colorMode = useMemo(
    () => ({ mode, toggleMode: () => setMode((m) => (m === "light" ? "dark" : "light")) }),
    [mode]
  );

  const theme = useMemo(() => createAppTheme(mode), [mode]);

  return (
    <ColorModeContext.Provider value={colorMode}>
      <MuiThemeProvider theme={theme}>
        <CssBaseline />
        {children}
      </MuiThemeProvider>
    </ColorModeContext.Provider>
  );
}
