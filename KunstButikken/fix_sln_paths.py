#!/usr/bin/env python3
"""
fix_sln_paths.py

Scan a .sln file for Project(...) entries where the referenced project file does not exist
and attempt to fix them by searching the repository for a matching .csproj and replacing the path
with a relative path from the .sln location.

Usage:
  python fix_sln_paths.py [--sln SOLN_PATH] [--root REPO_ROOT] [--backup] [--apply] [--yes]

- By default operates on 'KunstButikken.All.sln' in the script directory.
- Without --apply the script performs a dry-run and prints proposed changes.
- --yes auto-applies the best candidate when multiple matches are found.

Heuristics:
- Exact filename match (case-insensitive) is preferred.
- If multiple hits, prefer match where directory or file path contains the project name tokens.
- Falls back to the first sensible match if --yes passed, otherwise asks (prints choices).

Produces a backup of the original SOLN file with a '.bak' suffix when applying changes.

"""

import argparse
import os
import re
import sys
from pathlib import Path

PROJECT_RE = re.compile(
    r'^Project\("\{(?P<typeguid>[0-9A-Fa-f\-]+)\}"\)\s*=\s*"(?P<name>[^"]+)",\s*"(?P<path>[^"]+)",\s*"(?P<guid>[^"]+)"',
    re.MULTILINE,
)


def find_all_csproj(root: Path):
    """Return a list of all .csproj paths under root (absolute Path objects)."""
    results = []
    for p in root.rglob('*.csproj'):
        results.append(p.resolve())
    return results


def score_match(sln_project_path: str, candidate: Path, project_name: str):
    """Return a heuristic score for candidate: higher is better."""
    score = 0
    # exact filename match (case-insensitive)
    if candidate.name.lower() == Path(sln_project_path).name.lower():
        score += 100
    # project name occurs in parts of the path
    tokens = [t.lower() for t in re.split('[^A-Za-z0-9]+', project_name) if t]
    path_lower = '/'.join(candidate.parts).lower()
    for t in tokens:
        if t and t in path_lower:
            score += 10
    # prefer shallower paths (closer to root)
    score -= len(candidate.parts) // 3
    return score


def relative_sln_path(sln_dir: Path, target: Path):
    rel = os.path.relpath(target, start=sln_dir)
    # Use forward slashes in .sln files
    return rel.replace('\\', '/')


