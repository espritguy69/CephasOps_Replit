#!/usr/bin/env bash
# Phase 11: Parser governance script (drift report + profile-pack regression gate).
# Requires: DefaultConnection or ConnectionStrings__DefaultConnection in environment (DB connection string).
# Do NOT print the connection string. Exit 0 = PASS, 1 = FAIL.

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
API_PROJECT="$BACKEND_DIR/src/CephasOps.Api/CephasOps.Api.csproj"
OUT_DIR="${OUT_DIR:-.}"
DRIFT_DAYS="${DRIFT_DAYS:-7}"

# .NET reads ConnectionStrings__DefaultConnection
if [ -z "${ConnectionStrings__DefaultConnection:-}" ] && [ -n "${DefaultConnection:-}" ]; then
  export ConnectionStrings__DefaultConnection="$DefaultConnection"
fi
if [ -z "${ConnectionStrings__DefaultConnection:-}" ]; then
  echo "Missing required environment variable. Set DefaultConnection or ConnectionStrings__DefaultConnection (do not log the value)." >&2
  exit 1
fi

DRIFT_OUT="$OUT_DIR/drift-weekly.md"
mkdir -p "$OUT_DIR"

echo "Build (Release)..."
dotnet build "$API_PROJECT" -c Release --verbosity quiet
if [ $? -ne 0 ]; then echo "Build failed." >&2; exit 1; fi

echo "=== Parser Governance ==="
echo "Step 1: Drift report (last $DRIFT_DAYS days)..."
if ! dotnet run --project "$API_PROJECT" --no-build -c Release -- drift-report --days "$DRIFT_DAYS" --format markdown --out "$DRIFT_OUT" 2>&1; then
  echo "Step 1 (drift-report) failed (informational; continuing)."
fi

echo ""
echo "Step 2: Replay all profile packs (regression gate)..."
if ! dotnet run --project "$API_PROJECT" --no-build -c Release -- replay-all-profile-packs --ci-mode 2>&1; then
  EXIT2=$?
  echo ""
  echo "=== Result ==="
  echo "FAIL: One or more profile packs reported regressions (exit code $EXIT2)."
  exit 1
fi

echo ""
echo "=== Result ==="
echo "PASS: No regressions detected."
exit 0
