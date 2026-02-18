#!/bin/bash
# Connect to EVEMon's TCP JSON diagnostic stream (port 5555)
# The .NET app runs on Windows; from WSL use the Windows host IP.
#
# Usage:
#   ./scripts/diag-stream.sh              # all events
#   ./scripts/diag-stream.sh esi          # ESI requests only
#   ./scripts/diag-stream.sh fetch        # scheduler fetches only
#   ./scripts/diag-stream.sh warn         # warnings/errors only
#   ./scripts/diag-stream.sh evt          # EventAggregator events

PORT="${EVEMON_DIAG_PORT:-5555}"

# Resolve Windows host IP from WSL (gateway is more reliable than resolv.conf)
WINHOST=$(ip route show default 2>/dev/null | awk '{print $3}' | head -1)
if [ -z "$WINHOST" ]; then
    WINHOST=$(grep nameserver /etc/resolv.conf 2>/dev/null | head -1 | awk '{print $2}')
fi
if [ -z "$WINHOST" ]; then
    WINHOST="localhost"
fi

FILTER="${1:-all}"

echo "Connecting to EVEMon diagnostic stream at $WINHOST:$PORT ..."
echo "Filter: $FILTER (Ctrl+C to stop)"
echo "---"

case "$FILTER" in
    esi)
        nc "$WINHOST" "$PORT" | jq -r 'select(.tag == "ESI") | "\(.ts) [\(.lvl)] \(.msg)"'
        ;;
    fetch)
        nc "$WINHOST" "$PORT" | jq -r 'select(.tag == "FETCH") | "\(.ts) [\(.lvl)] \(.msg)"'
        ;;
    warn|error)
        nc "$WINHOST" "$PORT" | jq -r 'select(.lvl == "WRN" or .lvl == "ERR" or .lvl == "CRT") | "\(.ts) [\(.lvl)] \(.cat): \(.msg)"'
        ;;
    evt|event)
        nc "$WINHOST" "$PORT" | jq -r 'select(.tag == "EVT") | "\(.ts) \(.msg)"'
        ;;
    all)
        nc "$WINHOST" "$PORT" | jq -r '"\(.ts) [\(.lvl)] \(.tag) \(.cat): \(.msg)"'
        ;;
    raw)
        nc "$WINHOST" "$PORT"
        ;;
    *)
        # Custom tag filter
        nc "$WINHOST" "$PORT" | jq -r "select(.tag == \"${FILTER^^}\" or .cat | test(\"$FILTER\"; \"i\")) | \"\(.ts) [\(.lvl)] \(.msg)\""
        ;;
esac
