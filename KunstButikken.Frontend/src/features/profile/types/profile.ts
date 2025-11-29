export interface UserProfile {
  id?: string;
  userId?: string;
  keycloakId?: string;
  displayName: string;
  email: string;
  fullName: string;
  isSeller: boolean;
  isSellerVerified?: boolean;
  isAdmin: boolean;
  profileImageUrl?: string;
  preferencesJson?: string;
  phoneNumber?: string;
  address?: string;
  city?: string;
  postalCode?: string;
  country?: string;
  createdAt?: string;
  updatedAt?: string;
}

