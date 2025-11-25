"use client";

import React from "react";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import PaletteIcon from "@mui/icons-material/Palette";
import LinkButton from "@/features/navigation/components/LinkButton";

export default function NavbarBrand() {
  return (
    <Box sx={{ display: "flex", alignItems: "center", minWidth: 0, flexShrink: 0, gap: 0.5 }}>
      <LinkButton href="/" color="inherit" sx={{ textTransform: "none", px: 0, minWidth: 0, gap: 0.5 }}>
        <PaletteIcon sx={{ fontSize: 24 }} />
        <Typography variant="h6" component="span" sx={{ fontWeight: 700 }}>
          KunstButikken
        </Typography>
      </LinkButton>
    </Box>
  );
}

