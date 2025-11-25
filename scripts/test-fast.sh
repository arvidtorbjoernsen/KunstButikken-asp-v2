#!/usr/bin/env bash
set -euo pipefail

# Fast test loop for local development:
# - Builds once in Release for optimized execution
# - Runs tests without rebuilding
# - Integration tests are skipped by default; set RUN_INTEGRATION=1 to run them

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

echo "Cleaning solution..."
dotnet clean

echo "Building solution (Release)..."
dotnet build -c Release

if [ "${RUN_INTEGRATION:-0}" = "1" ]; then
  echo "RUN_INTEGRATION=1 -> Running all tests (including integration)."
  dotnet test -c Release
else
  echo "Running fast tests (integration skipped). Set RUN_INTEGRATION=1 to include integration tests."
  dotnet test --no-build -c Release --verbosity minimal
fi
