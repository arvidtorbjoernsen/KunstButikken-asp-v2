"use client";

import React from "react";
import AddPhotoAlternateIcon from "@mui/icons-material/AddPhotoAlternate";
import LogoutIcon from "@mui/icons-material/Logout";
import Button from "@mui/material/Button";
import LinkButton from "@/features/navigation/components/LinkButton";
import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { useRouter } from "next/navigation";

export type NavbarAuthActionsProps = {
  isAuthed: boolean;
  isSeller: boolean;
};

export default function NavbarAuthActions({ isAuthed, isSeller }: NavbarAuthActionsProps) {
  const { t } = useTranslations();
  const router = useRouter();

  if (!isAuthed) {
    return (
      <>
        <LinkButton href="/auth/signin" size="small" color="inherit" sx={{ display: { xs: "none", md: "inline-flex" } }}>
          {t("nav.signin")}
        </LinkButton>
        <LinkButton href="/auth/register" size="small" variant="outlined" sx={{ ml: { xs: 0.5, md: 1 } }}>
          {t("nav.register")}
        </LinkButton>
      </>
    );
  }

  return (
    <>
      {isSeller && (
        <LinkButton
          href="/art/sell/new-art"
          size="small"
          color="inherit"
          startIcon={<AddPhotoAlternateIcon />}
          sx={{ display: { xs: "none", md: "inline-flex" }, minWidth: 56, px: 1 }}
        >
          {t("nav.sellArt")}
        </LinkButton>
      )}
      <LinkButton href="/auth/me" size="small" variant="outlined" sx={{ ml: { xs: 0.5, md: 1 } }}>
        {t("nav.profile")}
      </LinkButton>
      <Button 
        onClick={() => router.push('/debug/clear-session')}
        size="small" 
        color="inherit"
        startIcon={<LogoutIcon />}
        sx={{ ml: { xs: 0.5, md: 1 } }}
      >
        {t("nav.logout")}
      </Button>
    </>
  );
}

