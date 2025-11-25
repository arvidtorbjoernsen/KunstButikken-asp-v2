#!/bin/bash
#
# KunstButikken Development Seeding Script
#
# This script seeds all necessary data for a clean development environment by calling
# the backend APIs. It is designed to be non-destructive and can be run multiple times.
#
# It requires the service URLs to be set as environment variables. When running
# with Aspire, these are injected automatically. If running manually, you must
# set them yourself by finding the URLs in the Aspire Dashboard.
#

# --- Helper Functions ---
print_header() {
  echo ""
  echo "================================================================================"
  echo "  $1"
  echo "================================================================================"
}

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

# --- Configuration & Pre-flight Checks ---
print_header "Configuration"

# Check for required environment variables and provide helpful errors.
if [ -z "$USER_SERVICE_URL" ]; then
  echo "❌ Error: USER_SERVICE_URL is not set."
  echo "   Please find the URL for the 'userservice' in the Aspire Dashboard and set it."
  echo "   Example: export USER_SERVICE_URL=http://localhost:54321"
  exit 1
fi

if [ -z "$ART_SERVICE_URL" ]; then
  echo "❌ Error: ART_SERVICE_URL is not set."
  echo "   Please find the URL for the 'artservice' in the Aspire Dashboard and set it."
  echo "   Example: export ART_SERVICE_URL=http://localhost:54322"
  exit 1
fi

echo "✅ UserService URL: $USER_SERVICE_URL"
echo "✅ ArtService URL:  $ART_SERVICE_URL"

# Check for jq for pretty-printing JSON.
if ! command_exists jq; then
  echo "⚠️ Warning: jq is not installed. JSON responses will not be pretty-printed."
  echo "   Install with 'brew install jq' (macOS) or 'sudo apt-get install jq' (Debian/Ubuntu)."
fi
echo ""

# --- Seeding Steps ---

# 1. Seed Keycloak Users
print_header "1. Seeding Keycloak Users and User Profiles"
KEYCLOAK_SEED_URL="$USER_SERVICE_URL/api/dev/seed-keycloak-users"
echo "📞 Calling endpoint: POST $KEYCLOAK_SEED_URL"
echo "   (This creates demo users in Keycloak and syncs them to the user database)"

# Capture the response from the API.
KEYCLOAK_RESPONSE=$(curl -s -X POST -H "Content-Type: application/json" -d '{}' "$KEYCLOAK_SEED_URL")

# Check if the request was successful and print a summary.
if [ $? -eq 0 ] && [ -n "$KEYCLOAK_RESPONSE" ]; then
  echo "✅ Request successful. Parsing summary..."
  if command_exists jq; then
    CREATED_USERS=$(echo "$KEYCLOAK_RESPONSE" | jq -r '.created')
    SKIPPED_USERS=$(echo "$KEYCLOAK_RESPONSE" | jq -r '.skipped')
    echo "   - Created: $CREATED_USERS users"
    echo "   - Skipped: $SKIPPED_USERS users (already exist)"
  else
    echo "$KEYCLOAK_RESPONSE"
  fi
else
  echo "❌ Error: Failed to connect to the UserService at $USER_SERVICE_URL."
  echo "   Please ensure the service is running and the URL is correct."
fi


# 2. Seed Art Catalog and Images
print_header "2. Seeding Art Catalog and Images"
ART_SEED_URL="$ART_SERVICE_URL/api/seed"
echo "📞 Calling endpoint: POST $ART_SEED_URL"
echo "   (This seeds the art catalog and uploads images to Azurite)"

# Capture the response from the API.
ART_RESPONSE=$(curl -s -X POST -H "Content-Type: application/json" -d '{"locale": "no"}' "$ART_SEED_URL")

# Check if the request was successful and print a summary.
if [ $? -eq 0 ] && [ -n "$ART_RESPONSE" ]; then
  echo "✅ Request successful. Parsing summary..."
  if command_exists jq; then
    CREATED_ART=$(echo "$ART_RESPONSE" | jq -r '.created')
    SKIPPED_ART=$(echo "$ART_RESPONSE" | jq -r '.skipped')
    UPLOADED_IMAGES=$(echo "$ART_RESPONSE" | jq -r '.imagesUploaded')
    echo "   - Created: $CREATED_ART art items"
    echo "   - Skipped: $SKIPPED_ART art items (already exist)"
    echo "   - Uploaded: $UPLOADED_IMAGES images to blob storage"
  else
    echo "$ART_RESPONSE"
  fi
else
  echo "❌ Error: Failed to connect to the ArtService at $ART_SERVICE_URL."
  echo "   Please ensure the service is running and the URL is correct."
fi


# 3. Final Summary
print_header "✅ Seeding Complete"
echo "The development environment has been seeded with initial data."
echo "Other services like Auctions and Payments will self-seed on their first run if needed."
echo ""
