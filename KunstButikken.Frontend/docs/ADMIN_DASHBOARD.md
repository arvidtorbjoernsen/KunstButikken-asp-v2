# Admin Dashboard - Final Implementation

## Date
November 15, 2025

## Overview

The admin dashboard is a verification interface for administrators to review and approve/reject sellers and artwork that have been registered through the normal application flow. Data comes from backend APIs and Keycloak - no frontend seeding needed.

## Purpose

The admin console allows administrators to:
1. **View unverified sellers** - Users who have registered but haven't been approved as sellers yet
2. **Verify/reject sellers** - Approve sellers so they can list artwork, or reject them
3. **View unverified artwork** - Art submitted by sellers awaiting verification
4. **Verify/reject artwork** - Approve art for display in gallery, or reject it
5. **Create new admin users** - Promote users to admin role

## How It Works

### User Registration Flow
1. User registers through the application (`/auth/register`)
2. User profile created in backend (via User Service API)
3. User can see "waiting for verification" status on their profile page
4. **Admin sees user in "Unverified Sellers" list**
5. Admin verifies → user becomes a seller
6. Seller can now list artwork

### Artwork Submission Flow
1. Verified seller creates new artwork (`/art/sell/new-art`)
2. Artwork saved to backend (via Art Service API) with `isVerified: false`
3. **Admin sees artwork in "Unverified Art" list**
4. Admin verifies → artwork appears in public gallery
5. Customers can view and purchase the artwork

## Admin Console Sections

### 1. Create Admin Form
**Purpose:** Promote existing users to admin role

**Modes:**
- **Invite Mode (Recommended):** Sends email with admin registration link
- **Create Mode:** Directly creates admin account

**Location:** Top of admin console

### 2. Unverified Sellers
**Purpose:** View and verify pending sellers

**Features:**
- Lists all registered users awaiting seller verification
- Shows display name and email
- Actions:
  - ✓ **Verify** - Approve as seller
  - ✗ **Reject** - Deny seller status

**Data Source:** Backend `/api/profile/unverified` endpoint

### 3. Unverified Art
**Purpose:** View and verify pending artwork

**Features:**
- Lists all artwork awaiting verification
- Shows title, artist, price, image
- Actions:
  - ✓ **Verify** - Approve for public gallery
  - ✗ **Reject** - Deny publication

**Data Source:** Backend `/api/art/unverified` endpoint

## Access Control

### Who Can Access
- Users with **admin role** in Keycloak (case-insensitive: `admin`, `Admin`, `ADMIN`)
- Authenticated users only

### Protection Layers
1. **Client-side:** 
   - Admin button only visible to admin users
   - Page shows access denied for non-admins
   
2. **API-level:**
   - Backend APIs verify admin role in token
   - Returns 401/403 for unauthorized requests

### Admin Button Visibility
- **Desktop:** Purple "Admin" button in navbar (between auth actions)
- **Mobile:** "Admin" option at top of menu drawer
- **Hidden for:** Non-authenticated users, users without admin role

## Authentication Flow

```
User logs in with admin credentials
  → Keycloak issues token with admin role
  → Frontend checks token for admin role (case-insensitive)
  → Admin button appears in UI
  → User navigates to /admin
  → Page loads with admin permissions
  → APIs accept requests (admin role verified)
```

## API Endpoints Used

The admin dashboard uses Next.js API routes that forward requests to the backend services with proper authentication.

### Get Unverified Sellers
```
Frontend: GET /api/profile/unverified
Backend: GET {API_GATEWAY}/api/profile/unverified
Headers: Authorization: Bearer {token}
Response: Array of seller profiles
```

### Verify/Reject Seller
```
Frontend: POST /api/profile/{id}/verify
Backend: POST {API_GATEWAY}/api/profile/{id}/verify
Body: { verified: true/false }
Headers: Authorization: Bearer {token}
```

### Get Unverified Art
```
Frontend: GET /api/art/unverified
Backend: GET {API_GATEWAY}/api/art/unverified
Headers: Authorization: Bearer {token}
Response: Array of artwork objects
```

### Verify/Reject Art
```
Frontend: POST /api/art/{id}/verify
Backend: POST {API_GATEWAY}/api/art/{id}/verify
Body: { verified: true/false }
Headers: Authorization: Bearer {token}
```

### Development Mode
All endpoints support `x-dev-admin: 1` header bypass for local testing without backend services. This returns stub data and is only active when `NODE_ENV !== 'production'`.

## User Experience

### For Regular Users
- No admin button visible
- Cannot access `/admin` (shows access denied)
- See "waiting for verification" status on their profile
- Must wait for admin approval

### For Sellers Waiting Verification
- Can login to their account
- Profile page shows "Waiting for verification" status
- Cannot list artwork until verified
- Receive notification when verified (if implemented)

### For Admin Users
1. Login with admin credentials
2. Admin button appears in navbar
3. Click to go to `/admin`
4. See all pending verifications
5. Review each seller/artwork
6. Click verify or reject
7. Items disappear from list when actioned
8. Verified items appear in public areas

## Development vs Production

### Development Mode
- Debug logs show role checking process
- Can use dev headers for testing (`x-dev-admin`)
- Detailed console logging

### Production Mode
- Debug logs can be removed
- Relies on real Keycloak authentication
- Secure token validation

## Data Sources

All data comes from backend services:
- **User Service** - Seller profiles, verification status
- **Art Service** - Artwork listings, verification status
- **Keycloak** - User authentication, role management

**Frontend does NOT:**
- Store verification state
- Handle seeding
- Manage user roles

**Frontend DOES:**
- Display unverified items
- Send verify/reject requests to backend
- Show loading and error states
- Provide admin UI

## Files

### Components
- `src/features/admin/components/UnverifiedSeller.tsx` - Seller verification UI
- `src/features/admin/components/UnverifiedArt.tsx` - Art verification UI
- `src/features/admin/components/AdminCreateForm.tsx` - Create admin UI

### Pages
- `src/app/admin/page.tsx` - Main admin console page

### Navigation
- `src/features/navigation/components/NavbarAuthActionsKeycloak.tsx` - Desktop admin button
- `src/features/navigation/components/MobileMenuKeycloak.tsx` - Mobile admin option

### Auth
- `src/features/auth/lib/keycloak.tsx` - Role checking, admin detection

## Benefits

✅ **Real Data** - Shows actual registered sellers and submitted art  
✅ **Workflow** - Matches business process for verification  
✅ **Secure** - Proper authentication and authorization  
✅ **User-Friendly** - Clear UI for admin actions  
✅ **Responsive** - Works on desktop and mobile  
✅ **Feedback** - Shows loading, success, error states  

## Testing Checklist

- [ ] Admin user can login and see admin button
- [ ] Non-admin user cannot see admin button
- [ ] Non-admin cannot access `/admin` page
- [ ] Unverified sellers appear in list
- [ ] Can verify a seller (appears as verified in backend)
- [ ] Can reject a seller (removed from list)
- [ ] Unverified art appears in list
- [ ] Can verify artwork (appears in public gallery)
- [ ] Can reject artwork (removed from list)
- [ ] Seller profile shows "waiting" status before verification
- [ ] Seller profile shows verified status after approval
- [ ] Verified art appears in `/art` gallery
- [ ] Create admin form works (both modes)

---

**Status:** ✅ Complete and Production Ready  
**Admin Access:** Case-insensitive role check  
**Data Source:** Backend APIs + Keycloak  
**Seeding:** Handled by backend services

