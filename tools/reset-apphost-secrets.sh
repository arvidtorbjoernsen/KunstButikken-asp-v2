#!/usr/bin/env bash
set -euo pipefail

PROJECT="KunstButikken.AppHost/KunstButikken.AppHost.csproj"

confirm() {
  read -r -p "This will CLEAR all AppHost user-secrets. Continue? [y/N] " resp
  case ${resp:-N} in
    [yY][eE][sS]|[yY]) return 0 ;;
    *) echo "Aborted."; exit 1 ;;
  esac
}

usage() {
  cat <<EOF
Reset (clear) dotnet user-secrets for the Aspire AppHost.

Usage:
  DOTNET_CLI_HOME=. tools/reset-apphost-secrets.sh [--yes] [--project <path-to-AppHost.csproj>]

Examples:
  DOTNET_CLI_HOME=. tools/reset-apphost-secrets.sh
  DOTNET_CLI_HOME=. tools/reset-apphost-secrets.sh --yes
  DOTNET_CLI_HOME=. tools/reset-apphost-secrets.sh --project KunstButikken.AppHost/KunstButikken.AppHost.csproj
EOF
}

YES=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --yes|-y) YES=1; shift ;;
    --project) PROJECT="$2"; shift 2 ;;
    --help|-h) usage; exit 0 ;;
    *) echo "Unknown arg: $1"; usage; exit 1 ;;
  esac
done

if [[ ${YES} -ne 1 ]]; then
  confirm
fi

# Ensure store exists
DOTNET_CLI_HOME=${DOTNET_CLI_HOME:-.} dotnet user-secrets init --project "$PROJECT" >/dev/null 2>&1 || true

# Clear all keys
DOTNET_CLI_HOME=${DOTNET_CLI_HOME:-.} dotnet user-secrets clear --project "$PROJECT"

echo "Done. Current secrets (should be empty):"
DOTNET_CLI_HOME=${DOTNET_CLI_HOME:-.} dotnet user-secrets list --project "$PROJECT" || true
