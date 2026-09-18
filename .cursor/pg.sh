# Shared Cloud Agent Postgres 17 helpers. Sourced by install.sh and start.sh.
# Native cluster (no Docker, no systemd). Compose-style role: gambol / gambol_dev.

PGDATA="${HOME}/.local/share/gambol-postgres"
PGBIN="/usr/lib/postgresql/17/bin"
PGHOST_TCP="127.0.0.1"
PGPORT="5432"
PGUSER_NAME="gambol"
PGPASSWORD_VALUE="gambol_dev"
PGDB_DEV="gambol"
PGDB_TEST="gambol_test"
CONN_TEST="Host=localhost;Database=${PGDB_TEST};Username=${PGUSER_NAME};Password=${PGPASSWORD_VALUE}"
CONN_DEV="Host=localhost;Database=${PGDB_DEV};Username=${PGUSER_NAME};Password=${PGPASSWORD_VALUE}"

append_once() {
    file="$1"
    line="$2"
    if [ ! -f "$file" ]; then
        touch "$file"
    fi
    if ! grep -Fqx "$line" "$file"; then
        echo "$line" >> "$file"
    fi
}

upsert_env_line() {
    file="$1"
    key="$2"
    value="$3"
    sudo touch "$file"
    if sudo grep -q "^${key}=" "$file"; then
        sudo sed -i "s|^${key}=.*|${key}=${value}|" "$file"
    else
        echo "${key}=${value}" | sudo tee -a "$file" >/dev/null
    fi
}

persist_cloud_env() {
    export DOTNET_ROOT="${HOME}/.dotnet"
    export PATH="${DOTNET_ROOT}:${DOTNET_ROOT}/tools:${PGBIN}:${PATH}"
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    export DOTNET_NOLOGO=1
    export TEST_DB_CONNECTION_STRING="$CONN_TEST"
    export DB_CONNECTION_STRING="$CONN_DEV"

    DOTNET_ROOT_LINE='export DOTNET_ROOT="$HOME/.dotnet"'
    PATH_LINE='export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:/usr/lib/postgresql/17/bin:$PATH"'
    TEST_LINE="export TEST_DB_CONNECTION_STRING='${CONN_TEST}'"
    DEV_LINE="export DB_CONNECTION_STRING='${CONN_DEV}'"

    append_once "${HOME}/.profile" "$DOTNET_ROOT_LINE"
    append_once "${HOME}/.profile" "$PATH_LINE"
    append_once "${HOME}/.profile" "$TEST_LINE"
    append_once "${HOME}/.profile" "$DEV_LINE"
    append_once "${HOME}/.bashrc" "$DOTNET_ROOT_LINE"
    append_once "${HOME}/.bashrc" "$PATH_LINE"
    append_once "${HOME}/.bashrc" "$TEST_LINE"
    append_once "${HOME}/.bashrc" "$DEV_LINE"

    sudo tee /etc/profile.d/gambol-cloud.sh >/dev/null <<EOF
export DOTNET_ROOT="\$HOME/.dotnet"
export PATH="\$DOTNET_ROOT:\$DOTNET_ROOT/tools:/usr/lib/postgresql/17/bin:\$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export TEST_DB_CONNECTION_STRING='${CONN_TEST}'
export DB_CONNECTION_STRING='${CONN_DEV}'
EOF

    upsert_env_line /etc/environment TEST_DB_CONNECTION_STRING "$CONN_TEST"
    upsert_env_line /etc/environment DB_CONNECTION_STRING "$CONN_DEV"
}

apt_noninteractive() {
    sudo DEBIAN_FRONTEND=noninteractive apt-get \
        -o Dpkg::Options::="--force-confdef" \
        -o Dpkg::Options::="--force-confold" \
        "$@"
}

