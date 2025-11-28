#!/usr/bin/env bash
set -euo pipefail

# Fail if any csproj contains inline PackageReference Version="..."
echo "Checking for inline PackageReference Version attributes in .csproj files..."
if grep -RIn "<PackageReference [^>]*Version=\"" --exclude-dir:.git --exclude-dir:bin --exclude-dir:obj . ; then
  echo "ERROR: Found one or more inline PackageReference Version attributes. Use Directory.Packages.props to manage versions centrally." >&2
  exit 1
fi

echo "No inline PackageReference versions found."

