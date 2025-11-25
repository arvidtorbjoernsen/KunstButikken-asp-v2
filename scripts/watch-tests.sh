#!/usr/bin/env bash
set -euo pipefail
# Run the KunstButikken.Tests project in watch mode using the TUnit runner flags.
# Usage: ./scripts/watch-tests.sh
ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"

# Use 'dotnet watch --project <proj> test -- --diagnostic' so arguments after the second '--' are sent to the test app (TUnit).
exec dotnet watch --project KunstButikken.Tests/KunstButikken.Tests.csproj test -- --diagnostic

