#!/usr/bin/env bash
set -euo pipefail

pkill -f "vite" >/dev/null 2>&1 || true

echo "dashboard-stopped"
exit 0
