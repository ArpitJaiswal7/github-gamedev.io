#!/usr/bin/env bash
# Provision .argos_venv + Argos Translate + offline language packs (macOS).
# Run from anywhere:  bash Tools/ArgosBridge/setup_argos.sh
set -euo pipefail
cd "$(dirname "$0")/../.."

if ! command -v python3 >/dev/null 2>&1 || ! python3 -c 'import sys; exit(0 if sys.version_info >= (3, 10) else 1)'; then
    echo "Installing Python 3.11..."
    if command -v brew >/dev/null 2>&1; then
        brew install python@3.11
    else
        curl -L "https://www.python.org/ftp/python/3.11.9/python-3.11.9-macos11.pkg" -o /tmp/python311.pkg
        sudo installer -pkg /tmp/python311.pkg -target /
    fi
fi

python3 -m venv .argos_venv
./.argos_venv/bin/python3 -m pip install --upgrade pip setuptools wheel
./.argos_venv/bin/python3 -m pip install argostranslate
./.argos_venv/bin/python3 Tools/ArgosBridge/precache_models.py
