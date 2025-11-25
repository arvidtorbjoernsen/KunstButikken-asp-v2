#!/bin/bash

# Simple script to reset all data by removing the .data folder
# This is the easiest way to get a fresh start

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DATA_DIR="$PROJECT_ROOT/.data"

echo "🗑️  Database Reset Script"
echo "========================"
echo ""
echo "This will delete the .data folder which contains:"
echo "  - All PostgreSQL databases (usersdb, artdb, auctionsdb, paymentsdb, admindb)"
echo "  - Keycloak data"
echo "  - Azurite blob storage"
echo ""
echo "⚠️  WARNING: ALL DATA WILL BE LOST!"
echo ""

read -p "Are you sure you want to continue? (type 'YES' to confirm): " confirmation

if [ "$confirmation" != "YES" ]; then
    echo "❌ Aborted."
    exit 1
fi

echo ""

if [ -d "$DATA_DIR" ]; then
    echo "🗑️  Removing .data folder..."
    
    # Try normal removal first
    if rm -rf "$DATA_DIR" 2>/dev/null; then
        echo "✅ .data folder removed successfully!"
    else
        echo "⚠️  Permission denied on some files, trying with elevated permissions..."
        echo "You may be prompted for your password."
        
        # Use sudo for stubborn files
        if sudo rm -rf "$DATA_DIR"; then
            echo "✅ .data folder removed successfully (with sudo)!"
        else
            echo "❌ Failed to remove .data folder even with sudo."
            echo "   Try manually: sudo rm -rf $DATA_DIR"
            exit 1
        fi
    fi
else
    echo "ℹ️  .data folder does not exist (already clean)"
fi

echo ""
echo "✅ Database reset complete!"
echo ""
echo "Next steps:"
echo "  1. Restart the application: dotnet run --project KunstButikken.AppHost"
echo "  2. All databases will be recreated and seeded automatically"
echo "  3. Keycloak will be initialized with fresh users"
echo ""

