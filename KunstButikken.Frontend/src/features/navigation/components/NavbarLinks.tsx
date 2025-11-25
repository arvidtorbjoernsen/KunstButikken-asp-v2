"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import LinkButton from "@/features/navigation/components/LinkButton";
import { tokens } from "@/features/ui/theme/tokens";
import BrushIcon from "@mui/icons-material/Brush";
import GavelIcon from "@mui/icons-material/Gavel";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import { useRouter } from "next/navigation";

export default function NavbarLinks() {
  const { t } = useTranslations();
  const router = useRouter();

  return (
    <>
      {/* Icon-only buttons for small-medium screens (sm to md) */}
      <Box
        sx={{
          display: { xs: "none", sm: "flex", md: "none" },
          alignItems: "center",
          justifyContent: "center",
          gap: tokens.space.sm,
          minWidth: 0,
          flex: 1,
        }}
      >
        <Tooltip title={t("nav.art")}>
          <IconButton onClick={() => router.push('/art')} color="inherit" aria-label={t("nav.art")}>
            <BrushIcon />
          </IconButton>
        </Tooltip>
        <Tooltip title={t("nav.auctions")}>
          <IconButton onClick={() => router.push('/art/auctions')} color="inherit" aria-label={t("nav.auctions")}>
            <GavelIcon />
          </IconButton>
        </Tooltip>
      </Box>

      {/* Full buttons with text for large screens (md+) */}
      <Box
        sx={{
          display: { xs: "none", md: "flex" },
          alignItems: "center",
          justifyContent: "center",
          gap: tokens.space.md,
          minWidth: 0,
          flex: 1,
        }}
      >
        <LinkButton href="/art" color="inherit" startIcon={<BrushIcon />}>
          {t("nav.art")}
        </LinkButton>
        <LinkButton href="/art/auctions" color="inherit" startIcon={<GavelIcon />}>
          {t("nav.auctions")}
        </LinkButton>
      </Box>
    </>
  );
}
