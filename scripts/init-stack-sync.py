#!/usr/bin/env python3
"""Sync AGENT_MEMORY module markers, emit stack-selection.json, prune TEMPLATE_INDEX."""
from __future__ import annotations

import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path

MODULE_LINES = {
    "dotnet-wpf": ".NET / WPF",
    "android": "Android / F-Droid",
    "web": "Web / PWA",
    "python": "Python",
    "node": "Node API",
    "lightroom": "Lightroom Classic",
    "rust": "Rust",
    "go": "Go",
}

MODULE_PATHS = {
    "dotnet-wpf": "modules/dotnet-wpf/MODULE.md",
    "android": "examples/android",
    "web": "examples/web",
    "python": "examples/python",
    "node": "examples/node",
    "lightroom": "examples/lightroom",
    "rust": "examples/rust",
    "go": "examples/go",
}

PARALLEL_NOTES = {
    "dotnet-wpf": "Sprint 1 Parallel: WPF scope only (`QuickMediaIngest/**`, `QuickMediaIngest.Tests/**`)",
    "web": "Sprint 1 Parallel: Web PWA scope only (`examples/web/**`)",
    "python": "Sprint 1 Parallel: Python CLI scope only (`examples/python/**`)",
    "android": "Sprint 1 Parallel: Android FOSS scope only (`examples/android/**`)",
    "node": "Sprint 1 Parallel: Node API scope only (`examples/node/**`)",
    "multi": "Sprint 1 Parallel: one agent per active stack; no overlapping paths",
    "none": "Sprint 1 Parallel: scope per AGENT_MEMORY active modules",
}


def module_exists(root: Path, key: str) -> bool:
    rel = MODULE_PATHS.get(key)
    return rel is not None and (root / rel).exists()


def active_modules(stack: str, root: Path) -> list[str]:
    if stack in MODULE_LINES and stack not in ("multi", "none"):
        return [stack] if module_exists(root, stack) else []
    return [key for key in MODULE_LINES if module_exists(root, key)]


def sync_agent_memory(root: Path, stack: str) -> None:
    path = root / "AGENT_MEMORY.md"
    text = path.read_text(encoding="utf-8")
    active = set(active_modules(stack, root))
    for key, label in MODULE_LINES.items():
        mark = "x" if key in active else " "
        # Match checkbox lines with optional suffix after label
        pattern = rf"^- \[[ x]\] {re.escape(label)}.*$"
        replacement = f"- [{mark}] {label}" + (
            f" (`modules/{key}/MODULE.md`)" if key == "dotnet-wpf" and key in active
            else " — not applicable" if key not in active
            else ""
        )
        if key == "dotnet-wpf" and key in active:
            replacement = f"- [{mark}] {label} (`modules/dotnet-wpf/MODULE.md`)"
        elif key not in active:
            replacement = f"- [ ] {label} — not applicable"
        text = re.sub(pattern, replacement, text, count=1)
    path.write_text(text, encoding="utf-8")


def prune_template_index(root: Path) -> None:
    index_path = root / "TEMPLATE_INDEX.json"
    data = json.loads(index_path.read_text(encoding="utf-8"))

    def exists(rel: str) -> bool:
        return (root / rel).exists()

    entry_points = data.get("entry_points", {})
    data["entry_points"] = {k: v for k, v in entry_points.items() if exists(v)}

    data["files"] = [item for item in data.get("files", []) if exists(item["path"])]

    modules = data.get("modules", {})
    pruned_modules = {}
    for key, mod in modules.items():
        guide = mod.get("guide", "")
        example = mod.get("example", "")
        if guide and not exists(guide):
            continue
        if example and not exists(example):
            continue
        pruned_modules[key] = mod
    data["modules"] = pruned_modules

    index_path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def write_stack_selection(root: Path, stack: str, pruned: bool) -> None:
    cursor_dir = root / ".cursor"
    cursor_dir.mkdir(exist_ok=True)
    modules = active_modules(stack, root)
    payload = {
        "stack": stack,
        "pruned": pruned,
        "active_modules": modules,
        "parallel_scope_note": PARALLEL_NOTES.get(stack, PARALLEL_NOTES["none"]),
        "selected_at": datetime.now(timezone.utc).replace(microsecond=0).isoformat(),
    }
    (cursor_dir / "stack-selection.json").write_text(
        json.dumps(payload, indent=2) + "\n", encoding="utf-8"
    )


def main() -> None:
    if len(sys.argv) != 4:
        print("Usage: init-stack-sync.py <stack> <root> <pruned:true|false>", file=sys.stderr)
        sys.exit(1)
    stack, root_s, pruned_s = sys.argv[1], sys.argv[2], sys.argv[3]
    root = Path(root_s)
    pruned = pruned_s.lower() == "true"
    sync_agent_memory(root, stack)
    if pruned:
        prune_template_index(root)
    write_stack_selection(root, stack, pruned)


if __name__ == "__main__":
    main()
