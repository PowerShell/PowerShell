#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-serve}"

cd /tmp
if [ ! -d QueenCalifia-CyberAI ]; then
  git clone https://github.com/HeruAhmose/QueenCalifia-CyberAI.git
fi
cd QueenCalifia-CyberAI

if command -v python3.11 >/dev/null 2>&1; then
  python3.11 -m venv .venv
  source .venv/bin/activate
else
  if [ ! -x /tmp/miniforge3/bin/conda ]; then
    curl -fsSL https://github.com/conda-forge/miniforge/releases/latest/download/Miniforge3-Linux-x86_64.sh -o /tmp/miniforge.sh
    bash /tmp/miniforge.sh -b -p /tmp/miniforge3
  fi
  source /tmp/miniforge3/etc/profile.d/conda.sh
  if ! conda env list | awk '{print $1}' | grep -qx qc311; then
    conda create -y -n qc311 python=3.11 pip
  fi
  conda activate qc311
fi

python -m pip install --upgrade pip
pip install -r requirements.txt

# Force local in-memory mode (no Redis dependency) and avoid debug reloader subprocesses.
unset QC_REDIS_URL
unset REDIS_URL
export QC_FORCE_REDIS_RATE_LIMIT=0
export QC_FORCE_REDIS_BUDGET=0
export QC_BUDGET_ENABLED=0
export QC_REDIS_TLS=0
export QC_NO_AUTH=1

pkill -f "python app.py" >/dev/null 2>&1 || true

if [ "$MODE" = "smoke" ]; then
  python app.py --no-auth --host 127.0.0.1 --port 5000 >/tmp/qc-dev.log 2>&1 &
  pid=$!

  ok=0
  for _ in $(seq 1 90); do
    sleep 1
    if curl -fsS http://127.0.0.1:5000/healthz >/tmp/qc-health.txt 2>/dev/null; then
      ok=1
      break
    fi
    if curl -fsS http://127.0.0.1:5000/api/health >/tmp/qc-health.txt 2>/dev/null; then
      ok=1
      break
    fi
  done

  if [ "$ok" -ne 1 ]; then
    echo "HEALTH_CHECK_FAILED"
    tail -n 120 /tmp/qc-dev.log || true
    kill "$pid" || true
    wait "$pid" || true
    exit 1
  fi

  cat /tmp/qc-health.txt
  LOCAL_BASE_URL="http://127.0.0.1:5000"
  echo "Health URLs:"
  echo "  - ${LOCAL_BASE_URL}/healthz"
  echo "  - ${LOCAL_BASE_URL}/api/health"

  if [ -n "${CODESPACE_NAME:-}" ] && [ -n "${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-}" ]; then
    CODESPACE_BASE_URL="https://${CODESPACE_NAME}-5000.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}"
    echo "Codespaces health URLs:"
    echo "  - ${CODESPACE_BASE_URL}/healthz"
    echo "  - ${CODESPACE_BASE_URL}/api/health"
  fi

  kill "$pid" || true
  wait "$pid" || true
  exit 0
fi

echo "Starting QueenCalifia in serve mode on 0.0.0.0:5000"
LOCAL_BASE_URL="http://127.0.0.1:5000"
echo "Health URLs:"
echo "  - ${LOCAL_BASE_URL}/healthz"
echo "  - ${LOCAL_BASE_URL}/api/health"

if [ -n "${CODESPACE_NAME:-}" ] && [ -n "${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN:-}" ]; then
  CODESPACE_BASE_URL="https://${CODESPACE_NAME}-5000.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}"
  echo "Codespaces health URLs:"
  echo "  - ${CODESPACE_BASE_URL}/healthz"
  echo "  - ${CODESPACE_BASE_URL}/api/health"
fi

echo "Note: root / may return 404 by design."
exec python app.py --no-auth --host 0.0.0.0 --port 5000
