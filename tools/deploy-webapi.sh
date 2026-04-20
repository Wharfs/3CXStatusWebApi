#!/usr/bin/env bash
#
# deploy-webapi.sh - install or upgrade 3CXStatusWebApi on a 3CX Debian server.
#
# Detects whether this is a fresh install or an upgrade over an existing one,
# and does the right thing for each:
#
#   Fresh install:
#     - Creates /opt/3CXWebApi
#     - Extracts the tarball
#     - Links 3CXPhoneSystem.ini from the PBX's usual location
#     - Installs the systemd unit at /etc/systemd/system/3CXWebApi.service
#     - Enables it so it starts on boot
#     - Starts it
#     - Smoke-tests with curl
#
#   Upgrade (existing install detected via systemd unit file):
#     - Stops the running service
#     - Backs up the current install dir
#     - Preserves 3CXPhoneSystem.ini
#     - Replaces the install contents with the new tarball
#     - Restores the ini
#     - Restarts the service
#     - Smoke-tests with curl
#
# Run on the 3CX Debian server itself, as root.
#
# Usage:
#   sudo ./deploy-webapi.sh [path-to-webapi.tgz]
#
# If no tarball path is given, defaults to ./webapi.tgz next to this script.

set -euo pipefail

INSTALL_DIR="/opt/3CXWebApi"
SERVICE_NAME="3CXWebApi.service"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}"
INI_PATH="${INSTALL_DIR}/3CXPhoneSystem.ini"

# ---------- Guards ----------

if [[ $EUID -ne 0 ]]; then
  echo "This script needs root. Try: sudo $0 $*" >&2
  exit 1
fi

if ! command -v systemctl >/dev/null; then
  echo "systemctl not found. This script assumes a systemd-based Linux (3CX v20 Debian)." >&2
  exit 1
fi

TARBALL="${1:-$(dirname "$0")/webapi.tgz}"
if [[ ! -f "$TARBALL" ]]; then
  echo "Tarball not found at $TARBALL." >&2
  echo "Pass the path as the first argument, or place webapi.tgz next to this script." >&2
  exit 1
fi

# ---------- Detect fresh vs upgrade ----------

UPGRADE=false
if [[ -f "$SERVICE_FILE" ]] && [[ -d "$INSTALL_DIR" ]] && [[ -x "$INSTALL_DIR/WebAPICore" ]]; then
  UPGRADE=true
fi

if $UPGRADE; then
  echo "==> Upgrade detected (existing systemd unit + install dir + WebAPICore binary)"
else
  echo "==> Fresh install (no existing systemd unit or install)"
fi

# ---------- Upgrade: stop + back up + preserve ini ----------

INI_BACKUP=""
if $UPGRADE; then
  echo "==> Stopping $SERVICE_NAME"
  systemctl stop "$SERVICE_NAME" || true

  BACKUP_DIR="${INSTALL_DIR}.backup-$(date +%Y%m%d-%H%M%S)"
  echo "==> Backing up $INSTALL_DIR to $BACKUP_DIR"
  cp -r "$INSTALL_DIR" "$BACKUP_DIR"

  if [[ -e "$INI_PATH" ]]; then
    INI_BACKUP="/tmp/3cxini.backup.$$"
    # Preserve whether it was a symlink or a real file.
    if [[ -L "$INI_PATH" ]]; then
      cp --no-dereference "$INI_PATH" "$INI_BACKUP"
    else
      cp "$INI_PATH" "$INI_BACKUP"
    fi
    echo "==> Preserved $INI_PATH to $INI_BACKUP"
  fi

  echo "==> Clearing $INSTALL_DIR"
  find "$INSTALL_DIR" -mindepth 1 -delete
else
  echo "==> Creating $INSTALL_DIR"
  mkdir -p "$INSTALL_DIR"
fi

# ---------- Extract the tarball ----------

echo "==> Extracting $TARBALL into $INSTALL_DIR"
tar xzf "$TARBALL" -C "$INSTALL_DIR"
chmod +x "$INSTALL_DIR/WebAPICore"

# ---------- Ini handling ----------

if [[ -n "$INI_BACKUP" ]]; then
  mv "$INI_BACKUP" "$INI_PATH"
  echo "==> Restored $INI_PATH"
elif [[ ! -e "$INI_PATH" ]]; then
  # Fresh install: try to locate the PBX's own ini and symlink it.
  # Confirmed layout on a v20 Debian install in the field: the live ini
  # lives at /var/lib/3cxpbx/Bin/, despite the DLL being under
  # /var/lib/3cxpbx/Instance1/Bin/. Don't use the skel template at
  # /usr/share/3cxpbx/skel/ - that's a stock copy, not the tenant's
  # live config.
  for candidate in \
    /var/lib/3cxpbx/Bin/3CXPhoneSystem.ini \
    /var/lib/3cxpbx/Instance1/Bin/3CXPhoneSystem.ini \
    /var/lib/3cxpbx/Instance1/3CXPhoneSystem.ini \
    /etc/3cxpbx/3CXPhoneSystem.ini
  do
    if [[ -f "$candidate" ]]; then
      ln -sf "$candidate" "$INI_PATH"
      echo "==> Linked $INI_PATH -> $candidate"
      break
    fi
  done
  if [[ ! -e "$INI_PATH" ]]; then
    echo "==> WARNING: 3CXPhoneSystem.ini not found automatically." >&2
    echo "    Locate it manually and symlink to $INI_PATH before starting the service." >&2
    echo "    Try: find /var/lib /etc /opt -name 3CXPhoneSystem.ini 2>/dev/null" >&2
  fi
fi

# ---------- Install systemd unit (fresh installs only) ----------

if ! $UPGRADE; then
  echo "==> Writing $SERVICE_FILE"
  cat > "$SERVICE_FILE" <<EOF
# /etc/systemd/system/3CXWebApi.service
# Comments can only go at the beginning of the line!
[Unit]
Description=Start the 3CXWebApi daemon.
After=3CXSystemService01.service

[Service]
Type=simple
ExecStart=${INSTALL_DIR}/WebAPICore
WorkingDirectory=${INSTALL_DIR}
Restart=always

[Install]
WantedBy=multi-user.target
EOF

  systemctl daemon-reload
  systemctl enable "$SERVICE_NAME"
  echo "==> Enabled $SERVICE_NAME (starts on boot)"
fi

# ---------- Start and smoke-test ----------

echo "==> Starting $SERVICE_NAME"
systemctl start "$SERVICE_NAME"
sleep 2
systemctl status "$SERVICE_NAME" --no-pager --lines=5 || true

echo ""
echo "==> Smoke test"
if command -v curl >/dev/null; then
  if curl -sf -o /tmp/webapi-smoke.$$ -w "HTTP %{http_code}\n" http://localhost:8889/status/extension/100; then
    echo "Response body:"
    cat /tmp/webapi-smoke.$$
    echo ""
    rm -f /tmp/webapi-smoke.$$
  else
    echo "Smoke test FAILED. Check: journalctl -u $SERVICE_NAME -n 50" >&2
    rm -f /tmp/webapi-smoke.$$
    exit 2
  fi
else
  echo "curl not installed; skipping smoke test."
fi

echo ""
if $UPGRADE; then
  echo "Upgrade complete. Previous install backed up at $BACKUP_DIR."
  echo "Roll back with:"
  echo "  systemctl stop $SERVICE_NAME && rm -rf $INSTALL_DIR && mv $BACKUP_DIR $INSTALL_DIR && systemctl start $SERVICE_NAME"
else
  echo "Fresh install complete."
fi
