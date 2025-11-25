#!/bin/bash

# Advanced reset script that stops the app first and handles Docker permissions
# This is the most reliable way to reset when files are locked

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
DATA_DIR="$PROJECT_ROOT/.data"

echo "🔄 Advanced Database Reset Script"
echo "================================="
echo ""
echo "This script will:"
echo "  1. Stop any running Aspire/Docker containers"
echo "  2. Remove the .data folder (all databases, Keycloak, Azurite)"
echo "  3. Clean up any locked files"
echo ""
echo "⚠️  WARNING: ALL DATA WILL BE LOST!"
echo ""

read -p "Are you sure you want to continue? (type 'YES' to confirm): " confirmation

if [ "$confirmation" != "YES" ]; then
    echo "❌ Aborted."
    exit 1
fi

echo ""
echo "📍 Step 1: Stopping any running containers..."

# Try to stop Docker containers that might be locking files
if command -v docker &> /dev/null; then
    # Find and stop containers related to this project
    echo "  Looking for running containers..."
    
    # Stop Keycloak container
    if docker ps --format '{{.Names}}' | grep -q keycloak; then
        echo "  Stopping Keycloak container..."
        docker stop $(docker ps --format '{{.Names}}' | grep keycloak) 2>/dev/null || true
    fi
    
    # Stop Postgres containers
    if docker ps --format '{{.Names}}' | grep -q postgres; then
        echo "  Stopping PostgreSQL containers..."
        docker stop $(docker ps --format '{{.Names}}' | grep postgres) 2>/dev/null || true
    fi
    
    # Stop Azurite containers
    if docker ps --format '{{.Names}}' | grep -q azurite; then
        echo "  Stopping Azurite containers..."
        docker stop $(docker ps --format '{{.Names}}' | grep azurite) 2>/dev/null || true
    fi
    
    # Give containers a moment to stop
    sleep 2
    echo "  ✓ Containers stopped"
else
    echo "  ⚠️  Docker not found, skipping container stop"
fi

echo ""
echo "📍 Step 2: Removing .data folder..."

if [ -d "$DATA_DIR" ]; then
    # First, try to make everything writable
    echo "  Setting permissions..."
    chmod -R u+w "$DATA_DIR" 2>/dev/null || true
    
    # Try normal removal
    echo "  Attempting removal..."
    if rm -rf "$DATA_DIR" 2>/dev/null; then
        echo "  ✅ Successfully removed .data folder!"
    else
        echo "  ⚠️  Permission issues detected, using sudo..."
        echo "  You may be prompted for your password."
        
        if sudo rm -rf "$DATA_DIR"; then
            echo "  ✅ Successfully removed .data folder with elevated permissions!"
        else
            echo ""
            echo "❌ ERROR: Could not remove .data folder"
            echo ""
            echo "Manual cleanup required:"
            echo "  sudo rm -rf $DATA_DIR"
            echo ""
            echo "If that doesn't work, try:"
            echo "  1. Stop the Aspire app (Ctrl+C)"
            echo "  2. docker stop \$(docker ps -aq)"
            echo "  3. sudo rm -rf $DATA_DIR"
            exit 1
        fi
    fi
else
    echo "  ℹ️  .data folder does not exist (already clean)"
fi

echo ""
echo "📍 Step 3: Cleaning up Docker resources (optional)..."
if command -v docker &> /dev/null; then
    read -p "Do you want to remove stopped containers? (y/N): " cleanup
    if [[ "$cleanup" =~ ^[Yy]$ ]]; then
        echo "  Removing stopped containers..."
        docker container prune -f 2>/dev/null || true
        echo "  ✓ Cleanup complete"
    else
        echo "  Skipped Docker cleanup"
    fi
fi

echo ""
echo "✅ Database reset complete!"
echo ""
echo "Next steps:"
echo "  1. Restart the application:"
echo "     cd $PROJECT_ROOT"
echo "     dotnet run --project KunstButikken.AppHost"
echo ""
echo "  2. Wait 15-20 seconds for seeding to complete"
echo ""
echo "  3. Check logs for:"
echo "     [KeycloakSync] Created profile for seller1..."
echo "     [ArtSeeding] Successfully fetched 3 sellers..."
echo ""
echo "  4. Test at http://localhost:3000"
echo ""

