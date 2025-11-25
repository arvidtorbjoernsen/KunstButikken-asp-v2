# Admin 401 Unauthorized Fix

## Date
November 15, 2025

## Issue

After removing the dev headers, the admin dashboard showed "Unauthorized — please sign in as an admin" errors. The browser console showed:

```
GET http://localhost:3000/api/profile/unverified 401 (Unauthorized)
GET http://localhost:3000/api/art/unverified 401 (Unauthorized)
```

## Root Cause

The fetch requests were using `credentials: "include"` which sends cookies, but the API routes need the Keycloak **Bearer token in the Authorization header**.

### Previous Code:
```typescript
fetch('/api/profile/unverified', { 
  credentials: "include"  // ← Only sends cookies, no Authorization header
})
```

### What Was Missing:
The Keycloak token needs to be explicitly added to the Authorization header:
```typescript
Authorization: Bearer {token}
```

## Solution

Updated both admin components to extract the Keycloak token and include it in the Authorization header for all API requests.

### Files Modified:

1. **`src/features/admin/components/UnverifiedSeller.tsx`**
   - Added token extraction: `const token = keycloak?.token;`
   - Added Authorization header to fetch requests
   - Applied to both GET (list) and POST (verify) requests

2. **`src/features/admin/components/UnverifiedArt.tsx`**
   - Added token extraction: `const token = keycloak?.token;`
   - Added Authorization header to fetch requests
   - Applied to both GET (list) and POST (verify) requests

### Updated Code Pattern:

```typescript
const fetchList = async () => {
  const token = keycloak?.token;
  const headers: Record<string, string> = {};
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  const res = await fetch(`/api/profile/unverified`, { 
    credentials: "include",
    headers  // ← Now includes Authorization: Bearer {token}
  });
  // ...
};

const setVerified = async (id: string, verified: boolean) => {
  const token = keycloak?.token;
  const headers: Record<string, string> = { 
    "Content-Type": "application/json" 
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  const res = await fetch(`/api/profile/${id}/verify`, {
    method: "POST",
    headers,  // ← Now includes Authorization: Bearer {token}
    body: JSON.stringify({ verified }),
    credentials: "include",
  });
  // ...
};
```

## How It Works Now

### Complete Request Flow:

```
1. User logs in with Keycloak
   ↓
2. Keycloak token stored in keycloak.token
   ↓
3. Admin component extracts token: keycloak?.token
   ↓
4. Component adds token to request:
   headers: { Authorization: 'Bearer {token}' }
   ↓
5. Frontend API route receives request
   ↓
6. API route extracts token: getBearerToken(req)
   ↓
7. API forwards to backend with token
   ↓
8. Backend validates token and returns data
   ↓
9. Data flows back to component and displays
```

### Why This Is Needed:

- **Cookies** (`credentials: "include"`) are for session-based auth
- **Keycloak uses token-based auth** (JWT in Authorization header)
- **Backend APIs expect** `Authorization: Bearer {token}` header
- **Token contains roles** (admin) that backend validates

## Token Security

The token is:
- ✅ Obtained from authenticated Keycloak session
- ✅ Only available when user is logged in
- ✅ Automatically refreshed by Keycloak
- ✅ Contains admin role for backend validation
- ✅ Sent over HTTPS in production

## Testing

### Expected Behavior After Fix:

1. **Login as admin** user
2. **Navigate to** `/admin`
3. **Components extract token** from Keycloak
4. **Requests include** `Authorization: Bearer {token}`
5. **API routes forward** to backend with token
6. **Backend validates** admin role in token
7. **Data returns** and displays in admin dashboard

### To Verify:

**Check Browser DevTools → Network Tab:**
- Request to `/api/profile/unverified`
- Should have `Authorization: Bearer ey...` header
- Should return 200 OK (not 401)
- Response should contain real backend data

**Check Browser Console:**
- No more 401 Unauthorized errors
- No error messages about "sign in as admin"
- Components should load data successfully

## Common Issues

### If Still Getting 401:

**Check 1: Is user logged in?**
```javascript
// In browser console
window.localStorage.getItem('kc-token')  // Should show token
```

**Check 2: Does token have admin role?**
```javascript
// Check console logs for:
[hasRole] Realm roles: [..., "Admin"]
[isAdmin] Result: true
```

**Check 3: Is token being sent?**
- Open Network tab
- Check request headers
- Should see `Authorization: Bearer ey...`

**Check 4: Is backend accepting the token?**
- Check backend logs
- Should see incoming request with Bearer token
- Backend should validate and process

### If Backend Returns Different Error:

- **403 Forbidden** → Token valid but no admin role
- **500 Server Error** → Backend issue, check backend logs
- **404 Not Found** → Backend endpoint doesn't exist
- **Network Error** → Backend not running

## Verification Checklist

✅ Added token extraction from Keycloak  
✅ Added Authorization header to GET requests  
✅ Added Authorization header to POST requests  
✅ Applied to UnverifiedSeller component  
✅ Applied to UnverifiedArt component  
✅ Build successful  
✅ Ready to test with backend  

## Next Steps

1. **Restart frontend dev server** (HMR should reload)
2. **Refresh browser** (Cmd+R or F5)
3. **Go to** `/admin` page
4. **Check Network tab** - requests should have Authorization header
5. **Should see data** or proper error messages (not 401)

If backend is running and has unverified data, you should now see it!

---

**Status:** ✅ Fixed  
**Build:** ✅ Successful  
**Issue:** Authorization header now included  
**Date:** November 15, 2025

The admin dashboard now properly sends the Keycloak Bearer token in the Authorization header, allowing the backend to authenticate and authorize the requests! 🎉

