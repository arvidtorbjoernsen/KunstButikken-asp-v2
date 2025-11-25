#!/usr/bin/env python3
import re
import sys
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LOGMESSAGES = ROOT / 'KunstButikken.Common.Logging' / 'LogMessages.cs'

# Patterns to find logger calls
# 1) instance-style: logger.LogWarning(ex, "msg {Name}", arg);
INST_RE = re.compile(r"(?P<logger>\b[_a-zA-Z]\w*\b)\.Log(?P<level>Trace|Debug|Information|Warning|Error|Critical)\s*\((?P<inside>[^;\)]*)\)\s*;", re.S)
# 2) static-style: LoggerExtensions.LogWarning(logger, ex, "msg {Name}", arg);
STATIC_RE = re.compile(r"LoggerExtensions\.Log(?P<level>Trace|Debug|Information|Warning|Error|Critical)\s*\((?P<inside>[^;\)]*)\)\s*;", re.S)

# helpers
TPL_RE = re.compile(r'@?"((?:[^"\\]|\\.)*)"')
PH_RE = re.compile(r"\{([^}]+)\}")

from collections import OrderedDict
found = OrderedDict()
replacements = []

def scan_files():
    csfiles = []
    for p in ROOT.rglob('*.cs'):
        if any(part in ('bin', 'obj', '.git') for part in p.parts):
            continue
        if p.samefile(LOGMESSAGES):
            continue
        csfiles.append(p)
    return csfiles


def normalize_template(raw):
    m = TPL_RE.search(raw)
    if not m:
        return raw.strip().strip('"').strip("'")
    return m.group(1).replace('""', '"')


def make_name(level, template, idx):
    ph = PH_RE.findall(template)
    base = level + (('_' + '_'.join([p.strip().split(':')[0] for p in ph[:3]])) if ph else '_Msg')
    name = re.sub(r'[^0-9A-Za-z_]', '_', base)
    if not name[0].isalpha():
        name = 'L_' + name
    # ensure unique
    name = f"{name}_{idx}"
    return name


def parse_args(inside):
    # split top-level commas
    parts = []
    cur = ''
    depth = 0
    i = 0
    while i < len(inside):
        ch = inside[i]
        if ch == '(' or ch == '{' or ch == '[':
            depth += 1
            cur += ch
        elif ch == ')' or ch == '}' or ch == ']':
            depth -= 1
            cur += ch
        elif ch == ',' and depth == 0:
            parts.append(cur.strip())
            cur = ''
        else:
            cur += ch
        i += 1
    if cur.strip():
        parts.append(cur.strip())
    return parts


def ensure_template(parts):
    # find first string literal in parts
    for p in parts:
        if '"' in p or "@\"" in p:
            return p
    return None


def process_file(path):
    global replacements
    txt = path.read_text(encoding='utf-8')
    newtxt = txt
    changed = False
    # instance calls
    for m in INST_RE.finditer(txt):
        logger = m.group('logger')
        level = m.group('level')
        inside = m.group('inside')
        parts = parse_args(inside)
        # Determine if first arg is exception or message
        ex = None
        template = None
        other_args = []
        if parts:
            # if first part contains string literal -> no exception
            if TPL_RE.search(parts[0]):
                # no exception
                template = parts[0]
                other_args = parts[1:]
                ex_present = False
            else:
                # assume first is exception
                ex = parts[0]
                # next should be template
                if len(parts) > 1:
                    template = parts[1]
                    other_args = parts[2:]
                ex_present = True
        else:
            continue
        if template is None:
            continue
        tpl_norm = normalize_template(template)
        ph = PH_RE.findall(tpl_norm)
        arg_count = len(ph)
        key = (level, tpl_norm, arg_count)
        if key not in found:
            idx = len(found) + 1
            name = make_name(level, tpl_norm, idx)
            found[key] = name
        else:
            name = found[key]
        # build replacement call: LogMessages.Name(logger, <args>, ex_or_null);
        call_args = [logger]
        # other_args correspond to placeholders; keep them as-is
        for a in other_args:
            call_args.append(a)
        if ex_present:
            call_args.append(ex)
        else:
            call_args.append('null')
        call_repl = f"LogMessages.{name}({', '.join(call_args)});"
        newtxt = newtxt.replace(m.group(0), call_repl)
        changed = True
        replacements.append((path, m.group(0), call_repl))
    # static calls
    for m in STATIC_RE.finditer(txt):
        level = m.group('level')
        inside = m.group('inside')
        parts = parse_args(inside)
        if len(parts) < 2:
            continue
        logger = parts[0]
        # next might be exception or template
        if TPL_RE.search(parts[1]):
            ex_present = False
            template = parts[1]
            other_args = parts[2:]
            ex = None
        else:
            ex_present = True
            ex = parts[1]
            if len(parts) > 2:
                template = parts[2]
                other_args = parts[3:]
            else:
                continue
        tpl_norm = normalize_template(template)
        ph = PH_RE.findall(tpl_norm)
        arg_count = len(ph)
        key = (level, tpl_norm, arg_count)
        if key not in found:
            idx = len(found) + 1
            name = make_name(level, tpl_norm, idx)
            found[key] = name
        else:
            name = found[key]
        call_args = [logger]
        for a in other_args:
            call_args.append(a)
        if ex_present:
            call_args.append(ex)
        else:
            call_args.append('null')
        call_repl = f"LogMessages.{name}({', '.join(call_args)});"
        newtxt = newtxt.replace(m.group(0), call_repl)
        changed = True
        replacements.append((path, m.group(0), call_repl))

    if changed:
        path.write_text(newtxt, encoding='utf-8')


def update_logmessages():
    if not found:
        print('No logging patterns found to centralize.')
        return
    if not LOGMESSAGES.exists():
        print('LogMessages.cs not found at', LOGMESSAGES)
        return
    lm_txt = LOGMESSAGES.read_text(encoding='utf-8')
    insert_point = lm_txt.rfind('\n}\n')
    if insert_point == -1:
        print('Cannot find insertion point in LogMessages.cs')
        return
    defs = '\n        // Auto-generated delegates\n'
    eid_base = 3000
    for i, ((level, tpl, arg_count), name) in enumerate(found.items()):
        tpl_escaped = tpl.replace('"', '\\"')
        if arg_count == 0:
            defs += f'        public static readonly Action<ILogger, Exception?> {name} =\n'
            defs += f'            LoggerMessage.Define(LogLevel.{level}, new EventId({eid_base + i}, "{name}"), "{tpl_escaped}");\n\n'
        else:
            types = ', '.join(['string'] * arg_count)
            defs += f'        public static readonly Action<ILogger, {types}, Exception?> {name} =\n'
            defs += f'            LoggerMessage.Define<{types}>(LogLevel.{level}, new EventId({eid_base + i}, "{name}"), "{tpl_escaped}");\n\n'
    new_lm = lm_txt[:insert_point+1] + defs + lm_txt[insert_point+1:]
    LOGMESSAGES.write_text(new_lm, encoding='utf-8')
    print(f'Wrote {len(found)} delegates to {LOGMESSAGES}. Patched {len(replacements)} call sites.')


def main():
    csfiles = scan_files()
    for f in csfiles:
        process_file(f)
    update_logmessages()

if __name__ == '__main__':
    main()
