"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { useColorMode } from "@/shared/providers/ThemeProvider";
import Brightness4Icon from "@mui/icons-material/Brightness4";
import Brightness7Icon from "@mui/icons-material/Brightness7";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";


export default function ThemeToggle() {
  const { mode, toggleMode } = useColorMode();
  const { t } = useTranslations();
  return (
    <Tooltip title={t("nav.theme")}>
      <IconButton
        onClick={toggleMode}
        color="inherit"
        aria-label={t("aria.toggleTheme")}
        sx={{ p: 0.5 }}
      >
        {mode === "dark" ? <Brightness7Icon sx={{ fontSize: 20 }} /> : <Brightness4Icon sx={{ fontSize: 20 }} />}
      </IconButton>
    </Tooltip>
  );
}
