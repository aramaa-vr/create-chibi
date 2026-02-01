#!/usr/bin/env python3
"""
VPM ZIP作成スクリプト。

Python 標準ライブラリのみで ZIP を作成します。
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import zipfile
from pathlib import Path

ZIP_NAME_PREFIX = "jp.aramaa.create-chibi"
ROOT_DIR = Path(__file__).resolve().parents[1]
SOURCE_DIR = ROOT_DIR / "Assets/Aramaa/CreateChibi"
BUILD_DIR = ROOT_DIR / "Build"
PACKAGE_JSON = ROOT_DIR / "Assets/Aramaa/CreateChibi/package.json"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="VPM ZIPを作成します。")
    parser.add_argument("version", nargs="?", help="バージョン (例: 0.3.0)")
    return parser.parse_args()


def read_version_from_package_json() -> str:
    if not PACKAGE_JSON.exists():
        raise FileNotFoundError(f"package.jsonが見つかりません: {PACKAGE_JSON}")
    content = PACKAGE_JSON.read_text(encoding="utf-8")
    try:
        data = json.loads(content)
    except json.JSONDecodeError as exc:
        raise ValueError(f"package.jsonの解析に失敗しました: {PACKAGE_JSON}") from exc
    version = data.get("version")
    if not isinstance(version, str) or not version:
        raise ValueError(f"package.jsonからversionを取得できません: {PACKAGE_JSON}")
    return version


def validate_version(version: str) -> None:
    if not version or "." not in version:
        raise ValueError(f"バージョン形式が不正です: {version}")


def add_directory_entry(zip_file: zipfile.ZipFile, relative: Path) -> None:
    """空ディレクトリもZIPに入れるためのエントリ追加。"""
    if relative == Path("."):
        return
    archive_name = relative.as_posix().rstrip("/") + "/"
    zip_info = zipfile.ZipInfo(archive_name)
    zip_info.external_attr = 0o40775 << 16
    zip_file.writestr(zip_info, "")


def create_zip(zip_path: Path, source_dir: Path) -> None:
    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as zip_file:
        for root, dirs, files in os.walk(source_dir):
            dirs.sort()
            files.sort()
            root_path = Path(root)
            relative_root = root_path.relative_to(source_dir)
            if not files and not dirs:
                add_directory_entry(zip_file, relative_root)
            for file_name in files:
                file_path = root_path / file_name
                archive_name = (relative_root / file_name).as_posix()
                zip_file.write(file_path, archive_name)


def main() -> int:
    args = parse_args()
    try:
        version = args.version or read_version_from_package_json()
        validate_version(version)
    except (FileNotFoundError, ValueError) as exc:
        print(str(exc), file=sys.stderr)
        return 1

    zip_file_name = f"{ZIP_NAME_PREFIX}-{version}.zip"
    zip_file_path = BUILD_DIR / zip_file_name

    if zip_file_path.exists():
        print(f"削除: {zip_file_path}")
        zip_file_path.unlink()

    BUILD_DIR.mkdir(parents=True, exist_ok=True)

    if not SOURCE_DIR.exists():
        print(f"ソースディレクトリが見つかりません: {SOURCE_DIR}", file=sys.stderr)
        return 1

    create_zip(zip_file_path, SOURCE_DIR)

    print(f"ZIP作成完了: {zip_file_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
