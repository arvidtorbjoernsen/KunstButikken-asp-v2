#!/bin/bash

# Script to drop all tables in all PostgreSQL databases
# WARNING: This will delete ALL data in the databases!

set -e

echo "⚠️  WARNING: This will drop ALL tables in ALL databases!"
echo "This includes: usersdb, artdb, auctionsdb, paymentsdb, admindb"
echo ""
read -p "Are you sure you want to continue? (type 'YES' to confirm): " confirmation

if [ "$confirmation" != "YES" ]; then
    echo "❌ Aborted."
    exit 1
fi

echo ""

# Get PostgreSQL connection settings
echo "📋 PostgreSQL Connection Settings"
echo "=================================="
echo ""
echo "Please provide your PostgreSQL connection details."
echo "Press Enter to use the default value shown in [brackets]."
echo ""

# Detect if running via Docker/Aspire (look for postgres container)
DETECTED_PORT=""
if command -v docker &> /dev/null; then
    DETECTED_PORT=$(docker ps --format "{{.Ports}}" | grep -o '0.0.0.0:[0-9]*->5432' | head -1 | sed 's/0.0.0.0:\([0-9]*\)->5432/\1/')
fi

# Get port
if [ -n "$DETECTED_PORT" ]; then
    echo "ℹ️  Auto-detected PostgreSQL running on port: $DETECTED_PORT"
    read -p "PostgreSQL port [$DETECTED_PORT]: " USER_PORT
    PGPORT="${USER_PORT:-$DETECTED_PORT}"
else
    read -p "PostgreSQL port [5432]: " USER_PORT
    PGPORT="${USER_PORT:-5432}"
fi

# Get host
read -p "PostgreSQL host [localhost]: " USER_HOST
PGHOST="${USER_HOST:-localhost}"

# Get username
read -p "PostgreSQL username [postgres]: " USER_USER
PGUSER="${USER_USER:-postgres}"

# Get password (hidden input)
echo ""
echo "Enter PostgreSQL password (input will be hidden)"
read -sp "PostgreSQL password [postgres]: " USER_PASSWORD
PGPASSWORD="${USER_PASSWORD:-postgres}"
echo ""
echo ""

export PGPASSWORD

# Show connection details
echo "📡 Connection Details:"
echo "  Host:     $PGHOST"
echo "  Port:     $PGPORT"
echo "  Username: $PGUSER"
echo "  Password: $([[ -n "$PGPASSWORD" ]] && echo "***" || echo "(none)")"
echo ""

# Test connection
echo "🔍 Testing PostgreSQL connection..."
if psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -c '\l' > /dev/null 2>&1; then
    echo "✅ Connection successful!"
    echo ""
else
    echo "❌ ERROR: Cannot connect to PostgreSQL!"
    echo ""
    echo "Please check:"
    echo "  - Is PostgreSQL running?"
    echo "  - Are the host, port, username, and password correct?"
    echo "  - Can you connect with: psql -h $PGHOST -p $PGPORT -U $PGUSER"
    echo ""
    exit 1
fi

echo "🗑️  Starting table drop process..."
echo ""

# List of databases to clean
DATABASES=("usersdb" "artdb" "auctionsdb" "paymentsdb" "admindb")

for DB in "${DATABASES[@]}"; do
    echo ""
    echo "📦 Processing database: $DB"
    
    # Check if database exists
    if psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -lqt | cut -d \| -f 1 | grep -qw "$DB"; then
        echo "  ✓ Database exists, dropping tables..."
        
        # Drop all tables in the database
        psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$DB" <<EOF
DO \$\$ DECLARE
    r RECORD;
BEGIN
    -- Drop all tables
    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public') LOOP
        EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
    END LOOP;
    
    -- Drop all sequences
    FOR r IN (SELECT sequence_name FROM information_schema.sequences WHERE sequence_schema = 'public') LOOP
        EXECUTE 'DROP SEQUENCE IF EXISTS ' || quote_ident(r.sequence_name) || ' CASCADE';
    END LOOP;
    
    -- Drop all views
    FOR r IN (SELECT viewname FROM pg_views WHERE schemaname = 'public') LOOP
        EXECUTE 'DROP VIEW IF EXISTS ' || quote_ident(r.viewname) || ' CASCADE';
    END LOOP;
END \$\$;
EOF
        
        echo "  ✓ All tables dropped in $DB"
    else
        echo "  ⚠️  Database $DB does not exist, skipping..."
    fi
done

echo ""
echo "✅ All tables have been dropped from all databases!"
echo ""
echo "Next steps:"
echo "  1. Restart the application: dotnet run --project KunstButikken.AppHost"
echo "  2. The migrations will run automatically and reseed the databases"
echo ""

