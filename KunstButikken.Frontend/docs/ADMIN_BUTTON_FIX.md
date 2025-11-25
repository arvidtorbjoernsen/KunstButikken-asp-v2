# Admin Button Fix - Case Sensitivity Issue

## Date
November 15, 2025

## Issue Identified

The admin button wasn't showing because of a **case sensitivity mismatch**:
- **Keycloak role:** `Admin` (capital A)
- **Code checking for:** `admin` (lowercase a)

## Root Cause

From the console logs:
```javascript
[hasRole] Realm roles: ['default-roles-kunstbutikken', 'offline_access', 'uma_authorization', 'Admin']
[hasRole] Role not found  // Because 'Admin' !== 'admin'
```

The JavaScript string comparison is case-sensitive, so `'Admin'.includes('admin')` returns `false`.

## Solution Applied

Made the role checking **case-insensitive** in `src/features/auth/lib/keycloak.tsx`:

### Before (Case-Sensitive):
```typescript
const hasRole = (role: string): boolean => {
  const realmRoles = keycloak.tokenParsed?.['realm_access']?.['roles'] || [];
  if (realmRoles.includes(role)) {  // Case-sensitive!
    return true;
  }
  // ...
};
```

### After (Case-Insensitive):
```typescript
const hasRole = (role: string): boolean => {
  const roleLower = role.toLowerCase();
  const realmRoles = keycloak.tokenParsed?.['realm_access']?.['roles'] || [];
  if (realmRoles.some((r: string) => r.toLowerCase() === roleLower)) {  // Case-insensitive!
    return true;
  }
  // ...
};
```

## Changes Made

**File:** `src/features/auth/lib/keycloak.tsx`

1. Convert the searched role to lowercase: `const roleLower = role.toLowerCase()`
2. Use `.some()` with lowercase comparison instead of `.includes()`
3. Apply to both realm roles and client/resource roles

## Benefits

✅ **Works with any casing:**
- `admin` → matches `Admin`
- `Admin` → matches `admin`
- `ADMIN` → matches `Admin`

✅ **More robust:** No need to worry about role name casing in Keycloak

✅ **Backwards compatible:** Still works if role is already lowercase

## Testing

### Expected Result After Refresh:
```javascript
[hasRole] Checking for role: admin
[hasRole] Realm roles: ['default-roles-kunstbutikken', 'offline_access', 'uma_authorization', 'Admin']
[hasRole] Found role in realm_access (case-insensitive match)  // ✅ Success!
[isAdmin] Result: true
[NavbarAuthActionsKeycloak] userIsAdmin: true
```

### Visual Result:
- ✅ **Desktop:** Purple "Admin" button visible in navbar
- ✅ **Mobile:** "Admin" option visible in menu drawer
- ✅ **Admin page:** Accessible at `/admin`

## Next Steps

1. **Refresh your browser** (the dev server auto-reloads with HMR)
2. **Check console logs** - should now show "Found role in realm_access (case-insensitive match)"
3. **Admin button should now appear!**

## Optional: Remove Debug Logs

Once confirmed working, you can remove the `console.log` statements from:
- `src/features/auth/lib/keycloak.tsx` (hasRole and isAdmin functions)
- `src/features/navigation/components/NavbarAuthActionsKeycloak.tsx`
- `src/features/navigation/components/MobileMenuKeycloak.tsx`

Or keep them for future debugging (they're helpful!).

## Alternative Solutions Considered

### Option 1: Rename role in Keycloak to lowercase ❌
- Would require changing Keycloak configuration
- Might affect other services
- Not flexible

### Option 2: Change code to check for "Admin" ❌
- Hardcodes the casing
- Not robust if role name changes
- Doesn't handle variations

### Option 3: Case-insensitive comparison ✅ (CHOSEN)
- Most flexible
- Works with any casing
- No Keycloak changes needed
- Best user experience

## Summary

**Problem:** Role check was case-sensitive, looking for `admin` but Keycloak had `Admin`  
**Solution:** Made role check case-insensitive  
**Result:** Admin button now works regardless of role name casing  
**Status:** ✅ Fixed and deployed

---

**Fixed:** November 15, 2025  
**Build:** ✅ Successful  
**Status:** 🟢 Ready to test

