"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { parseResponse } from '@/shared/api';
import type { ApiArt } from "@/features/art/types/art";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import React, { useState } from "react";

export default function NewArtPage() {
  const router = useRouter();
  const { t } = useTranslations();

  const [titleEn, setTitleEn] = useState("");
  const [titleNb, setTitleNb] = useState("");
  const [descriptionEn, setDescriptionEn] = useState("");
  const [descriptionNb, setDescriptionNb] = useState("");
  const [artist, setArtist] = useState("");
  const [price, setPrice] = useState("");
  const [imageUrl, setImageUrl] = useState(""); // still supported when user pastes a URL
  const [file, setFile] = useState<File | null>(null); // new: optional file upload
  const [error, setError] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);

  const getApiBase = () =>
    (process.env.NEXT_PUBLIC_API_ART || process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5012").replace(/\/$/, "");

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setOk(null);
    try {
      const base = getApiBase();

      // Step 1: Create the art as Draft. If the user only provided a URL and no file, include it.
      const res = await fetch(`${base}/api/art`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          titleEn,
          titleNb,
          descriptionEn,
          descriptionNb,
          artist,
          price: Number(price),
          imageUrl: file ? "" : imageUrl, // ignore imageUrl when uploading a file
        }),
      });
      if (!res.ok) {
        const msg = await res.text();
        setError(msg || `${t("form.newArt.createError")} (${res.status})`);
        return;
      }
      const created = await parseResponse<Partial<ApiArt>>(res);
      // Some backends may return `id` or `Id` — be defensive and read either property.
      const createdAny = created as Partial<ApiArt> & Record<string, unknown>;
      const artId = (createdAny.id ?? createdAny.Id) as string | number | undefined;

      // Step 2: If file selected, upload to ArtService which stores to Azurite/Azure and sets ImageUrl
      if (artId && file) {
        const form = new FormData();
        form.append("file", file);
        const uploadRes = await fetch(`${base}/api/art/${artId}/upload`, {
          method: "POST",
          body: form,
        });
        if (!uploadRes.ok) {
          const msg = await uploadRes.text();
          setError(msg || `Image upload failed (${uploadRes.status})`);
          return;
        }
      }

      setOk(t("form.newArt.success"));
      if (artId) {
        router.push(`/art/${artId}`);
      } else {
        router.push('/art');
      }
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : String(e);
      setError(message);
    }
  };

  return (
    <Container sx={{ py: 6 }}>
      <Typography variant="h4" gutterBottom>{t("form.newArt.title")}</Typography>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {ok && <Alert severity="success" sx={{ mb: 2 }}>{ok}</Alert>}
      <Box component="form" onSubmit={onSubmit} sx={{ display: "flex", flexDirection: "column", gap: 2, maxWidth: { xs: '100%', sm: 600 }, width: '100%' }}>
        <TextField fullWidth label={t("form.title") + " (English)"} value={titleEn} onChange={(e) => setTitleEn(e.target.value)} required />
        <TextField fullWidth label={t("form.title") + " (Norwegian)"} value={titleNb} onChange={(e) => setTitleNb(e.target.value)} required />
        <TextField fullWidth label={t("form.description") + " (English)"} value={descriptionEn} onChange={(e) => setDescriptionEn(e.target.value)} multiline rows={4} />
        <TextField fullWidth label={t("form.description") + " (Norwegian)"} value={descriptionNb} onChange={(e) => setDescriptionNb(e.target.value)} multiline rows={4} />
        <TextField fullWidth label={t("form.artist")} value={artist} onChange={(e) => setArtist(e.target.value)} required />
        <TextField fullWidth label={t("form.price")} type="number" value={price} onChange={(e) => setPrice(e.target.value)} required />
        {/* Keep existing Image URL input for backwards compatibility */}
        <TextField fullWidth label={t("form.imageUrl")} value={imageUrl} onChange={(e) => setImageUrl(e.target.value)} />
        {/* New: optional file input for image upload */}
        <input
          type="file"
          accept="image/*"
          onChange={(e) => setFile(e.target.files && e.target.files[0] ? e.target.files[0] : null)}
          aria-label={t("form.imageFile")}
          data-testid="image-file-input"
        />
        <Button type="submit" variant="contained" size="large" sx={{ alignSelf: 'stretch' }}>{t("form.createButton")}</Button>
      </Box>
    </Container>
  );
}
