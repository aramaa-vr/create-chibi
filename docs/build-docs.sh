#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
docs_dir="$root_dir/docs"
out_dir="${1:-$root_dir/.docs-site}"

if command -v bundle >/dev/null 2>&1; then
  (
    cd "$docs_dir"
    bundle install
    bundle exec jekyll build --destination "$out_dir"
  )
  exit 0
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "bundle も docker も見つかりません。" >&2
  echo "Ruby/Bundler または Docker を用意してから再実行してください。" >&2
  exit 1
fi

docker run --rm \
  -v "$docs_dir":/srv/jekyll \
  -v "$out_dir":/srv/jekyll/_site \
  jekyll/jekyll:4 \
  jekyll build
