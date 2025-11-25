#!/bin/bash
# Verification script for multilingual fields migration

set -e

# Compute repository root (two levels up from scripts directory)
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "🔍 Verifying Multilingual Fields Migration..."
echo ""

# 1. Checking backend builds using repo-root paths
echo "1️⃣  Checking backend builds..."
DOTNET_PROJECT="$REPO_ROOT/KunstButikken.ArtService/KunstButikken.ArtService.csproj"
if [ ! -f "$DOTNET_PROJECT" ]; then
    echo "❌ Project not found: $DOTNET_PROJECT"
    exit 1
fi

dotnet build "$DOTNET_PROJECT" -v q
echo "✅ Backend builds successfully"
echo ""

# 2. Checking frontend TypeScript
echo "2️⃣  Checking frontend TypeScript..."
FRONTEND_DIR="$REPO_ROOT/KunstButikken.Frontend"
if [ ! -d "$FRONTEND_DIR" ]; then
    echo "❌ Frontend directory not found: $FRONTEND_DIR"
    exit 1
fi
cd "$FRONTEND_DIR"
pnpm exec tsc --noEmit
echo "✅ Frontend TypeScript compiles successfully"
echo ""

# 3. Checking migration files
cd "$REPO_ROOT"
MIGRATION_FILE="$REPO_ROOT/KunstButikken.ArtService/Migrations/20251024225139_RemoveOldTitleDescription.cs"
if [ -f "$MIGRATION_FILE" ]; then
    echo "✅ Migration file exists"
else
    echo "❌ Migration file not found: $MIGRATION_FILE"
    exit 1
fi
echo ""

# 4. Verifying Art.cs model
ART_MODEL="$REPO_ROOT/KunstButikken.ArtService/Models/Art.cs"
if grep -q "public string TitleEn" "$ART_MODEL" && \
   grep -q "public string TitleNb" "$ART_MODEL" && \
   ! grep -q "public string Title \{" "$ART_MODEL"; then
    echo "✅ Art.cs uses multilingual fields only"
else
    echo "❌ Art.cs model verification failed! ($ART_MODEL)"
    exit 1
fi
echo ""

# 5. Verifying frontend types
FRONT_ART_TYPE="$REPO_ROOT/KunstButikken.Frontend/types/art.ts"
if grep -q "titleEn: string;" "$FRONT_ART_TYPE" && \
   grep -q "titleNb: string;" "$FRONT_ART_TYPE" && \
   ! grep -q "title: string;" "$FRONT_ART_TYPE"; then
    echo "✅ Frontend types use multilingual fields"
else
    echo "❌ Frontend types verification failed! ($FRONT_ART_TYPE)"
    exit 1
fi
echo ""

echo "✨ All verification checks passed!"
echo ""
echo "📋 Next steps:"
echo "   1. Apply migration: cd $REPO_ROOT/KunstButikken.ArtService && dotnet ef database update"
echo "   2. Reseed data: curl -X POST http://localhost:3000/api/dev/bootstrap"
echo "   3. Test the application with both English and Norwegian locales"
echo ""
