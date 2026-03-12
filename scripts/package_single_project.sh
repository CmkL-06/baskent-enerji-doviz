#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_DIR="${1:-/opt/cursor/artifacts}"
TS="$(date +%Y%m%d_%H%M%S)"
ARCHIVE="${OUT_DIR}/single_project_${TS}.tar.gz"
SHA_FILE="${OUT_DIR}/single_project_${TS}.sha256"
LIST_FILE="${OUT_DIR}/single_project_${TS}_filelist.log"
LATEST_FILE="${OUT_DIR}/single_project_latest_timestamp.txt"

mkdir -p "${OUT_DIR}"

# Canonical release scope (exclude legacy mirror + external archives)
INCLUDE_PATHS=(
  ".cursor"
  ".gitattributes"
  ".github"
  ".gitignore"
  "BaskentEnerji.API"
  "BaskentEnerji.Business"
  "BaskentEnerji.Data"
  "BaskentEnerji.Entity"
  "BaskentEnerji.Tests"
  "BaskentEnerji.sln"
  "README.md"
  "SECURITY.md"
  "dealer-panel-enhance.js"
  "deploy-api-to-canli.ps1"
  "docs"
  "i18n-fix.js"
  "index.html"
  "scripts"
  "telegram-bot"
)

cd "${ROOT_DIR}"
tar -czf "${ARCHIVE}" "${INCLUDE_PATHS[@]}"
sha256sum "${ARCHIVE}" > "${SHA_FILE}"
tar -tzf "${ARCHIVE}" > "${LIST_FILE}"
echo "${TS}" > "${LATEST_FILE}"

echo "Archive: ${ARCHIVE}"
echo "SHA256:  ${SHA_FILE}"
echo "List:    ${LIST_FILE}"
