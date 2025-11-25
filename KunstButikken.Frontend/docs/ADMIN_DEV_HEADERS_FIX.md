# Admin Dashboard Fix - Dev Headers Removed

## Date
November 15, 2025

## Issue

The admin dashboard was showing stub data ("Dev Seller A", "Dev Seller B") instead of real data from the backend because the frontend components were automatically sending `x-dev-admin: 1` header, which triggered the dev bypass in the API routes.

## Root Cause

Both `UnverifiedSeller.tsx` and `UnverifiedArt.tsx` had a `buildDevHeaders()` function that automatically added `x-dev-admin: 1` header when running on localhost:

```typescript
const buildDevHeaders = (): Record<string, string> => {
  const devHeaders: Record<string, string> = {};
  if (process.env.NODE_ENV !== 'production' && typeof window !== 'undefined') {
    const host = window.location.hostname;
    if (host === 'localhost' || host === '127.0.0.1') {
      devHeaders['x-dev-admin'] = '1';  // ← This triggered stub data
    }
  }
  return devHeaders;
};
```

This header caused the API routes to return stub data instead of forwarding to the backend.

## Solution

Removed the `buildDevHeaders()` function and all references to it from both components.

### Files Modified:

1. **`src/features/admin/components/UnverifiedSeller.tsx`**
   - Removed `buildDevHeaders()` function
   - Changed `fetch('/api/profile/unverified', { credentials: "include", headers: buildDevHeaders() })`
   - To: `fetch('/api/profile/unverified', { credentials: "include" })`
   - Removed from verify endpoint as well

2. **`src/features/admin/components/UnverifiedArt.tsx`**
   - Removed `buildDevHeaders()` function
   - Changed `fetch('/api/art/unverified', { credentials: "include", headers: buildDevHeaders() })`
   - To: `fetch('/api/art/unverified', { credentials: "include" })`
   - Removed from verify endpoint as well

3. **`docs/ADMIN_API_IMPLEMENTATION.md`**
   - Updated documentation to reflect the changes
   - Clarified that components no longer send dev headers

## Result

Now the components **always** call the real backend APIs:

### Request Flow (Before):
```
Component → fetch with x-dev-admin:1 header
          → API route sees dev header
          → Returns stub data (Dev Seller A, Dev Seller B)
```

### Request Flow (After):
```
Component → fetch with credentials only
          → API route extracts Bearer token
          → Forwards to backend with token
          → Backend returns real data
          → Component displays real sellers/art
```

## What You'll See Now

After restarting your dev server:

### If Backend Has Data:
- Real unverified sellers from your database
- Real unverified artwork from your database
- Actual user emails and display names
- Actual artwork titles and details

### If Backend Has No Unverified Data:
- "No unverified sellers found" message
- "No unverified art found" message
- This is expected if your backend has no pending verifications

### If Backend Is Not Running:
- Error messages: "Failed to fetch from backend"
- Network errors in console
- Empty lists with error alerts

## Testing

### To Verify It's Working:

1. **Check browser console** - should see requests to `/api/profile/unverified` and `/api/art/unverified`
2. **Check network tab** - requests should NOT have `x-dev-admin: 1` header
3. **Check API response** - should see real data from backend, not stub data
4. **Backend logs** - should see incoming requests with Bearer tokens

### Create Test Data in Backend:

If you see empty lists, you need unverified data in your backend:

1. **Register a new user** through the app (they start unverified)
2. **Create artwork** as a verified seller (it starts unverified)
3. **Use backend seeding** to populate test data
4. **Refresh admin dashboard** to see the items

## Dev Bypass Still Available

The API routes still support dev bypass for testing, but it's now **opt-in** instead of automatic:

### To Use Stub Data (Optional):
You can still test with stub data by manually adding the header:

```typescript
// Temporarily in component for testing
fetch('/api/profile/unverified', { 
  credentials: 'include',
  headers: { 'x-dev-admin': '1' }  // Add manually if needed
})
```

Or test API routes directly:
```bash
curl -H "x-dev-admin: 1" http://localhost:3000/api/profile/unverified
```

## Verification Checklist

✅ Removed `buildDevHeaders()` from UnverifiedSeller  
✅ Removed `buildDevHeaders()` from UnverifiedArt  
✅ Components no longer send dev header  
✅ Build successful  
✅ Documentation updated  
✅ Components will now use real backend data  

## Next Steps

1. **Restart your frontend dev server** (if running)
2. **Ensure backend services are running**
3. **Login as admin** and go to `/admin`
4. **Check browser console** for any errors
5. **Should now see real backend data** (or empty if no unverified items)

---

**Status:** ✅ Fixed and Ready  
**Build:** ✅ Successful  
**Date:** November 15, 2025

The admin dashboard now connects to real backend APIs and will display actual unverified sellers and artwork from your database! 🎉

