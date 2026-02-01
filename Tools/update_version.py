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
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CS_CONSTANTS = ROOT / "Assets/Aramaa/CreateChibi/Editor/Utilities/ChibiEditorConstants.cs"
PACKAGE_JSON = ROOT / "Assets/Aramaa/CreateChibi/package.json"
VPM_SCRIPT = ROOT / "Tools/create_vpm_zip.sh"


def ensure_file_exists(path: Path) -> None:
    if not path.exists():
        raise FileNotFoundError(f"Required file not found: {path}")


def read_text(path: Path) -> str:
    return path.read_bytes().decode("utf-8")


def write_text(path: Path, content: str) -> None:
    normalized = content.replace("\r\n", "\n").replace("\r", "\n")
    path.write_bytes(normalized.encode("utf-8"))


def update_csharp_constants(version: str, dry_run: bool) -> None:
    ensure_file_exists(CS_CONSTANTS)
    content = read_text(CS_CONSTANTS)
    new_content, count = re.subn(
        r'(public const string ToolVersion = ")([^"]+)(";)',
        rf"\g<1>{version}\g<3>",
        content,
    )
    if count != 1:
        raise ValueError(f"ToolVersion not updated (matches: {count}) in {CS_CONSTANTS}")
    if not dry_run:
        write_text(CS_CONSTANTS, new_content)


def update_package_json(version: str, dry_run: bool) -> None:
    ensure_file_exists(PACKAGE_JSON)
    content = read_text(PACKAGE_JSON)
    version_content, version_count = re.subn(
        r'("version"\s*:\s*")([^"]+)(")',
        rf"\g<1>{version}\g<3>",
        content,
    )
    if version_count != 1:
        raise ValueError(
            f"package.json version not updated (matches: {version_count}) in {PACKAGE_JSON}"
        )
    url = (
        "https://github.com/aramaa-vr/create-chibi/releases/download/"
        f"{version}/jp.aramaa.create-chibi-{version}.zip"
    )
    new_content, url_count = re.subn(
        r'("url"\s*:\s*")([^"]+)(")',
        rf"\g<1>{url}\g<3>",
        version_content,
    )
    if url_count != 1:
        raise ValueError(
            f"package.json url not updated (matches: {url_count}) in {PACKAGE_JSON}"
        )

    if not dry_run:
        write_text(PACKAGE_JSON, new_content)


def update_vpm_script(version: str, dry_run: bool) -> None:
    ensure_file_exists(VPM_SCRIPT)
    content = read_text(VPM_SCRIPT)
    new_content, count = re.subn(
        r'(readonly DEFAULT_VERSION=")([^"]+)(")',
        rf"\g<1>{version}\g<3>",
        content,
    )
    if count != 1:
        raise ValueError(f"DEFAULT_VERSION not updated (matches: {count}) in {VPM_SCRIPT}")
    if not dry_run:
        write_text(VPM_SCRIPT, new_content)


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
