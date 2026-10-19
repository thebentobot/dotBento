#!/bin/sh
set -eu
trap 'exit 0' TERM INT

while :; do
    # Log failures and retry on the next cycle. No renewal means a zero exit too.
    if ! certbot renew --non-interactive --webroot --webroot-path /var/www/certbot; then
        echo "Certificate renewal failed; retrying in 12 hours" >&2
    fi
    sleep 43200 &
    wait $!
done
