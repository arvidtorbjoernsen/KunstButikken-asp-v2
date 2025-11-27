#!/usr/bin/env bash
# scripts/add-csproj-to-slnx.sh
# Find all .csproj files in the repo and interactively ask whether to add each one to a .slnx solution file.

set -euo pipefail
IFS=$'\n\t'

SOLUTION_FILE="KunstButikken-asp.slnx"
BACKUP_SUFFIX=".bak"

usage() {
  cat <<EOF
Usage: $0 [--sln PATH]

Options:
  --sln PATH   Path to the .slnx solution file to update (default: ${SOLUTION_FILE})
  -h, --help   Show this help

The script will:
  - locate all .csproj files under the repository (skipping common ignored dirs)
  - for each .csproj not already listed in the solution, ask whether to add it
  - create a backup of the solution file before modifying it
  - insert <Project Path="..." /> lines just before the closing </Solution> tag

EOF
}

# parse args
while [[ ${#} -gt 0 ]]; do
  case "$1" in
    --sln)
      SOLUTION_FILE="$2"; shift 2;;
    -h|--help)
      usage; exit 0;;
    *)
      echo "Unknown arg: $1"; usage; exit 2;;
  esac
done

if [ ! -f "$SOLUTION_FILE" ]; then
  echo "Solution file '$SOLUTION_FILE' not found in $(pwd)." >&2
  exit 1
fi

# create backup
BACKUP_FILE="${SOLUTION_FILE}${BACKUP_SUFFIX}"
cp -- "$SOLUTION_FILE" "$BACKUP_FILE"
echo "Created backup: $BACKUP_FILE"

# collect .csproj files, avoid backups / .git / node_modules / test-results / build artifacts
mapfile -t all_csprojs < <(find . -type f -name '*.csproj' \
  -not -path './backups/*' -not -path './.git/*' -not -path './node_modules/*' -not -path './test-results/*' -not -path './**/bin/*' -not -path './**/obj/*' \
  -print | sed 's|^\./||' | sort)

if [ ${#all_csprojs[@]} -eq 0 ]; then
  echo "No .csproj files found under $(pwd)."; exit 0
fi

# build list of candidates not already in solution
missing=()
for p in "${all_csprojs[@]}"; do
  if ! grep -Fq "Path=\"$p\"" "$SOLUTION_FILE"; then
    missing+=("$p")
  fi
done

if [ ${#missing[@]} -eq 0 ]; then
  echo "All .csproj files are already present in $SOLUTION_FILE"
  exit 0
fi

echo "Found ${#missing[@]} .csproj files not present in $SOLUTION_FILE"

chosen=()
for proj in "${missing[@]}"; do
  while true; do
    read -r -p "Add '$proj' to '$SOLUTION_FILE'? [y/N] " yn
    case "$yn" in
      [Yy]|[Yy][Ee][Ss]) chosen+=("$proj"); break;;
      [Nn]|[Nn][Oo]|"") break;;
      *) echo "Please answer y or n.";;
    esac
  done
done

if [ ${#chosen[@]} -eq 0 ]; then
  echo "No projects selected. Exiting. (backup kept at $BACKUP_FILE)"
  exit 0
fi

# build new file: write up to but not including closing tag, then append chosen entries, then closing tag
TMPFILE=$(mktemp)
awk '/<\\/Solution>/{exit} {print}' "$SOLUTION_FILE" > "$TMPFILE"

# ensure there's a newline before we append
printf "\n" >> "$TMPFILE"
for proj in "${chosen[@]}"; do
  printf '  <Project Path="%s" />\n' "$proj" >> "$TMPFILE"
done

# append closing tag
printf "\n</Solution>\n" >> "$TMPFILE"

# move into place
mv -- "$TMPFILE" "$SOLUTION_FILE"

echo "Updated $SOLUTION_FILE with ${#chosen[@]} project(s). Backup: $BACKUP_FILE"

exit 0

