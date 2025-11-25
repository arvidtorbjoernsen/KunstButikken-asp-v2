#!/usr/bin/env bash
set -euo pipefail

# Quick check that dev services are listening on the expected ports.
# Prints HTTP status codes for known health/alive endpoints.

BASES=(
  "Frontend|http://localhost:3000"
  "Keycloak|http://localhost:8080"
  "AuthGateway|http://localhost:5010/auth/alive"
  "UserService|http://localhost:5011/health"
  "ArtService|http://localhost:5012/health"
  "AuctionService|http://localhost:5013/health"
  "AdminService|http://localhost:5014/api/admin/health"
  "PaymentService API|http://localhost:5082/health"
  "PaymentService Webhook|http://localhost:5083/health"
)

curl_silent() {
  local url="$1"
  curl -s -o /dev/null -w "%{http_code}" "$url" || echo "000"
}

printf "\n=== Checking expected dev ports ===\n\n"
for entry in "${BASES[@]}"; do
  name="${entry%%|*}"
  url="${entry##*|}"
  code=$(curl_silent "$url")
  printf "%-24s %3s  %s\n" "$name" "$code" "$url"
  if [[ "$code" == "000" ]]; then
    echo "  - Not reachable. Is the AppHost running? (dotnet run --project KunstButikken.AppHost/KunstButikken.AppHost.csproj)"
  fi
done

printf "\nTip: If any port is occupied, stop the conflicting process or change the port in KunstButikken.AppHost/AppHost.cs and update Keycloak/Insomnia accordingly.\n\n"