install_postgres_packages() {
    export DEBIAN_FRONTEND=noninteractive
    sudo apt-get update -qq
    apt_noninteractive install -y --no-install-recommends ca-certificates curl postgresql-common
    if [ -x /usr/share/postgresql-common/pgdg/apt.postgresql.org.sh ]; then
        sudo DEBIAN_FRONTEND=noninteractive /usr/share/postgresql-common/pgdg/apt.postgresql.org.sh -y
    else
        sudo install -d /usr/share/postgresql-common/pgdg
        sudo curl --retry 3 --retry-delay 2 -fsSL -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
            https://www.postgresql.org/media/keys/ACCC4CF8.asc
        echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt $(. /etc/os-release && echo "$VERSION_CODENAME")-pgdg main" \
            | sudo tee /etc/apt/sources.list.d/pgdg.list >/dev/null
        sudo apt-get update -qq
    fi
    echo 'create_main_cluster = false' | sudo tee /etc/postgresql-common/createcluster.conf >/dev/null
    apt_noninteractive install -y --no-install-recommends postgresql-17 postgresql-client-17
    if [ ! -x "${PGBIN}/pg_ctl" ]; then
        echo "PostgreSQL 17 binaries missing at ${PGBIN}" >&2
        exit 1
    fi
}

remove_stale_pid() {
    if [ ! -f "${PGDATA}/postmaster.pid" ]; then
        return 0
    fi
    pid="$(head -1 "${PGDATA}/postmaster.pid")"
    if [ -n "$pid" ] && kill -0 "$pid" 2>/dev/null; then
        return 0
    fi
    rm -f "${PGDATA}/postmaster.pid"
}

pg_running() {
    [ -x "${PGBIN}/pg_ctl" ] && "${PGBIN}/pg_ctl" -D "$PGDATA" status >/dev/null 2>&1
}

init_cluster() {
    if [ -f "${PGDATA}/PG_VERSION" ]; then
        return 0
    fi
    mkdir -p "$PGDATA"
    pwfile="$(mktemp)"
    printf '%s\n' "$PGPASSWORD_VALUE" > "$pwfile"
    chmod 600 "$pwfile"
    "${PGBIN}/initdb" \
        -D "$PGDATA" \
        --username="$PGUSER_NAME" \
        --pwfile="$pwfile" \
        --auth-host=scram-sha-256 \
        --auth-local=trust \
        --encoding=UTF8 \
        --locale=C.UTF-8
    rm -f "$pwfile"
    cat >> "${PGDATA}/postgresql.conf" <<EOF
listen_addresses = '${PGHOST_TCP}'
port = ${PGPORT}
unix_socket_directories = '/tmp'
EOF
}

start_postgres() {
    remove_stale_pid
    if pg_running; then
        return 0
    fi
    "${PGBIN}/pg_ctl" -D "$PGDATA" -l "${PGDATA}/postgres.log" -w start
}

stop_postgres() {
    if pg_running; then
        "${PGBIN}/pg_ctl" -D "$PGDATA" -m fast -w stop
    fi
}

ensure_databases() {
    "${PGBIN}/psql" -h /tmp -U "$PGUSER_NAME" -d postgres -v ON_ERROR_STOP=1 -c 'SELECT 1' >/dev/null
    for db in "$PGDB_DEV" "$PGDB_TEST"; do
        exists="$("${PGBIN}/psql" -h /tmp -U "$PGUSER_NAME" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='${db}'")"
        if [ "$exists" != "1" ]; then
            "${PGBIN}/psql" -h /tmp -U "$PGUSER_NAME" -d postgres -v ON_ERROR_STOP=1 \
                -c "CREATE DATABASE ${db} OWNER ${PGUSER_NAME};"
        fi
    done
}

wait_for_tcp() {
    i=0
    while [ "$i" -lt 30 ]; do
        if "${PGBIN}/pg_isready" -h "$PGHOST_TCP" -p "$PGPORT" >/dev/null 2>&1; then
            return 0
        fi
        i=$((i + 1))
        sleep 1
    done
    echo "Postgres did not accept connections on ${PGHOST_TCP}:${PGPORT}" >&2
    if [ -f "${PGDATA}/postgres.log" ]; then
        tail -n 40 "${PGDATA}/postgres.log" >&2
    fi
    exit 1
}

verify_postgres_tcp() {
    PGPASSWORD="$PGPASSWORD_VALUE" "${PGBIN}/psql" \
        -h "$PGHOST_TCP" -p "$PGPORT" -U "$PGUSER_NAME" -d "$PGDB_TEST" \
        -v ON_ERROR_STOP=1 -c 'SELECT 1' >/dev/null
}
