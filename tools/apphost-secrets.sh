#!/usr/bin/env bash
set -euo pipefail

# Manage Aspire AppHost user-secrets without requiring MSBuild to load the project.
# This script extracts <UserSecretsId> from the AppHost .csproj and executes
# `dotnet user-secrets` with --id, which bypasses project evaluation.
#
# Usage examples:
#   DOTNET_CLI_HOME=. tools/apphost-secrets.sh list
#   DOTNET_CLI_HOME=. tools/apphost-secrets.sh clear
#   DOTNET_CLI_HOME=. tools/apphost-secrets.sh set key value
#   DOTNET_CLI_HOME=. tools/apphost-secrets.sh remove key
#
# Options:
#   --project <path>   Path to KunstButikken.AppHost.csproj (default: KunstButikken.AppHost/KunstButikken.AppHost.csproj)
#   --yes              Non-interactive when clearing
#
# Notes:
# - Requires bash, grep, sed.
# - Works even if the Aspire SDK is not installed.

PROJECT_PATH="KunstButikken.AppHost/KunstButikken.AppHost.csproj"
NON_INTERACTIVE=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --project)
      PROJECT_PATH="$2"; shift 2;;
    --yes)
      NON_INTERACTIVE=true; shift;;
    list|clear|set|remove)
      CMD="$1"; shift; break;;
    *)
      echo "Unknown option or command: $1" >&2
      echo "Usage: $0 [--project <path>] [--yes] <list|clear|set|remove> [args...]" >&2
      exit 1;;
  esac
done

if [[ -z "${CMD:-}" ]]; then
  echo "Missing command. Usage: $0 [--project <path>] [--yes] <list|clear|set|remove> [args...]" >&2
  exit 1
fi

if [[ ! -f "$PROJECT_PATH" ]]; then
  echo "Project file not found: $PROJECT_PATH" >&2
  exit 1
fi

# Extract <UserSecretsId> value from the csproj
USER_SECRETS_ID=$(grep -oE '<UserSecretsId>[^<]+'</ "$PROJECT_PATH" | sed -E 's#</?UserSecretsId>##g' | tr -d '\n' | xargs)
if [[ -z "$USER_SECRETS_ID" ]]; then
  echo "Could not find <UserSecretsId> in $PROJECT_PATH" >&2
  exit 1
fi

# Ensure DOTNET_CLI_HOME is set for better reproducibility if not already
export DOTNET_CLI_HOME=${DOTNET_CLI_HOME:-.}

case "$CMD" in
  list)
    dotnet user-secrets list --id "$USER_SECRETS_ID"
    ;;
  clear)
    if ! $NON_INTERACTIVE; then
      read -r -p "This will clear ALL user-secrets for AppHost. Continue? [y/N] " ans
      if [[ ! "$ans" =~ ^[Yy]$ ]]; then
        echo "Aborted."
        exit 1
      fi
    fi
    dotnet user-secrets clear --id "$USER_SECRETS_ID"
    ;;
  set)
    KEY=${1:-}
    VAL=${2:-}
    if [[ -z "$KEY" || -z "$VAL" ]]; then
      echo "Usage: $0 set <key> <value>" >&2
      exit 1
    fi
    dotnet user-secrets set "$KEY" "$VAL" --id "$USER_SECRETS_ID"
    ;;
  remove)
    KEY=${1:-}
    if [[ -z "$KEY" ]]; then
      echo "Usage: $0 remove <key>" >&2
      exit 1
    fi
    dotnet user-secrets remove "$KEY" --id "$USER_SECRETS_ID"
    ;;
  *)
    echo "Unknown command: $CMD" >&2
    exit 1
    ;;
esac

echo "[apphost-secrets] Done ($CMD)."