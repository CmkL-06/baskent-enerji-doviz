#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_DIR="${ROOT_DIR}/output"
STAMP="$(date +%Y%m%d_%H%M%S)"
ARCHIVE="${OUT_DIR}/baskentenerji_single_project_${STAMP}.tar.gz"
SHA_FILE="${ARCHIVE}.sha256"
LIST_FILE="${ARCHIVE}.files.txt"

mkdir -p "${OUT_DIR}"

# Canonical release scope only.
INCLUDE_PATHS=(
  "BaskentEnerji.API"
  "BaskentEnerji.Business"
  "BaskentEnerji.Data"
  "BaskentEnerji.Entity"
  "BaskentEnerji.Tests"
  "BaskentEnerji.sln"
  "README.md"
  "SECURITY.md"
  "docs"
  "scripts"
  "telegram-bot"
  "dealer-panel-enhance.js"
  "i18n-fix.js"
  "index.html"
  "deploy-api-to-canli.ps1"
)

pushd "${ROOT_DIR}" >/dev/null
tar -czf "${ARCHIVE}" "${INCLUDE_PATHS[@]}"
sha256sum "${ARCHIVE}" > "${SHA_FILE}"
tar -tzf "${ARCHIVE}" > "${LIST_FILE}"
popd >/dev/null

echo "Archive: ${ARCHIVE}"
echo "SHA256 : ${SHA_FILE}"
echo "Files  : ${LIST_FILE}"
