#!/bin/bash

# Script to repair broken Keycloak accounts with null expires_at
# Run this after deploying the custom adapter fix

echo "🔧 Repairing Broken Keycloak Accounts"
echo "======================================"
echo ""

# Check if the app is running
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

# Call the repair API
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

