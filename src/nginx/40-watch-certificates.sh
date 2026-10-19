#!/bin/sh
set -eu

# Certbot atomically replaces live symlinks on renewal. Watch their targets,
# rather than private key contents; no Docker socket or cross-container signals.
certificate_state() {
    find /etc/letsencrypt/live -type l -exec readlink '{}' \; | sort | sha256sum
}

(
    previous=$(certificate_state)
    while sleep "${CERTIFICATE_CHECK_INTERVAL:-60}"; do
        current=$(certificate_state)
        if [ "$current" != "$previous" ]; then
            if nginx -t && nginx -s reload; then
                previous=$current
            fi
        fi
    done
) &
