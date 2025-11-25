#!/usr/bin/env bash
set -euo pipefail
# Run the KunstButikken.Tests project using the TUnit runner so tests are discovered.
# Use: ./scripts/run-tests.sh
ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"

# Ensure a clean build to make analyzer warnings (e.g. CA2007) consistently visible
echo "Cleaning solution..."
dotnet clean

dotnet test --project KunstButikken.Tests/KunstButikken.Tests.csproj -- --diagnostic
