#!/usr/bin/env bash
set -euo pipefail

export PLAYWRIGHT_BASE_URL="${PLAYWRIGHT_BASE_URL:-http://localhost:5000}"

cd "$(dirname "$0")/.."

PROJECT="${1:-guest}"
GREP="${2:-}"

echo "Running E2E: project=$PROJECT base=$PLAYWRIGHT_BASE_URL"
if [ -n "$GREP" ]; then
  npx playwright test --project="$PROJECT" --grep "$GREP" --reporter=list
else
  npx playwright test --project="$PROJECT" --reporter=list
fi
