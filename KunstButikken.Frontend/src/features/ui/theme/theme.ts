import { createTheme, type ThemeOptions } from "@mui/material/styles";
import { tokens } from "./tokens";

export type Mode = "light" | "dark";

export const brand = {
  primary: {
    light: "#8f62c2",
    main: "#6b3fa0",
    dark: "#4a2d74",
    contrastText: "#ffffff",
  },
  secondary: {
    light: "#ffd180",
    main: "#f5a524",
    dark: "#b87500",
    contrastText: "#1a130f",
  },
};

export function createAppTheme(mode: Mode) {
  const isDark = mode === "dark";

  const common: ThemeOptions = {
    palette: {
      mode,
      primary: brand.primary,
      secondary: brand.secondary,
      background: isDark
        ? { default: "#0f0d13", paper: "#17141f" }
        : { default: "#faf9fc", paper: "#ffffff" },
    },
    shape: { borderRadius: tokens.radii.md },
    spacing: tokens.base,
    typography: {
      fontFamily: 'Inter, ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Helvetica, Arial',
      h6: { fontWeight: 700 },
      button: { textTransform: "none", fontWeight: 600 },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          ':root': {
            '--kb-space-xs': `${tokens.space.xs}px`,
            '--kb-space-sm': `${tokens.space.sm}px`,
            '--kb-space-md': `${tokens.space.md}px`,
            '--kb-space-lg': `${tokens.space.lg}px`,
            '--kb-space-xl': `${tokens.space.xl}px`,
            '--kb-radius-sm': `${tokens.radii.sm}px`,
            '--kb-radius-md': `${tokens.radii.md}px`,
            '--kb-radius-lg': `${tokens.radii.lg}px`,
          },
          body: {
            scrollbarGutter: "stable both-edges",
          },
        },
      },
      MuiAppBar: {
        defaultProps: { elevation: 0, color: "default" },
        styleOverrides: {
          root: ({ theme }) => ({
            backdropFilter: "saturate(180%) blur(10px)",
            WebkitBackdropFilter: "saturate(180%) blur(10px)",
            borderBottom: "1px solid",
            borderColor: theme.palette.divider,
            borderRadius: 0,
            paddingLeft: tokens.space.md,
            paddingRight: tokens.space.md,
          }),
        },
      },
      MuiToolbar: {
        styleOverrides: {
          root: {
            minHeight: tokens.layout.navHeight.xs,
            paddingTop: tokens.space.sm,
            paddingBottom: tokens.space.sm,
            "@media (min-width:600px)": { minHeight: tokens.layout.navHeight.sm },
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: { borderRadius: tokens.radii.pill },
        },
      },
      MuiPaper: {
        defaultProps: { elevation: 0 },
        styleOverrides: {
          root: { borderRadius: tokens.radii.lg },
        },
      },
      MuiIconButton: {
        defaultProps: { size: "small", color: "inherit" },
      },
      MuiSvgIcon: {
        styleOverrides: { root: { fontSize: "1.2rem" } },
      },
      MuiUseMediaQuery: {
        defaultProps: { noSsr: true },
      },
    },
  };

  return createTheme(common);
}
