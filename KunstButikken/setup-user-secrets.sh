#!/bin/bash

# Quick setup script for user secrets
# Run this from the project root

cd "$(dirname "$0")/KunstButikken.AppHost"

echo "🔐 Setting up user secrets for KunstButikken..."
echo ""

# Initialize user secrets
dotnet user-secrets init

# Add all secrets
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
echo "   cd KunstButikken.AppHost"
echo "   dotnet run"

