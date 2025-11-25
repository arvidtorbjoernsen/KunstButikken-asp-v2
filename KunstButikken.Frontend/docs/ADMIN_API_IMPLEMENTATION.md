# Admin API Implementation Complete

## Date
November 15, 2025

## What Was Implemented

The admin dashboard API routes have been fully implemented to forward requests to the backend services.

## Changes Made

### 1. GET /api/profile/unverified
**Status:** ✅ Implemented

- Forwards to backend User Service API
- Passes Keycloak bearer token
- Returns list of unverified sellers
- Dev bypass with `x-dev-admin: 1` header

**Backend URL:** `{API_GATEWAY}/api/profile/unverified`

### 2. POST /api/profile/{id}/verify
**Status:** ✅ Implemented

- Forwards verification request to backend
- Passes Keycloak bearer token
- Body: `{ verified: true/false }`
- Returns success/error response

**Backend URL:** `{API_GATEWAY}/api/profile/{id}/verify`

### 3. GET /api/art/unverified
**Status:** ✅ Implemented

- Forwards to backend Art Service API
- Passes Keycloak bearer token
- Returns list of unverified artwork
- Dev bypass with `x-dev-admin: 1` header

**Backend URL:** `{API_GATEWAY}/api/art/unverified`

### 4. POST /api/art/{id}/verify
**Status:** ✅ Implemented

- Forwards verification request to backend
- Passes Keycloak bearer token
- Body: `{ verified: true/false }`
- Returns success/error response

**Backend URL:** `{API_GATEWAY}/api/art/{id}/verify`

## How It Works

### Request Flow:
```
Admin Dashboard Component
  ↓
Frontend API Route (/api/profile/unverified)
  ↓
Extract Bearer Token from Request
  ↓
Forward to Backend API ({API_GATEWAY}/api/profile/unverified)
  ↓
Backend Validates Token & Returns Data
  ↓
Frontend Returns Data to Component
```

### Token Handling:
1. Admin logs in with Keycloak
2. Keycloak token stored in browser
3. Frontend components make requests with credentials
4. API routes extract Bearer token from request headers
5. Forward token to backend in Authorization header
6. Backend validates token and processes request

## Environment Variables

The API routes use these environment variables (in order of preference):

### For User/Profile Endpoints:
1. `NEXT_PUBLIC_API_GATEWAY` (preferred)
2. `NEXT_PUBLIC_API_USER` (fallback)
3. `http://localhost:5011` (default)

### For Art Endpoints:
1. `NEXT_PUBLIC_API_GATEWAY` (preferred)
2. `NEXT_PUBLIC_API_ART` (fallback)
3. `http://localhost:5012` (default)

## Development Mode

### Dev Bypass Feature:
All endpoints support development bypass when:
- `NODE_ENV !== 'production'`
- Request header `x-dev-admin: 1` is present

When active, returns stub data without calling backend:
- **Sellers:** 2 fake sellers (Dev Seller A, Dev Seller B)
- **Art:** 2 fake artworks (Dev Art A, Dev Art B)

This allows frontend development without running backend services.

## Error Handling

### Client-side (Components):
- Shows loading spinner during fetch
- Displays error alerts for failures
- Shows "No items" message when list is empty
- Snackbar notifications for verify/reject actions

### API Routes:
- 400: Bad Request (missing ID or invalid JSON)
- 401: Unauthorized (missing Bearer token)
- 500: Internal Server Error (backend communication failed)
- Logs errors to console with context

## Testing

### Production Mode (Real Backend):
1. Start backend services (User Service, Art Service, Keycloak)
2. Start frontend dev server
3. Login as admin user
4. Navigate to `/admin`
5. Should see real unverified sellers and art from backend database

### Development Mode (Stub Data):
The API routes still support dev bypass with `x-dev-admin: 1` header, but the frontend components no longer send this header automatically. To use stub data for testing without backend:

1. Temporarily add the header in the component code, OR
2. Use a tool like Postman/curl to test the API routes directly

**Current behavior:** Components always call real backend APIs.

## Files Modified

1. **`src/app/api/profile/unverified/route.ts`**
   - Added backend forwarding
   - Improved dev stub data

2. **`src/app/api/profile/[id]/verify/route.ts`**
   - Added backend forwarding
   - Better error handling

3. **`src/app/api/art/unverified/route.ts`**
   - Added backend forwarding
   - Improved dev stub data

4. **`src/app/api/art/[id]/verify/route.ts`**
   - Added backend forwarding
   - Better error handling

5. **`docs/ADMIN_DASHBOARD.md`**
   - Updated API documentation
   - Added backend URL info

## What Frontend Components Do

### UnverifiedSeller Component:
```typescript
// Fetches unverified sellers from backend
fetch('/api/profile/unverified', { 
  credentials: 'include'
})

// Verifies a seller
fetch(`/api/profile/${id}/verify`, {
  method: 'POST',
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ verified: true }),
  credentials: 'include'
})
```

### UnverifiedArt Component:
```typescript
// Fetches unverified art from backend
fetch('/api/art/unverified', { 
  credentials: 'include'
})

// Verifies artwork
fetch(`/api/art/${id}/verify`, {
  method: 'POST',
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ verified: true }),
  credentials: 'include'
})
```

**Note:** The components NO LONGER send `x-dev-admin: 1` header. They always use the real backend APIs.

## Backend Requirements

For the admin dashboard to work properly, the backend APIs must implement:

### User Service:
- `GET /api/profile/unverified` - Returns array of unverified seller profiles
- `POST /api/profile/{id}/verify` - Updates seller verification status

### Art Service:
- `GET /api/art/unverified` - Returns array of unverified artwork
- `POST /api/art/{id}/verify` - Updates artwork verification status

All endpoints must:
- Accept Bearer token in Authorization header
- Validate admin role in token
- Return appropriate HTTP status codes
- Return JSON responses

## Verification

✅ **Build:** Successful  
✅ **TypeScript:** No errors  
✅ **API Routes:** All 4 endpoints implemented  
✅ **Token Forwarding:** Bearer token passed to backend  
✅ **Error Handling:** Proper error responses  
✅ **Dev Mode:** Stub data for local testing  

## Next Steps

1. **Start your backend services** (if not already running)
2. **Restart the frontend dev server** (to pick up API changes)
3. **Login as admin** and navigate to `/admin`
4. **Check browser console** for any API errors
5. **Verify** that sellers and art appear in the lists

If the lists are empty and showing "No unverified items found":
- This means the backend has no unverified data
- Use your backend seeding to create test users/art
- Or manually register users and submit art through the UI

---

**Status:** ✅ Complete and Ready to Test  
**Build:** ✅ Successful  
**Date:** November 15, 2025

