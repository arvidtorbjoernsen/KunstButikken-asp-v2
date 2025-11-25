"use client";

import React from "react";
import Button, { ButtonProps } from "@mui/material/Button";
import NextLink from "next/link";

export type LinkButtonProps = Omit<ButtonProps, "component" | "href"> & {
  href: string;
};

export default function LinkButton({ href, ...rest }: LinkButtonProps) {
  // We pass Next.js Link as the underlying component. This is safe in a client component.
  return <Button {...rest} component={NextLink as React.ElementType} href={href} />;
}
