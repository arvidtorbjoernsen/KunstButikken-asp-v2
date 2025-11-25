"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import LanguageIcon from "@mui/icons-material/Language";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Tooltip from "@mui/material/Tooltip";
import React from "react";

export default function NavbarLanguageMenu() {
  const { t, locale, setLocale } = useTranslations();
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const open = Boolean(anchorEl);
  const onOpen = (e: React.MouseEvent<HTMLElement>) => setAnchorEl(e.currentTarget);
  const onClose = () => setAnchorEl(null);

  return (
    <>
      <Tooltip title={t("nav.language") || "Language"}>
        <IconButton color="inherit" onClick={onOpen} aria-label={t("aria.language")} sx={{ p: 0.5 }}>
          {/* Show flag for current locale; fall back to globe icon if unknown */}
          {(() => {
            const flags: Record<string, string> = { nb: "🇳🇴", en: "🇬🇧", es: "🇪🇸", "pt-PT": "🇵🇹", "pt-BR": "🇧🇷" };
            const flag = flags[locale] ?? null;
            return flag ? (
              <Box component="span" sx={{ fontSize: 20, lineHeight: 1 }}>{flag}</Box>
            ) : (
              <LanguageIcon sx={{ fontSize: 20 }} />
            );
          })()}
        </IconButton>
      </Tooltip>

      <Menu anchorEl={anchorEl} open={open} onClose={onClose}>
        <MenuItem selected={locale === "nb"} onClick={() => { setLocale("nb"); onClose(); }}>Norsk (NB)</MenuItem>
        <MenuItem selected={locale === "en"} onClick={() => { setLocale("en"); onClose(); }}>English</MenuItem>
        <MenuItem selected={locale === "es"} onClick={() => { setLocale("es"); onClose(); }}>Español</MenuItem>
        <MenuItem selected={locale === "pt-PT"} onClick={() => { setLocale("pt-PT"); onClose(); }}>Português (PT)</MenuItem>
        <MenuItem selected={locale === "pt-BR"} onClick={() => { setLocale("pt-BR"); onClose(); }}>Português (BR)</MenuItem>
      </Menu>
    </>
  );
}
