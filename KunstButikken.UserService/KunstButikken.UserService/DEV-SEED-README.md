Dev seeding / Keycloak setup (UserService)

This file lists the environment variables and commands you typically need to run the UserService locally and to invoke the dev Keycloak seeding endpoint (/api/dev/seed-keycloak-users).

1) Required environment variables (summary)

- KEYCLOAK_ISSUER (or NEXT_PUBLIC_KEYCLOAK_ISSUER)
  - Full issuer URL published by Keycloak (includes realm). Example:
    http://localhost:55246/realms/kunstbutikken
  - The seeder will derive the admin base (issuer without "/realms/{realm}") if KEYCLOAK_ISSUER is provided.

- KEYCLOAK_REALM
  - Realm name (e.g. "kunstbutikken"). If omitted the seeder will try to derive it from the issuer.

- KEYCLOAK_BASE (optional)
  - Used when the issuer is an Aspire endpoint reference; can override the admin base. Example: http://localhost:55246

- KEYCLOAK_ADMIN_CLIENT_ID and KEYCLOAK_ADMIN_CLIENT_SECRET (optional)
  - If present, the seeder will attempt client_credentials grant for admin operations.

- KC_BOOTSTRAP_ADMIN_USERNAME and KC_BOOTSTRAP_ADMIN_PASSWORD (optional)
  - If client creds are not used/available, the seeder will attempt a password grant using these credentials (defaults in your Aspire config are often admin/admin).

- KEYCLOAK_ADMIN_TOKEN_REALM (optional)
  - Token realm to request admin tokens from (default: master)

- ASPNETCORE_ENVIRONMENT
  - Should be Development for the dev endpoints to be enabled and match behavior used by the seeder.

- ASPNETCORE_URLS (or use environment variable injection from Aspire)
  - Where the UserService binds locally (e.g. http://127.0.0.1:6005). When running via Aspire/AppHost a dynamic port is used and appears in the Aspire dashboard.

- NEXT_PUBLIC_KEYCLOAK_BASE_URL (frontend)
  - For Next.js/Frontend: the base URL used by the frontend to reach Keycloak (must match Keycloak server authority and port). Example: http://localhost:55246

2) How the seeder chooses auth method

- If KEYCLOAK_ADMIN_CLIENT_ID and KEYCLOAK_ADMIN_CLIENT_SECRET are set, the seeder uses client_credentials (preferred).
- Otherwise it will try a password grant using KC_BOOTSTRAP_ADMIN_USERNAME/KC_BOOTSTRAP_ADMIN_PASSWORD (or defaults if set in the Aspire profile).
- Token requests default to realm "master" unless KEYCLOAK_ADMIN_TOKEN_REALM is set.

3) Quick local run + seed example

1) Run Keycloak (from Aspire) so you know the Keycloak port (e.g. 55246). Verify discovery:

```bash
curl -sS http://localhost:55246/realms/kunstbutikken/.well-known/openid-configuration | jq .issuer
```

2) Run UserService locally (example binds to port 6005):

```bash
# replace values with the ones you see in Aspire/dashboard
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://127.0.0.1:6005
export KEYCLOAK_ISSUER="http://localhost:55246/realms/kunstbutikken"
export KEYCLOAK_REALM="kunstbutikken"
# Option A (password grant):
export KC_BOOTSTRAP_ADMIN_USERNAME=admin
export KC_BOOTSTRAP_ADMIN_PASSWORD=admin
# Option B (client credentials):
# export KEYCLOAK_ADMIN_CLIENT_ID=some-client
# export KEYCLOAK_ADMIN_CLIENT_SECRET=secret

dotnet run
```

3) In a new terminal call the dev seed endpoint (it's a POST with an empty JSON body):

```bash
curl -v -X POST "http://127.0.0.1:6005/api/dev/seed-keycloak-users" \
  -H "Content-Type: application/json" -d '{}'
```

If you run the service via Aspire/AppHost the UserService will be exposed on a dynamic port (check the Aspire dashboard "Resources" for the port). Example using a port you saw earlier in the dashboard (55242):

```bash
curl -v -X POST "http://localhost:55242/api/dev/seed-keycloak-users" -H "Content-Type: application/json" -d '{}'
```

4) Helpful verification and troubleshooting commands

- Verify the admin token (password grant) directly:

```bash
curl -s -X POST "http://localhost:55246/realms/master/protocol/openid-connect/token" \
  -d "grant_type=password&client_id=admin-cli&username=admin&password=admin" | jq .
```

- With that token you can call the admin user list:

```bash
TOKEN="<access_token_from_previous_step>"
curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:55246/admin/realms/kunstbutikken/users?max=1" | jq .
```

- If the seed call times out or there's no HTTP response:
  - Check the UserService process is running and listening on the expected port:

```bash
# on macOS
lsof -nP -iTCP:6005 -sTCP:LISTEN || true
netstat -anv | grep LISTEN | grep 6005 || true
```

  - Confirm the service binds to 127.0.0.1 vs 0.0.0.0. When running under Aspire the dashboard wires port/proxying; locally prefer explicit ASPNETCORE_URLS.
  - Check the service logs; running dotnet in foreground prints logs to console.

- If Keycloak admin replies 401 to admin endpoints:
  - Ensure the token request succeeded and that you're requesting tokens from the correct realm (master by default). The seeder prints token endpoint diagnostics to the service logs.
  - If you have client credentials configured, ensure the client has proper permissions in Keycloak.

5) Frontend notes (Next.js / Angular)

- The frontend must receive the Keycloak base URL on startup via NEXT_PUBLIC_KEYCLOAK_BASE_URL (Next.js reads NEXT_PUBLIC_* at build/start time). The value should be the Keycloak authority base, e.g.:

```text
NEXT_PUBLIC_KEYCLOAK_BASE_URL=http://localhost:55246
```

- If the frontend was previously hardcoded to 8080, make sure the code reads the env var instead. You can verify the frontend dev-check endpoint:

```bash
curl -s http://localhost:3000/api/auth/dev-check | jq .
```

6) When calling via Aspire/AppHost

- Aspire may expose services on dynamic ports. Use the Aspire dashboard to find the mapped port for the UserService and call the dev endpoint using that port.
- The seeder recognizes Aspire endpoint references and can use KEYCLOAK_BASE when present.

7) If you want me to run the seed here

- I can run the UserService locally here (Option A) and call the endpoint if you want — I will need either the Keycloak port or the Aspire-provided KEYCLOAK_BASE/NEXT_PUBLIC_KEYCLOAK_BASE_URL values.

---
If you want, I can also:
- (A) attempt to start the UserService here and call the seed (I can capture logs and show the full response), or
- (C) add a small helper script in `KunstButikken.UserService` to perform the seed (curl wrapper) and show a one-line command to run it.

Tell me which of the above you want next.

