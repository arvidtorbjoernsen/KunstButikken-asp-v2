#!/usr/bin/env bash
set -euo pipefail

# clean-bin-obj.sh
# Recursively find and remove "bin" and "obj" directories across the repository.
# Default mode is a safe dry-run. Use --yes to perform deletion.
# Excludes common folders like .git and node_modules by default. Use --exclude to add more.

ROOT_DIR="${1:-.}"
DRY_RUN=true
FORCE=false
EXCLUDES=(.git node_modules .next .venv venv "KunstButikken.Frontend" "KunstButikken.Frontend-Ang" "backups") # default excludes

usage() {
  cat <<EOF
Usage: $0 [--yes] [--dry-run] [--exclude <path>] [--root <path>]

Options:
  --yes            Actually delete matched directories (irreversible). Without this the script runs in dry-run mode.
  --dry-run        Explicit dry-run (default). Prints directories that would be removed.
  --exclude <path> Add an exclude path (can be used multiple times). Paths are relative to root and will be pruned from search.
  --root <path>    Root directory to scan (default: repository root where script is located if no arg given, else current dir).
  -h|--help        Show this help.

Examples:
  # Show what would be deleted
  bash scripts/clean-bin-obj.sh --dry-run

  # Actually delete (use with care)
  bash scripts/clean-bin-obj.sh --yes

  # Add custom exclude
  bash scripts/clean-bin-obj.sh --exclude "KunstButikken.Frontend/.next" --yes
EOF
}

# Parse args
POSITIONAL=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --yes)
      DRY_RUN=false; FORCE=true; shift ;;
    --dry-run)
      DRY_RUN=true; shift ;;
    --exclude)
      if [[ -z "${2:-}" ]]; then echo "Missing value for --exclude"; exit 2; fi
      EXCLUDES+=("$2"); shift 2 ;;
    --root)
      if [[ -z "${2:-}" ]]; then echo "Missing value for --root"; exit 2; fi
      ROOT_DIR="$2"; shift 2 ;;
    -h|--help)
      usage; exit 0 ;;
    --)
      shift; break ;;
    -*|--*)
      echo "Unknown option $1"; usage; exit 2 ;;
    *)
      POSITIONAL+=("$1"); shift ;;
  esac
done

if [[ ${#POSITIONAL[@]} -gt 0 && -z "${ROOT_DIR:-}" ]]; then
  ROOT_DIR="${POSITIONAL[0]}"
fi

# Normalize root dir
ROOT_DIR="$(cd "$ROOT_DIR" && pwd -P)"

# Build the find command with pruning for excludes
# Example: find . ( -path ./node_modules -o -path ./.git ) -prune -o -type d ( -name bin -o -name obj ) -print0

PRUNE_EXPR=()
if [[ ${#EXCLUDES[@]} -gt 0 ]]; then
  PRUNE_EXPR+=(\( )
  for ex in "${EXCLUDES[@]}"; do
    # allow user to provide absolute or relative patterns
    # convert to path relative to ROOT_DIR
    # trim trailing slashes
    ex_trimmed="${ex%/}"
    PRUNE_EXPR+=( -path "$ROOT_DIR/$ex_trimmed" -o )
  done
  # remove final -o
  unset 'PRUNE_EXPR[-1]'
  PRUNE_EXPR+=( \) -prune -o )
fi

# Construct the full find command as an array to preserve spacing
FIND_CMD=(find "$ROOT_DIR")
FIND_CMD+=("${PRUNE_EXPR[@]}")
FIND_CMD+=( -type d \( -name bin -o -name obj \) -print0)

# Show what will be run
if [[ "$DRY_RUN" == true ]]; then
  echo "Running in dry-run mode. No directories will be deleted. Use --yes to delete."
else
  echo "Deletion mode: directories will be removed."
fi

echo "Scanning: $ROOT_DIR"
if [[ ${#EXCLUDES[@]} -gt 0 ]]; then
  echo "Excluding: ${EXCLUDES[*]}"
fi

echo "Searching for directories named 'bin' or 'obj'..."

# Execute find and process results robustly (handle whitespace/newlines)
MATCHES=()
while IFS= read -r -d '' dir; do
  MATCHES+=("$dir")
done < <("${FIND_CMD[@]}")

if [[ ${#MATCHES[@]} -eq 0 ]]; then
  echo "No 'bin' or 'obj' directories found."
  exit 0
fi

# Print matches and delete if requested
for d in "${MATCHES[@]}"; do
  # Print path relative to repo root for readability
  rel="${d#$ROOT_DIR/}"
  rel="${rel%/}"
  if [[ "$DRY_RUN" == true ]]; then
    echo "[DRY-RUN] Would remove: $rel"
  else
    echo "Removing: $rel"
    rm -rf -- "$d"
  fi
done

if [[ "$DRY_RUN" == true ]]; then
  echo "Dry-run complete. To delete the listed directories run this script with --yes."
else
  echo "Deletion complete."
fi

exit 0
