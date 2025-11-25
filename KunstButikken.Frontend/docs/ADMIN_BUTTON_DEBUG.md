# Admin Button Debugging Guide

## Issue
Admin button not showing in navbar or mobile menu even when logged in with admin credentials.

## Debugging Steps Added

I've added comprehensive console logging to help identify the issue. When you load the page after logging in, check your browser's developer console (F12) for these logs:

### 1. Check Keycloak Token Parsing
Look for logs like:
```
[hasRole] Checking for role: admin
[hasRole] Token parsed: { ... }
[hasRole] Realm roles: [...]
[hasRole] Resource access: { ... }
```

### 2. Check Admin Detection
Look for:
```
[isAdmin] Result: true/false
[NavbarAuthActionsKeycloak] isAuthed: true/false
[NavbarAuthActionsKeycloak] isAdmin(): true/false
[NavbarAuthActionsKeycloak] userIsAdmin: true/false
```

### 3. Check Mobile Menu
Look for:
```
[MobileMenuKeycloak] isAuthed: true/false
[MobileMenuKeycloak] isAdmin(): true/false
[MobileMenuKeycloak] userIsAdmin: true/false
```

## Common Issues & Solutions

### Issue 1: Role Not in Token
**Symptom:** `[hasRole] Realm roles: []` or role not in the array

**Solution:** The admin role needs to be assigned in Keycloak:
1. Log into Keycloak Admin Console
2. Go to your realm (KunstButikken)
3. Go to Users → Find your admin user
4. Go to "Role Mapping" tab
5. Assign the "admin" role (either realm role or client role)

### Issue 2: Role Name Mismatch
**Symptom:** Roles shown but not matching "admin"

**Possible names to check:**
- `admin` (lowercase)
- `Admin` (capitalized)
- `ADMIN` (uppercase)
- `administrator`

**Solution:** Update the role check in code or rename the role in Keycloak to "admin"

### Issue 3: Token Not Parsed
**Symptom:** `[hasRole] No token parsed`

**Solution:** 
- Check if Keycloak initialization completed successfully
- Look for earlier logs: `[KeycloakProvider] Keycloak initialized successfully!`
- Check Keycloak configuration in environment variables

### Issue 4: Not Authenticated
**Symptom:** `[NavbarAuthActionsKeycloak] isAuthed: false`

**Solution:**
- Verify you're actually logged in (check for logout button)
- Check Keycloak initialization logs
- Try logging out and back in

## How to Check Your Token

### Option 1: Browser Console
After logging in, run in browser console:
```javascript
// Check localStorage for Keycloak token
localStorage.getItem('kc-token')
```

### Option 2: JWT Decoder
1. Copy your token from the console logs
2. Go to https://jwt.io
3. Paste the token
4. Check the payload for:
   - `realm_access.roles` array
   - `resource_access.{client-name}.roles` array
5. Verify "admin" is in one of these arrays

### Option 3: Debug Page
Navigate to `/debug/token` to see your decoded token

## Expected Token Structure

Your Keycloak token should have one of these structures:

### Realm Role (Most Common)
```json
{
  "realm_access": {
    "roles": ["admin", "user", ...]
  }
}
```

### Client/Resource Role
```json
{
  "resource_access": {
    "kunstbutikken-frontend": {
      "roles": ["admin", ...]
    }
  }
}
```

## Quick Fix: Assign Admin Role in Keycloak

1. **Access Keycloak Admin Console:**
   - URL: http://localhost:8080/admin
   - Login with your Keycloak admin credentials

2. **Navigate to Your Realm:**
   - Select "KunstButikken" realm (or your realm name)

3. **Go to Users:**
   - Click "Users" in the left menu
   - Search for your user (username: admin)
   - Click on the user

4. **Assign Admin Role:**
   - Click "Role mapping" tab
   - Click "Assign role"
   - Find "admin" role
   - Check the checkbox
   - Click "Assign"

5. **Logout and Login Again:**
   - Logout from the frontend
   - Clear browser cache/cookies (optional)
   - Login again
   - The admin button should now appear

## Verify the Fix

After assigning the role and logging back in:

1. Open browser console (F12)
2. Look for: `[hasRole] Realm roles: [..., "admin", ...]`
3. Look for: `[isAdmin] Result: true`
4. Look for: `[NavbarAuthActionsKeycloak] userIsAdmin: true`
5. Admin button should now be visible

## If Still Not Working

Check these additional items:

1. **Environment Variables:**
   - Verify `NEXT_PUBLIC_KEYCLOAK_BASE_URL`
   - Verify `NEXT_PUBLIC_KEYCLOAK_REALM`
   - Verify `NEXT_PUBLIC_KEYCLOAK_CLIENT_ID`

2. **Keycloak Client Configuration:**
   - Ensure client is set to "public" access type
   - Ensure "Standard Flow" is enabled
   - Check redirect URIs include your frontend URL

3. **Browser Issues:**
   - Try in incognito/private mode
   - Clear all cookies and cache
   - Try a different browser

4. **Code Issues:**
   - Check for JavaScript errors in console
   - Verify Keycloak provider is wrapping the app
   - Check if navbar is using `NavbarKeycloak` (not old Navbar)

## Remove Debugging Logs (After Fixed)

Once you've identified and fixed the issue, you can remove the console.log statements from:
- `src/features/auth/lib/keycloak.tsx` (hasRole and isAdmin functions)
- `src/features/navigation/components/NavbarAuthActionsKeycloak.tsx`
- `src/features/navigation/components/MobileMenuKeycloak.tsx`

Or leave them for future debugging (they won't affect production if process.env.NODE_ENV checks are added).

---

**Created:** November 15, 2025  
**Status:** Debugging Active

