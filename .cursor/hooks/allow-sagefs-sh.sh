#!/bin/bash
# Allow the six SageFS unattended-route scripts. Anything else that matched the hook asks.
input=$(cat)

command=""
if command -v python >/dev/null 2>&1; then
    command=$(printf '%s' "$input" | python -c 'import json,sys; print(json.load(sys.stdin).get("command") or "")')
elif command -v python3 >/dev/null 2>&1; then
    command=$(printf '%s' "$input" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("command") or "")')
elif command -v node >/dev/null 2>&1; then
    command=$(printf '%s' "$input" | node -e 'let s="";process.stdin.on("data",d=>s+=d);process.stdin.on("end",()=>{try{process.stdout.write(String((JSON.parse(s).command)||""))}catch(e){process.stdout.write("")}})')
fi

if [[ -z "$command" ]]; then
    printf '%s\n' '{ "permission": "ask", "agent_message": "SageFS shell allow hook could not read the command." }'
    exit 0
fi

command="${command#"${command%%[![:space:]]*}"}"
command="${command%"${command##*[![:space:]]}"}"
command="${command#./}"
command="${command//\\//}"

hex='[0-9a-f]{8}'
workflow='interactive|repl|normal|hotreload|weblive|live|web|livetesting|live-testing|testing|test'
store='friction|cohort|cohort-ledger|all'

allow=0
[[ "$command" =~ ^scripts/sagefs-hard-reset\.sh\ ${hex}(\ (true|false))?$ ]] && allow=1
[[ "$command" =~ ^scripts/sagefs-reset\.sh\ ${hex}$ ]] && allow=1
[[ "$command" =~ ^scripts/sagefs-stop-session\.sh\ ${hex}$ ]] && allow=1
[[ "$command" =~ ^scripts/sagefs-stop-app\.sh\ ${hex}$ ]] && allow=1
[[ "$command" =~ ^scripts/sagefs-workflow\.sh\ ${hex}\ (${workflow})$ ]] && allow=1
[[ "$command" =~ ^scripts/sagefs-clear-local-data\.sh\ (${store})$ ]] && allow=1

if [[ "$allow" -eq 1 ]]; then
    printf '%s\n' '{ "permission": "allow" }'
else
    printf '%s\n' '{ "permission": "ask" }'
fi
exit 0
