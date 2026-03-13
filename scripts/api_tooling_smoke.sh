#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${1:-http://localhost:5093}"

echo "== API Tooling Smoke Test =="
echo "Base URL: ${BASE_URL}"

check_get() {
  local name="$1"
  local path="$2"
  local expected="$3"
  local code
  code="$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}${path}")"
  if [[ "${code}" == "${expected}" ]]; then
    echo "✅ ${name} -> ${code}"
  else
    echo "❌ ${name} -> ${code} (expected ${expected})"
    exit 1
  fi
}

check_ready() {
  local code
  code="$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/health/ready")"
  if [[ "${code}" == "200" ]]; then
    echo "✅ Health ready -> 200 (DB reachable)"
  elif [[ "${code}" == "503" ]]; then
    echo "⚠️  Health ready -> 503 (DB unreachable in current environment)"
  else
    echo "❌ Health ready -> ${code} (expected 200 or 503)"
    exit 1
  fi
}

check_not_404_post() {
  local name="$1"
  local path="$2"
  local payload="$3"
  local code
  code="$(curl -s -o /dev/null -w "%{http_code}" -H "Content-Type: application/json" -d "${payload}" "${BASE_URL}${path}")"
  if [[ "${code}" == "404" ]]; then
    echo "❌ ${name} -> 404 (route not found)"
    exit 1
  fi
  echo "✅ ${name} -> ${code} (route found)"
}

check_get "Health root" "/health" "200"
check_get "Health live" "/health/live" "200"
check_ready
check_get "Diagnostics ping" "/api/v1/Diagnostics/ping" "200"
check_get "Diagnostics version" "/api/v1/Diagnostics/version" "200"
check_get "Diagnostics system" "/api/v1/Diagnostics/system" "200"
check_get "Diagnostics db endpoint" "/api/v1/Diagnostics/db" "200"
check_get "Swagger" "/swagger/index.html" "200"

# Naming checks for critical API routes.
check_not_404_post "User login route name" "/api/v1/User/login" "{\"Mail\":\"tooling-check\",\"Password\":\"tooling-check\"}"
check_not_404_post "Exchange route name" "/api/v1/Exchange/exchange" "[]"

old_code="$(curl -s -o /dev/null -w "%{http_code}" -H "Content-Type: application/json" -d "[]" "${BASE_URL}/api/v1/ExchangeOffice/Exchange/exchange")"
if [[ "${old_code}" == "404" ]]; then
  echo "✅ Legacy/old ExchangeOffice route -> 404 (expected)"
else
  echo "❌ Legacy/old ExchangeOffice route -> ${old_code} (expected 404)"
  exit 1
fi

echo "All tooling checks passed."
