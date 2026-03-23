#!/bin/bash
set -e

cd /home/runner/workspace/frontend && npm install --prefer-offline --no-audit --no-fund 2>/dev/null || true
