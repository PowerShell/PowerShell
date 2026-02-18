#!/usr/bin/env bash
set -euo pipefail

pkill -f "python app.py --no-auth --host 0.0.0.0 --port 5000" >/dev/null 2>&1 || true
pkill -f "python app.py --no-auth --host 127.0.0.1 --port 5000" >/dev/null 2>&1 || true

echo "stopped"
exit 0
