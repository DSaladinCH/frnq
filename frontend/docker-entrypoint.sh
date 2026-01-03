#!/bin/sh
set -e

echo "Generating runtime configuration..."

# Validate required environment variables
REQUIRED_VARS="API_BASE_URL"

for var in $REQUIRED_VARS; do
  eval value=\$$var
  if [ -z "$value" ]; then
    echo "ERROR: Required environment variable $var is not set"
    exit 1
  fi
  echo "$var is set"
done

TEMPLATE="/app/build/client/config.template.js"
OUTPUT="/app/build/client/config.js"

if [ ! -f "$TEMPLATE" ]; then
  echo "ERROR: $TEMPLATE not found"
  echo "Available files in /app/build/client:"
  ls -la /app/build/client/ || true
  exit 1
fi

# Generate config.js - envsubst automatically substitutes all ${VAR} placeholders
envsubst < "$TEMPLATE" > "$OUTPUT"

# Remove any pre-compressed versions so they don't serve stale content
rm -f "$OUTPUT.br" "$OUTPUT.gz"

echo "Runtime configuration written to $OUTPUT"
echo "Generated config content:"
cat "$OUTPUT" || true

# All environment variables are automatically passed to the exec'd process
echo ""
echo "Starting application with environment variables available to Node.js"

exec "$@"
