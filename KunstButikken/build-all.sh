#!/usr/bin/env bash
set -euo pipefail

# build-all.sh
# Usage:
#   ./build-all.sh           -> dry-run (shows commands that would run)
#   ./build-all.sh --run     -> actually execute restore/build/test for each solution
#   ./build-all.sh --help    -> show help

RUN=false
VERBOSE=false
DRY_RUN_MSG="(dry-run)"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --run) RUN=true; shift ;;
    --verbose) VERBOSE=true; shift ;;
    --help|-h) echo "Usage: $0 [--run] [--verbose]"; exit 0;;
    *) echo "Unknown arg: $1"; echo "Usage: $0 [--run] [--verbose]"; exit 1;;
  esac
done

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT_DIR"

echo "build-all: repo root = $ROOT_DIR"

# Find solution files (prefer KunstButikken.All.sln then any .sln)
SOLNS=( $(find . -maxdepth 2 -type f -name '*.sln' | sort) )
if [ ${#SOLNS[@]} -eq 0 ]; then
  echo "No .sln files found in repo root. Searching recursively..."
  SOLNS=( $(find . -type f -name '*.sln' | sort) )
fi

if [ ${#SOLNS[@]} -eq 0 ]; then
  echo "No solution files (.sln) found. Falling back to building individual projects (.csproj)."
  PROJS=( $(find . -type f -name '*.csproj' | sort) )
  if [ ${#PROJS[@]} -eq 0 ]; then
    echo "No .csproj files found. Nothing to build.";
    exit 0;
  fi
fi

failures=()

run_cmd() {
  if [ "$RUN" = true ]; then
    if [ "$VERBOSE" = true ]; then
      echo "+ $*"
    fi
    eval "$@"
  else
    echo "$DRY_RUN_MSG $*"
  fi
}

# helper: find test projects for a solution
_find_test_projects_for_solution() {
  local sln="$1"
  # get project list from solution (lines that end with .csproj)
  local projects
  projects=( $(dotnet sln "$sln" list 2>/dev/null | sed -n '2,$p' | sed -E 's/^\s*//') )
  local tests=()
  for p in "${projects[@]}"; do
    # normalize path (relative to repo root)
    local path
    path="$(cd "$(dirname "$sln")" && realpath "$p" 2>/dev/null || echo "")"
    if [ -z "$path" ]; then
      # fallback: try joining
      path="$(realpath "$(dirname "$sln")/$p" 2>/dev/null || echo "")"
    fi
    if [ -z "$path" ] || [ ! -f "$path" ]; then
      continue
    fi
    # quick heuristics: filename contains 'Test' OR csproj contains Microsoft.NET.Test.Sdk
    name="$(basename "$path")"
    if [[ "$name" == *[Tt]est*.csproj ]] ; then
      tests+=("$path")
      continue
    fi
    if grep -q "Microsoft.NET.Test.Sdk" "$path" 2>/dev/null; then
      tests+=("$path")
      continue
    fi
  done
  # print tests (one per line)
  for t in "${tests[@]}"; do
    echo "$t"
  done
}

# Clean (optional): only when running for real
if [ "$RUN" = true ]; then
  echo "Running clean on solutions..."
  for s in "${SOLNS[@]}"; do
    echo "-> clean $s"
    run_cmd "dotnet clean \"$s\" -v minimal"
  done
fi

# Restore / Build / Test per solution (or per project if no sln)
if [ ${#SOLNS[@]} -gt 0 ]; then
  for s in "${SOLNS[@]}"; do
    echo "\n=== Solution: $s ==="
    echo "Restore:"; run_cmd "dotnet restore \"$s\""
    echo "Build:"; run_cmd "dotnet build \"$s\" -v minimal"

    echo "Test:";
    # detect test projects under the solution and run them individually
    if [ "$RUN" = true ]; then
      mapfile -t test_projects < <(_find_test_projects_for_solution "$s")
      if [ ${#test_projects[@]} -eq 0 ]; then
        echo "  No test projects detected in solution; skipping tests for $s"
      else
        for tp in "${test_projects[@]}"; do
          echo "  Running tests for $tp";
          if ! dotnet test --no-build -v minimal "$tp"; then
            echo "  Tests failed for $tp";
            failures+=("test:$tp")
          fi
        done
      fi
    else
      echo "$DRY_RUN_MSG dotnet test --no-build <each-test-project>"
    fi
  done
else
  # build individual projects
  for p in "${PROJS[@]}"; do
    echo "\n=== Project: $p ==="
    echo "Build:"; run_cmd "dotnet build \"$p\" -v minimal"
  done
fi

# Summary
if [ "$RUN" = true ]; then
  if [ ${#failures[@]} -eq 0 ]; then
    echo "\nAll builds/tests completed (no recorded failures)."
    exit 0
  else
    echo "\nCompleted with failures:"; printf "%s\n" "${failures[@]}"; exit 2
  fi
else
  echo "\nDry-run complete. Rerun with --run to actually execute the commands."
fi
