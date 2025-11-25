#!/bin/bash
# Verification script for multilingual fields migration

set -e

echo "🔍 Verifying Multilingual Fields Migration..."
echo ""

# Change to the repo root
cd "$(dirname "$0")/.."

echo "1️⃣  Checking backend builds..."
dotnet build KunstButikken.ArtService/KunstButikken.ArtService.csproj -v q
echo "✅ Backend builds successfully"
echo ""

echo "2️⃣  Checking frontend TypeScript..."
cd KunstButikken.Frontend
pnpm exec tsc --noEmit
echo "✅ Frontend TypeScript compiles successfully"
echo ""

echo "3️⃣  Checking migration files..."
cd ..
if [ -f "KunstButikken.ArtService/Migrations/20251024225139_RemoveOldTitleDescription.cs" ]; then
    echo "✅ Migration file exists"
else
    echo "❌ Migration file not found!"
    exit 1
fi
echo ""

echo "4️⃣  Verifying Art.cs model..."
if grep -q "public string TitleEn" KunstButikken.ArtService/Models/Art.cs && \
   grep -q "public string TitleNb" KunstButikken.ArtService/Models/Art.cs && \
   ! grep -q "public string Title {" KunstButikken.ArtService/Models/Art.cs; then
    echo "✅ Art.cs uses multilingual fields only"
else
    echo "❌ Art.cs model verification failed!"
    exit 1
fi
echo ""

echo "5️⃣  Verifying frontend types..."
if grep -q "titleEn: string;" KunstButikken.Frontend/types/art.ts && \
   grep -q "titleNb: string;" KunstButikken.Frontend/types/art.ts && \
   ! grep -q "title: string;" KunstButikken.Frontend/types/art.ts; then
    echo "✅ Frontend types use multilingual fields"
else
    echo "❌ Frontend types verification failed!"
    exit 1
fi
echo ""

echo "✨ All verification checks passed!"
echo ""
echo "📋 Next steps:"
echo "   1. Apply migration: cd KunstButikken.ArtService && dotnet ef database update"
echo "   2. Reseed data: curl -X POST http://localhost:3000/api/dev/bootstrap"
echo "   3. Test the application with both English and Norwegian locales"
echo ""

