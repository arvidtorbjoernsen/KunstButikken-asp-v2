#!/usr/bin/env bash

# Centralized repair-accounts script for the frontend
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
FRONTEND_DIR="$REPO_ROOT/KunstButikken.Frontend"

if [ ! -d "$FRONTEND_DIR" ]; then
  echo "Frontend directory not found: $FRONTEND_DIR"
  exit 1
fi

cd "$FRONTEND_DIR"

# Run the original script logic (assumes frontend runs on port 3000 locally)

echo "🔧 Repairing Broken Keycloak Accounts"
echo "======================================"
echo ""

if ! curl -s http://localhost:3000 > /dev/null 2>&1; then
  echo "⚠️  Warning: Frontend doesn't seem to be running on http://localhost:3000"
  echo "   Make sure the app is started before running this repair."
  echo ""
  read -p "Continue anyway? (y/N) " -n 1 -r
  echo
  if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    exit 1
  fi
fi

echo "📡 Calling repair endpoint..."

echo ""
RESPONSE=$(curl -s -X POST http://localhost:3000/api/repair-accounts)

echo "Response:"
echo "$RESPONSE" | jq '.' 2>/dev/null || echo "$RESPONSE"

echo ""
echo "✅ Done!"
echo ""
echo "Next steps:"
echo "1. Sign out from the application"
echo "2. Clear browser cookies (optional but recommended)"
echo "3. Sign in again"
echo "4. Your account will be created fresh with proper tokens"

echo ""

