#!/usr/bin/env bash
set -euo pipefail

# Reset local Keycloak persistent data to force realm re-import on next AppHost start.
# This removes the bind-mounted data directory used by the AppHost Keycloak container
# so Keycloak will treat the next start as a fresh boot and import realms from tools/keycloak.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
DATA_DIR="${REPO_ROOT}/.data/keycloak"

remove_dir() {
  local path="$1"
  if rm -rf "$path" 2>/dev/null; then
    return 0
  fi
  # Fix permissions and try again
  chmod -R u+rwX "$path" 2>/dev/null || true
  if rm -rf "$path" 2>/dev/null; then
    return 0
  fi
  # Fallback to sudo if available
  if command -v sudo >/dev/null 2>&1; then
    echo "[reset-keycloak] 'rm -rf' failed due to permissions. Trying with sudo..."
    if sudo rm -rf "$path"; then
      return 0
    fi
  fi
  return 1
}

if [[ -d "${DATA_DIR}" ]]; then
  echo "[reset-keycloak] Removing persistent Keycloak data at ${DATA_DIR}..."
  if remove_dir "${DATA_DIR}"; then
    echo "[reset-keycloak] Data directory removed."
  else
    echo "[reset-keycloak] ERROR: Failed to remove ${DATA_DIR}." >&2
    echo "[reset-keycloak] You may need to run: sudo rm -rf '${DATA_DIR}'" >&2
    exit 1
  fi
else
  echo "[reset-keycloak] No persistent Keycloak data directory found at ${DATA_DIR}. Nothing to remove."
fi

# Recreate empty directory to avoid permission issues on next start (container will own it)
mkdir -p "${DATA_DIR}"
# Make world-writable to avoid UID/GID mapping issues when the container writes here
chmod 777 "${DATA_DIR}" 2>/dev/null || true

echo "[reset-keycloak] Done. Now restart the AppHost to trigger realm re-import from tools/keycloak/KunstButikken-realm.json."
echo "[reset-keycloak] Example: dotnet run --project KunstButikken.AppHost/KunstButikken.AppHost.csproj"
