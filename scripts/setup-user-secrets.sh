#!/bin/bash

# Centralized setup-user-secrets.sh
# Usage: run from anywhere; the script locates the repository root and operates on the AppHost project.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APPHOST_DIR="$REPO_ROOT/KunstButikken/KunstButikken.AppHost"

if [ ! -d "$APPHOST_DIR" ]; then
  echo "Error: AppHost directory not found: $APPHOST_DIR"
  exit 1
fi

cd "$APPHOST_DIR"

echo "🔐 Setting up user secrets for KunstButikken..."

echo "Initializing user secrets (dotnet user-secrets init)..."
dotnet user-secrets init

# Add all secrets (tweak values as needed for local development)
echo "Adding Keycloak admin credentials..."
dotnet user-secrets set "KEYCLOAK_ADMIN_USER" "admin"
dotnet user-secrets set "KEYCLOAK_ADMIN_PASSWORD" "admin"

echo "Adding Keycloak client configuration..."
dotnet user-secrets set "KEYCLOAK_CLIENT_ID" "kunstbutikken-frontend"
dotnet user-secrets set "KEYCLOAK_CLIENT_SECRET" "kunstbutikken-dev-secret-change-in-production"

echo "Adding NextAuth secret..."
dotnet user-secrets set "NEXTAUTH_SECRET" "dev-nextauth-secret-change-this-in-production-use-openssl-rand"

echo ""
echo "✅ User secrets configured successfully!"
echo ""
echo "📋 Configured secrets:"
dotnet user-secrets list

echo ""
echo "🚀 You can now start the application:"
echo "   cd KunstButikken.KunstButikken.AppHost"
echo "   dotnet run"

