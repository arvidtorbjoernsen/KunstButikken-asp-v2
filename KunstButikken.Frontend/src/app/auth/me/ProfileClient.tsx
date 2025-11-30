"use client";

import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettings";
import InfoIcon from "@mui/icons-material/Info";
import LocalShippingIcon from "@mui/icons-material/LocalShipping";
import PendingIcon from "@mui/icons-material/Pending";
import PersonIcon from "@mui/icons-material/Person";
import RefreshIcon from "@mui/icons-material/Refresh";
import SaveIcon from "@mui/icons-material/Save";
import SettingsIcon from "@mui/icons-material/Settings";
import StoreIcon from "@mui/icons-material/Store";
import VerifiedIcon from "@mui/icons-material/Verified";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CardHeader from "@mui/material/CardHeader";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import FormControlLabel from "@mui/material/FormControlLabel";
import Snackbar from "@mui/material/Snackbar";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useContainer } from '@/presentation/providers/DiProvider';
import { GetProfile } from '@/application/useCases/GetProfile';
import { UpdateProfile } from '@/application/useCases/UpdateProfile';
import type { UserProfile } from '@/features/profile/types/profile';
import { useEffect, useMemo, useState } from "react";

export default function ProfileClient() {
  // State management
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState("");
  const [snackbarSeverity, setSnackbarSeverity] = useState<"success" | "error">("success");

  // Form state
  const [displayName, setDisplayName] = useState("");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [address, setAddress] = useState("");
  const [city, setCity] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [country, setCountry] = useState("");
  const [isSeller, setIsSeller] = useState(false);
  const [profileImageUrl, setProfileImageUrl] = useState("");

  const showSnackbar = (message: string, severity: "success" | "error" = "success") => {
    setSnackbarMessage(message);
    setSnackbarSeverity(severity);
    setSnackbarOpen(true);
  };

  // Load profile data on mount
  useEffect(() => {
    const loadProfile = async () => {
      setLoading(true);
      setError(null);

      try {
        const profileData = await getProfileUseCase.execute();

        setProfile(profileData);

        // Populate form with existing data
        setDisplayName(profileData.displayName || "");
        setFullName(profileData.fullName || "");
        setEmail(profileData.email || "");
        setPhoneNumber(profileData.phoneNumber || "");
        setAddress(profileData.address || "");
        setCity(profileData.city || "");
        setPostalCode(profileData.postalCode || "");
        setCountry(profileData.country || "");
        setIsSeller(profileData.isSeller === true);
        setProfileImageUrl(profileData.profileImageUrl || "");
      } catch (err) {
        console.error('[ProfileClient] Error loading profile:', err);
        const errorMessage = err instanceof Error ? err.message : "Failed to load profile";
        setError(`Failed to load profile. Please try again. (${errorMessage})`);
        showSnackbar("Failed to load profile", "error");
      } finally {
        setLoading(false);
      }
    };

    void loadProfile();
  }, []);

  const handleSave = async () => {
    if (!displayName || !fullName || !email) {
      showSnackbar("Please fill in all required fields correctly", "error");
      return;
    }

    setSaving(true);

    const updatedProfile: UserProfile = {
      ...profile,
      displayName,
      fullName,
      email,
      phoneNumber,
      address,
      city,
      postalCode,
      country,
      isSeller,
      isSellerVerified: profile?.isSellerVerified ?? false,
      isAdmin: profile?.isAdmin ?? false,
      profileImageUrl
    };

    try {
      await updateProfileUseCase.execute(updatedProfile);

      setProfile(updatedProfile);
      showSnackbar("Profile updated successfully!", "success");
    } catch (err) {
      console.error('Error updating profile:', err);
      showSnackbar("Failed to update profile. Please try again.", "error");
    } finally {
      setSaving(false);
    }
  };

  const handleReset = () => {
    if (profile) {
      setDisplayName(profile.displayName || "");
      setFullName(profile.fullName || "");
      setEmail(profile.email || "");
      setPhoneNumber(profile.phoneNumber || "");
      setAddress(profile.address || "");
      setCity(profile.city || "");
      setPostalCode(profile.postalCode || "");
      setCountry(profile.country || "");
      setIsSeller(profile.isSeller === true);
      setProfileImageUrl(profile.profileImageUrl || "");
      showSnackbar("Form reset to saved values", "success");
    }
  };

  const container = useContainer();
  const getProfileUseCase = useMemo(() => container.resolve(GetProfile), [container]);
  const updateProfileUseCase = useMemo(() => container.resolve(UpdateProfile), [container]);

  if (loading) {
    return (
      <Box sx={{ display: "flex", flexDirection: "column", alignItems: "center", py: 8 }}>
        <CircularProgress size={50} sx={{ mb: 2 }} />
        <Typography>Loading your profile...</Typography>
      </Box>
    );
  }

  if (error && !profile) {
    return (
      <Card>
        <CardContent>
          <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
            <InfoIcon color="error" />
            <Typography>{error}</Typography>
          </Box>
        </CardContent>
      </Card>
    );
  }

  return (
    <Box>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 1 }}>
          <AccountCircleIcon sx={{ fontSize: 40, color: "primary.main" }} />
          <Box>
            <Typography variant="h4">My Profile</Typography>
            <Typography variant="body2" color="text.secondary">
              Manage your account and auction preferences
            </Typography>
          </Box>
        </Box>
      </Box>

      {/* Profile Overview Card */}
      <Card sx={{ mb: 3 }}>
        <CardHeader
          avatar={
            profileImageUrl ? (
              <Box
                component="img"
                src={profileImageUrl}
                alt="Profile"
                sx={{ width: 60, height: 60, borderRadius: "50%" }}
              />
            ) : (
              <AccountCircleIcon sx={{ fontSize: 60 }} />
            )
          }
          title={profile?.displayName}
          subheader={profile?.email}
        />
        <CardContent>
          <Stack direction="row" spacing={1} flexWrap="wrap">
            {profile?.isSeller && (
              <>
                <Chip icon={<StoreIcon />} label="Seller Account" color="primary" />
                {profile.isSellerVerified ? (
                  <Chip icon={<VerifiedIcon />} label="Verified" color="success" />
                ) : (
                  <Chip icon={<PendingIcon />} label="Verification Pending" color="warning" />
                )}
              </>
            )}
            {profile?.isAdmin && (
              <Chip icon={<AdminPanelSettingsIcon />} label="Administrator" color="error" />
            )}
          </Stack>
        </CardContent>
      </Card>

      {/* Personal Information */}
      <Card sx={{ mb: 3 }}>
        <CardHeader
          avatar={<PersonIcon />}
          title="Personal Information"
        />
        <Divider />
        <CardContent>
          <Stack spacing={3}>
            <TextField
              label="Display Name"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              fullWidth
              required
              helperText="This name will be visible to other users"
            />
            <TextField
              label="Full Name"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              fullWidth
              required
            />
            <TextField
              label="Email Address"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              fullWidth
              required
            />
            <TextField
              label="Phone Number"
              type="tel"
              value={phoneNumber}
              onChange={(e) => setPhoneNumber(e.target.value)}
              fullWidth
              helperText="Optional: For auction notifications"
            />
            <TextField
              label="Profile Image URL"
              value={profileImageUrl}
              onChange={(e) => setProfileImageUrl(e.target.value)}
              fullWidth
              helperText="Optional: URL to your profile picture"
            />
          </Stack>
        </CardContent>
      </Card>

      {/* Shipping Address */}
      <Card sx={{ mb: 3 }}>
        <CardHeader
          avatar={<LocalShippingIcon />}
          title="Shipping Address"
        />
        <Divider />
        <CardContent>
          <Stack spacing={3}>
            <TextField
              label="Street Address"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              fullWidth
              helperText="Used for shipping won auction items"
            />
            <Box sx={{ display: "flex", gap: 2 }}>
              <TextField
                label="Postal Code"
                value={postalCode}
                onChange={(e) => setPostalCode(e.target.value)}
                sx={{ flex: "0 0 200px" }}
              />
              <TextField
                label="City"
                value={city}
                onChange={(e) => setCity(e.target.value)}
                fullWidth
              />
            </Box>
            <TextField
              label="Country"
              value={country}
              onChange={(e) => setCountry(e.target.value)}
              fullWidth
            />
          </Stack>
        </CardContent>
      </Card>

      {/* Account Settings */}
      <Card sx={{ mb: 3 }}>
        <CardHeader
          avatar={<SettingsIcon />}
          title="Account Settings"
        />
        <Divider />
        <CardContent>
          <FormControlLabel
            control={
              <Switch
                checked={isSeller}
                onChange={(e) => setIsSeller(e.target.checked)}
                color="primary"
              />
            }
            label={
              <Box>
                <Typography variant="body1" fontWeight="bold">
                  Seller Account
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Enable to sell art and participate in auctions as a seller
                </Typography>
              </Box>
            }
          />
          {isSeller && !profile?.isSellerVerified && (
            <Alert severity="info" sx={{ mt: 2 }}>
              <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                <InfoIcon />
                <Typography variant="body2">
                  Your seller account is pending verification by our team
                </Typography>
              </Box>
            </Alert>
          )}
        </CardContent>
      </Card>

      {/* Account Information */}
      <Card sx={{ mb: 3 }}>
        <CardHeader
          avatar={<InfoIcon />}
          title="Account Information"
        />
        <Divider />
        <CardContent>
          <Stack spacing={2}>
            <Box>
              <Typography variant="caption" color="text.secondary">
                Account ID
              </Typography>
              <Typography variant="body2">{profile?.userId || "N/A"}</Typography>
            </Box>
            {profile?.createdAt && (
              <Box>
                <Typography variant="caption" color="text.secondary">
                  Member Since
                </Typography>
                <Typography variant="body2">
                  {new Date(profile.createdAt).toLocaleDateString()}
                </Typography>
              </Box>
            )}
            {profile?.updatedAt && (
              <Box>
                <Typography variant="caption" color="text.secondary">
                  Last Updated
                </Typography>
                <Typography variant="body2">
                  {new Date(profile.updatedAt).toLocaleString()}
                </Typography>
              </Box>
            )}
          </Stack>
        </CardContent>
      </Card>

      {/* Action Buttons */}
      <Box sx={{ display: "flex", justifyContent: "flex-end", gap: 2 }}>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={handleReset}
          disabled={saving}
        >
          Reset
        </Button>
        <Button
          variant="contained"
          startIcon={saving ? <CircularProgress size={20} /> : <SaveIcon />}
          onClick={handleSave}
          disabled={saving || !displayName || !fullName || !email}
        >
          {saving ? "Saving..." : "Save Changes"}
        </Button>
      </Box>

      {/* Snackbar for notifications */}
      <Snackbar
        open={snackbarOpen}
        autoHideDuration={3000}
        onClose={() => setSnackbarOpen(false)}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        <Alert severity={snackbarSeverity} onClose={() => setSnackbarOpen(false)}>
          {snackbarMessage}
        </Alert>
      </Snackbar>
    </Box>
  );
}
