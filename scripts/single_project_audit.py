#!/usr/bin/env python3
"""
Single-project guard for canonical BaskentEnerji backend.
Fails fast on high-signal structural and security drifts.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

CANONICAL_DIRS = [
    ROOT / "BaskentEnerji.API",
    ROOT / "BaskentEnerji.Business",
    ROOT / "BaskentEnerji.Data",
    ROOT / "BaskentEnerji.Entity",
]
CONTROLLERS_DIR = ROOT / "BaskentEnerji.API" / "Controllers"

FORBIDDEN_EXTERNAL_FILES = {
    "desktop_import",
    "local_import",
}

MUTATING_HTTP_ATTR_RE = re.compile(r"\[Http(Post|Put|Delete|Patch)\b", re.IGNORECASE)
CLASS_AUTHORIZE_RE = re.compile(
    r"\[Authorize(?:\([^\)]*\))?\]\s*(?:\r?\n\s*)*(?:public\s+)?class\s+\w+",
    re.IGNORECASE | re.MULTILINE,
)
DOUBLE_AUTHORIZE_RE = re.compile(
    r"\[Authorize(?:\([^\)]*\))?\]\s*\r?\n\s*\[Authorize(?:\([^\)]*\))?\]",
    re.IGNORECASE | re.MULTILINE,
)


def scan() -> tuple[list[str], list[str]]:
    errors: list[str] = []
    warnings: list[str] = []

    for directory in CANONICAL_DIRS:
        if not directory.exists():
            errors.append(f"Missing canonical directory: {directory.relative_to(ROOT)}")

    if not CONTROLLERS_DIR.exists():
        errors.append("Missing controllers directory under BaskentEnerji.API")
        return errors, warnings

    for controller in sorted(CONTROLLERS_DIR.rglob("*Controller.cs")):
        rel = controller.relative_to(ROOT)
        text = controller.read_text(encoding="utf-8", errors="ignore")

        mutating_count = len(MUTATING_HTTP_ATTR_RE.findall(text))
        class_has_authorize = CLASS_AUTHORIZE_RE.search(text) is not None
        has_duplicate_authorize = DOUBLE_AUTHORIZE_RE.search(text) is not None

        if mutating_count > 0 and not class_has_authorize:
            errors.append(
                f"{rel}: mutating endpoints ({mutating_count}) without class-level [Authorize]"
            )

        if has_duplicate_authorize:
            errors.append(f"{rel}: duplicate consecutive [Authorize] attributes found")

        if "C:\\Users\\" in text:
            errors.append(f"{rel}: hardcoded Windows user path found")

        if "TODO" in text:
            warnings.append(f"{rel}: TODO marker present")

    bot_file = ROOT / "telegram-bot" / "baskent_api.py"
    if bot_file.exists():
        bot_text = bot_file.read_text(encoding="utf-8", errors="ignore")
        if "/user/login" in bot_text:
            errors.append("telegram-bot/baskent_api.py: lowercase '/user/login' found")
    else:
        warnings.append("telegram-bot/baskent_api.py not found")

    readme = ROOT / "README.md"
    if readme.exists():
        readme_text = readme.read_text(encoding="utf-8", errors="ignore")
        if "BaskentEnerji.API" not in readme_text or "Kanonik" not in readme_text:
            errors.append("README.md: canonical project structure section missing/incomplete")
    else:
        errors.append("README.md missing")

    strategy_doc = ROOT / "docs" / "TEK_PROJE_STRATEJISI.md"
    if not strategy_doc.exists():
        errors.append("docs/TEK_PROJE_STRATEJISI.md missing")

    external_dir = ROOT / "external"
    if external_dir.exists():
        children = {p.name for p in external_dir.iterdir()}
        forbidden_hits = children.intersection(FORBIDDEN_EXTERNAL_FILES)
        if forbidden_hits:
            errors.append(
                "external/: forbidden project-external content still present: "
                + ", ".join(sorted(forbidden_hits))
            )

    return errors, warnings


def main() -> int:
    errors, warnings = scan()

    if errors:
        print("❌ single_project_audit: FAILED")
        for item in errors:
            print(f"  - {item}")
    else:
        print("✅ single_project_audit: PASSED")

    if warnings:
        print("⚠️ warnings:")
        for item in warnings:
            print(f"  - {item}")

    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