def main():
    ap = argparse.ArgumentParser(description='Fix .sln Project references to point to actual .csproj files in the repo')
    ap.add_argument('--sln', '-s', default='KunstButikken.All.sln', help='Solution file to fix (default: KunstButikken.All.sln)')
    ap.add_argument('--root', '-r', default='.', help='Repository root to search for projects (default: script dir)')
    ap.add_argument('--apply', action='store_true', help='Apply changes to the .sln file (default: dry-run)')
    ap.add_argument('--yes', action='store_true', help='Auto-accept best candidate when multiple found')
    ap.add_argument('--backup', action='store_true', help='Create .bak backup even in dry-run')
    args = ap.parse_args()

    sln_path = Path(args.sln).resolve()
    if not sln_path.exists():
        # try relative to script dir
        script_dir = Path(__file__).resolve().parent
        alt = script_dir / args.sln
        if alt.exists():
            sln_path = alt
        else:
            print(f'ERROR: solution file not found: {args.sln}', file=sys.stderr)
            sys.exit(2)

    repo_root = Path(args.root).resolve()
    # if user left default '.' and sln is in a subfolder, use sln parent as root candidate
    if args.root == '.' and sln_path.parent.name.lower() == 'kunstbutikken':
        repo_root = sln_path.parent.parent.resolve()

    print(f'Using solution: {sln_path}')
    print(f'Search root: {repo_root}')

    sln_text = sln_path.read_text(encoding='utf-8')

    projects = []
    for m in PROJECT_RE.finditer(sln_text):
        projects.append({
            'full_match': m.group(0),
            'typeguid': m.group('typeguid'),
            'name': m.group('name'),
            'path': m.group('path'),
            'guid': m.group('guid'),
            'span': m.span(0),
        })

    if not projects:
        print('No Project(...) entries found in the solution file. Nothing to do.')
        return

    all_csprojs = find_all_csproj(repo_root)
    print(f'Found {len(all_csprojs)} .csproj files under {repo_root}')

    changes = []

    sln_dir = sln_path.parent

    for p in projects:
        orig_path = p['path']
        orig_resolved = (sln_dir / orig_path).resolve() if not Path(orig_path).is_absolute() else Path(orig_path).resolve()
        if orig_resolved.exists():
            # exists on disk -> OK
            continue

        print('\nProject missing:')
        print(f"  Name: {p['name']}")
        print(f"  Path in sln: {orig_path}")
        # candidate search: exact filename first
        filename = Path(orig_path).name
        matches = [c for c in all_csprojs if c.name.lower() == filename.lower()]

        # if none, try to find by project name token
        if not matches:
            tokens = [t for t in re.split('[^A-Za-z0-9]+', p['name']) if t]

            def token_match(c):
                low = '/'.join(c.parts).lower()
                return any(t.lower() in low for t in tokens)

            matches = [c for c in all_csprojs if token_match(c)]

        # if still none, try fuzzy: file name contains project name without dots/spaces
        if not matches:
            key = ''.join(re.split('[^A-Za-z0-9]+', p['name'])).lower()
            matches = [c for c in all_csprojs if key and key in c.name.lower()]

        if not matches:
            print('  No candidate .csproj found under the repo root. Skipping.')
            continue

        # rank matches
        scored = [(score_match(orig_path, c, p['name']), c) for c in matches]
        scored.sort(key=lambda x: x[0], reverse=True)

        # present top candidates
        print('  Candidate matches:')
        for i, (sc, c) in enumerate(scored[:10], start=1):
            try:
                rel = relative_sln_path(sln_dir, c)
            except Exception:
                rel = str(c)
            print(f'   {i}. score={sc} -> {c} (rel: {rel})')

        best_score, best_path = scored[0]
        # if ambiguous and not yes, prompt (but user likely running non-interactive; we print instructions)
        if len(scored) > 1 and scored[0][0] == scored[1][0] and not args.yes:
            print('  Ambiguous best candidates (top scores equal). To auto-apply choose --yes or run manually.')
            # don't apply
            continue

        new_rel = relative_sln_path(sln_dir, best_path)
        print(f"  Selected: {best_path} -> will write: {new_rel}")

        changes.append((p['path'], new_rel, p['typeguid'], p['name'], p['guid']))

    if not changes:
        print('\nNo changes to apply.')
        return

    # apply or dry-run
    if args.apply:
        bak = sln_path.with_suffix(sln_path.suffix + '.bak')
        if args.backup or not bak.exists():
            print(f'Creating backup: {bak}')
            sln_path.rename(bak)
            original = bak.read_text(encoding='utf-8')
            sln_text = original
        else:
            print(f'Backup exists: {bak}, will overwrite target file directly')
            original = sln_text

        new_text = sln_text
        for old_path, new_rel, typeguid, name, guid in changes:
            # Replace only the first occurrence of the old path in a Project(...) line
            # Build a regex that matches the specific Project line and capture groups
            esc_old = re.escape(old_path)
            pattern = re.compile(r'(Project\("\{' + re.escape(typeguid) + r'\}"\)\s*=\s*"' + re.escape(name) + r'",\s*")' + esc_old + r'("\s*,\s*"' + re.escape(guid) + r'"))', re.MULTILINE)
            # If pattern matches, replace the middle path with new_rel
            if pattern.search(new_text):
                new_text = pattern.sub(r"\1" + new_rel.replace('\\', '/') + r"\2", new_text, count=1)
            else:
                # Fallback: naive replace of the quoted old_path
                new_text = new_text.replace(f'"{old_path}"', f'"{new_rel}"', 1)

        sln_path.write_text(new_text, encoding='utf-8')
        print(f'Applied {len(changes)} changes to {sln_path}')
    else:
        print('\nDry-run mode (no file changes). To apply the changes run with --apply and optionally --yes')
        for old_path, new_rel, _, _, _ in changes:
            print(f'  {old_path} -> {new_rel}')


if __name__ == '__main__':
    main()
