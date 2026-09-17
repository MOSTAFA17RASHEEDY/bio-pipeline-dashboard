#!/usr/bin/env bash
# Make dockerd start automatically on WSL instance boot. This distro has no
# init system (no /etc/init.d/docker, no systemd), so dockerd was started by
# hand every time in Phases 1-3 -- see README "Windows/WSL note". Takes
# effect on the next full instance start (after `wsl --shutdown`), not
# immediately.
set -euo pipefail

cat > /etc/wsl.conf <<'EOF'
[boot]
command = "mkdir -p /var/log && dockerd > /var/log/dockerd.log 2>&1 &"
EOF

cat /etc/wsl.conf
