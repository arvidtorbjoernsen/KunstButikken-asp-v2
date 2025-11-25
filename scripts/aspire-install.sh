#!/usr/bin/env bash

# Centralized aspire-install wrapper: delegates to the project's script if present
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOCAL_SCRIPT="$REPO_ROOT/KunstButikken/aspire-install.sh"

if [ -f "$LOCAL_SCRIPT" ]; then
  echo "Invoking local aspire install script: $LOCAL_SCRIPT"
  exec "$LOCAL_SCRIPT" "$@"
else
  echo "aspire-install.sh not found in KunstButikken/; please provide the script or use the centralized helper."
  exit 1
fi

