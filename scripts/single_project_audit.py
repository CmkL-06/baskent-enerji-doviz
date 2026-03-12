#!/usr/bin/env python3
"""
Single-project guard for canonical BaskentEnerji backend.
Fails fast on high-signal structural and security drifts.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CANONICAL_DIRS = [
    ROOT / "BaskentEnerji.API",
    ROOT / "BaskentEnerji.Business",
    ROOT / "BaskentEnerji.Data",
    ROOT / "BaskentEnerji.Entity",
]
CONTROLLERS_DIR = ROOT / "BaskentEnerji.API" / "Controllers"


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def main() -> int:
    errors: list[str] = []
    warnings: list[str] = []

    for d in CANONICAL_DIRS:
        if not d.exists():
            errors.append(f"Missing canonical directory: {d}")

    if not CONTROLLERS_DIR.exists():
        errors.append(f"Missing controllers directory: {CONTROLLERS_DIR}")
    else:
        for controller in sorted(CONTROLLERS_DIR.rglob("*Controller.cs")):
            text = read_text(controller)
            header = text.split("class ", 1)[0]
            class_has_authorize = "[Authorize]" in header
            mutating_count = len(re.findall(r"\[Http(Post|Put|Delete|Patch)[^\]]*\]", text))

            if mutating_count > 0 and not class_has_authorize:
                errors.append(
                    f"{controller}: mutating endpoints ({mutating_count}) without class-level [Authorize]"
                )

            if "[Authorize]\n    [Authorize]" in header or "[Authorize]\r\n    [Authorize]" in header:
                warnings.append(f"{controller}: duplicate [Authorize] markers")

            if "C:\\Users\\" in text:
                errors.append(f"{controller}: hardcoded Windows user path found")

    bot_api = ROOT / "telegram-bot" / "baskent_api.py"
    if bot_api.exists():
        bot_text = read_text(bot_api)
        if "/user/login" in bot_text:
            errors.append(f"{bot_api}: lowercase /user/login found (expected /User/login)")

    readme = ROOT / "README.md"
    if readme.exists():
        readme_text = read_text(readme)
        if "Kanonik backend" not in readme_text:
            warnings.append("README.md: canonical project section missing")

    print("=== Single Project Audit ===")
    if warnings:
        print("Warnings:")
        for warning in warnings:
            print(f" - {warning}")

    if errors:
        print("Errors:")
        for error in errors:
            print(f" - {error}")
        return 1

    print("PASS: canonical project guard checks are clean.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
