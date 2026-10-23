#!/bin/bash
# One test command for agents. See .agents/skills/tdd/SKILL.md
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

usage() {
    echo "Usage: scripts/agent-test.sh [--full | --filter <expr> [server|shared]]" >&2
    exit 1
}

run_filtered() {
    local project="$1"
    local filter="$2"
    echo "==> dotnet test $project --filter $filter"
    dotnet test "$project" --filter "$filter"
}

full_suite() {
    set -euo pipefail
    export AMBIT_FULL_SUITE=1
    echo "AMBIT_FULL_SUITE=1"
    echo "==> dotnet test tests/Server.Tests"
    dotnet test tests/Server.Tests -nodeReuse:false -p:AMBIT_FULL_SUITE=1
    echo "==> dotnet test tests/Shared.Tests"
    dotnet test tests/Shared.Tests -nodeReuse:false -p:AMBIT_FULL_SUITE=1
    echo "==> full suite finished"
}

run_full() {
    local ts log
    ts="$(date +%Y%m%d-%H%M%S)"
    log="/tmp/ambit-full-suite-${ts}.log"
    if [[ "$(uname -s)" == Linux ]]; then
        # Cloud agent: return while the suite runs.
        export -f full_suite
        nohup bash -c 'full_suite' >"$log" 2>&1 </dev/null &
        echo "log: $log"
        echo "pid: $!"
    elif [[ "${CURSOR_EXTENSION_HOST_ROLE:-}" == agent-exec ]]; then
        # Desktop agent shell is Git Bash. Background and disown so this script can exit.
        export -f full_suite
        bash -c 'full_suite' >"$log" 2>&1 </dev/null &
        disown
        echo "log: $log"
        echo "pid: $!"
    else
        full_suite
        echo "log: $log"
    fi
}

project_for() {
    local dir matches
    dir="$(dirname "$1")"
    shopt -s nullglob
    while [[ "$dir" == tests/* ]]; do
        matches=("$dir"/*.fsproj)
        if [[ ${#matches[@]} -gt 0 ]]; then
            echo "$dir"
            shopt -u nullglob
            return 0
        fi
        dir="$(dirname "$dir")"
    done
    shopt -u nullglob
    return 1
}

declare -A STEMS_FOR_PROJECT=()

add_name() {
    local project="$1" name="$2" current
    current="${STEMS_FOR_PROJECT[$project]:-}"
    case $'\n'"$current" in
        *$'\n'"$name"$'\n'*) ;;
        *) STEMS_FOR_PROJECT["$project"]="${current}${name}"$'\n' ;;
    esac
}

run_mapped() {
    local base changed path stem key tpath project name filter names
    if ! git rev-parse --verify --quiet origin/staging >/dev/null; then
        echo "origin/staging is missing" >&2
        exit 1
    fi
    base="$(git merge-base HEAD origin/staging)"
    changed="$(
        {
            git diff --name-only --diff-filter=ACMR "$base"
            git ls-files --others --exclude-standard
        } | sed '/^$/d' | sort -u
    )"

    declare -A PATHS_BY_STEM=()
    while IFS= read -r path; do
        [[ -z "$path" ]] && continue
        stem="$(basename "$path" .fs)"
        PATHS_BY_STEM["$stem"]+="${path}"$'\n'
    done < <(find tests -type f -name '*Tests.fs' -not -path '*/bin/*' -not -path '*/obj/*')

    while IFS= read -r path; do
        [[ -z "$path" ]] && continue
        if [[ "$path" =~ ^tests/.+Tests\.fs$ ]]; then
            project="$(project_for "$path")" || continue
            add_name "$project" "$(basename "$path" .fs)"
        elif [[ "$path" =~ ^src/.+\.fs$ ]]; then
            stem="$(basename "$path" .fs)"
            key="${stem}Tests"
            [[ -n "${PATHS_BY_STEM[$key]:-}" ]] || continue
            while IFS= read -r tpath; do
                [[ -z "$tpath" ]] && continue
                project="$(project_for "$tpath")" || continue
                add_name "$project" "$key"
            done <<<"${PATHS_BY_STEM[$key]}"
        fi
    done <<<"$changed"

    if [[ ${#STEMS_FOR_PROJECT[@]} -eq 0 ]]; then
        echo "No edited files map to a test run."
        exit 0
    fi

    local projects=()
    while IFS= read -r project; do
        [[ -n "$project" ]] && projects+=("$project")
    done < <(printf '%s\n' "${!STEMS_FOR_PROJECT[@]}" | sort)

    for project in "${projects[@]}"; do
        names="$(printf '%s' "${STEMS_FOR_PROJECT[$project]}" | sed '/^$/d' | sort -u)"
        filter=""
        while IFS= read -r name; do
            [[ -z "$name" ]] && continue
            if [[ -z "$filter" ]]; then
                filter="FullyQualifiedName~${name}"
            else
                filter="${filter}|FullyQualifiedName~${name}"
            fi
        done <<<"$names"
        run_filtered "$project" "$filter"
    done
}

if [[ $# -eq 0 ]]; then
    run_mapped
    exit 0
fi

case "$1" in
    --full)
        [[ $# -eq 1 ]] || usage
        run_full
        ;;
    --filter)
        [[ $# -eq 2 || $# -eq 3 ]] || usage
        [[ -n "$2" ]] || usage
        case "${3:-both}" in
            server) run_filtered tests/Server.Tests "$2" ;;
            shared) run_filtered tests/Shared.Tests "$2" ;;
            both)
                run_filtered tests/Server.Tests "$2"
                run_filtered tests/Shared.Tests "$2"
                ;;
            *) usage ;;
        esac
        ;;
    *)
        usage
        ;;
esac
