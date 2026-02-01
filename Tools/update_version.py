#!/usr/bin/env python3
"""
CreateChibi version update helper.

Usage:
  Tools/update_version.py <version>
  Tools/update_version.py <version> --dry-run

Updates:
  - Assets/Aramaa/CreateChibi/Editor/Utilities/ChibiEditorConstants.cs (ToolVersion)
  - Assets/Aramaa/CreateChibi/package.json (version, url)
  - Tools/create_vpm_zip.sh (DEFAULT_VERSION)
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CS_CONSTANTS = ROOT / "Assets/Aramaa/CreateChibi/Editor/Utilities/ChibiEditorConstants.cs"
PACKAGE_JSON = ROOT / "Assets/Aramaa/CreateChibi/package.json"
VPM_SCRIPT = ROOT / "Tools/create_vpm_zip.sh"


def ensure_file_exists(path: Path) -> None:
    if not path.exists():
        raise FileNotFoundError(f"Required file not found: {path}")


def update_csharp_constants(version: str, dry_run: bool) -> None:
    ensure_file_exists(CS_CONSTANTS)
    content = CS_CONSTANTS.read_text(encoding="utf-8")
    new_content, count = re.subn(
        r'(public const string ToolVersion = ")([^"]+)(";)',
        rf"\g<1>{version}\g<3>",
        content,
    )
    if count != 1:
        raise ValueError(f"ToolVersion not updated (matches: {count}) in {CS_CONSTANTS}")
    if not dry_run:
        CS_CONSTANTS.write_text(new_content, encoding="utf-8")


def update_package_json(version: str, dry_run: bool) -> None:
    ensure_file_exists(PACKAGE_JSON)
    data = json.loads(PACKAGE_JSON.read_text(encoding="utf-8"))
    if "version" not in data or "url" not in data:
        raise KeyError("package.json missing required keys: version and/or url")
    data["version"] = version
    data["url"] = (
        "https://github.com/aramaa-vr/create-chibi/releases/download/"
        f"{version}/jp.aramaa.create-chibi-{version}.zip"
    )
    formatted = json.dumps(data, ensure_ascii=False, indent=4) + "\n"

    if not dry_run:
        PACKAGE_JSON.write_text(formatted, encoding="utf-8")


def update_vpm_script(version: str, dry_run: bool) -> None:
    ensure_file_exists(VPM_SCRIPT)
    content = VPM_SCRIPT.read_text(encoding="utf-8")
    new_content, count = re.subn(
        r'(readonly DEFAULT_VERSION=")([^"]+)(")',
        rf"\g<1>{version}\g<3>",
        content,
    )
    if count != 1:
        raise ValueError(f"DEFAULT_VERSION not updated (matches: {count}) in {VPM_SCRIPT}")
    if not dry_run:
        VPM_SCRIPT.write_text(new_content, encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Update CreateChibi version references.")
    parser.add_argument("version", help="New version string (e.g. 0.3.0)")
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Validate and report changes without writing files.",
    )
    args = parser.parse_args()

    update_csharp_constants(args.version, args.dry_run)
    update_package_json(args.version, args.dry_run)
    update_vpm_script(args.version, args.dry_run)

    if args.dry_run:
        print(f"[dry-run] Version update validated for {args.version}")
    else:
        print(f"Version updated to {args.version}")


if __name__ == "__main__":
    main()
