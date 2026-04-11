namespace Gambol.Server

open System
open System.Threading.Tasks
open Npgsql
open Dapper

/// Low-level PostgreSQL helpers. All operations are explicit and typed to the
/// present model (Change JSON via Serialization, Graph text via Snapshot.write).
[<RequireQualifiedAccess>]
module Database =

    // ------------------------------------------------------------------
    // Connection
    // ------------------------------------------------------------------

    let getConnection (connectionString: string) =
        new NpgsqlConnection(connectionString)

    // ------------------------------------------------------------------
    // Schema bootstrap
    // ------------------------------------------------------------------

    let initSchema (connectionString: string) : Task =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            use cmd = conn.CreateCommand()
            cmd.CommandText <- """
                CREATE TABLE IF NOT EXISTS changes (
                    seq_id      BIGSERIAL    PRIMARY KEY,
                    change_id   INT          NOT NULL,
                    payload     TEXT         NOT NULL,
                    recorded_at TIMESTAMPTZ  DEFAULT NOW()
                );

                CREATE INDEX IF NOT EXISTS idx_changes_change_id
                    ON changes (change_id);

                CREATE TABLE IF NOT EXISTS snapshots (
                    id          BIGSERIAL    PRIMARY KEY,
                    revision    INT          NOT NULL,
                    content     TEXT         NOT NULL,
                    recorded_at TIMESTAMPTZ  DEFAULT NOW()
                );
            """
            do! cmd.ExecuteNonQueryAsync() :> Task
        }

    // ------------------------------------------------------------------
    // Changes table
    // ------------------------------------------------------------------

    type ChangeRow =
        { change_id: int
          payload: string }

    /// Append a change to the log. Payload is the compact JSON of the Change.
    let appendChange (connectionString: string) (changeId: int) (json: string) : Task =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            do! conn.ExecuteAsync(
                    "INSERT INTO changes (change_id, payload) VALUES (@change_id, @payload)",
                    {| change_id = changeId; payload = json |})
                :> Task
        }

    /// Load all change rows with change_id > afterChangeId, ordered ascending.
    let getChangesAfter (connectionString: string) (afterChangeId: int) : Task<ChangeRow list> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            let! rows =
                conn.QueryAsync<ChangeRow>(
                    "SELECT change_id, payload FROM changes WHERE change_id > @after ORDER BY change_id ASC",
                    {| after = afterChangeId |})
            return rows |> Seq.toList
        }

    // ------------------------------------------------------------------
    // Snapshots table
    // ------------------------------------------------------------------

    type SnapshotRow =
        { revision: int
          content: string }

    /// Insert a new snapshot. Each snapshot is the full Snapshot.write text
    /// plus the revision it was taken at.
    let insertSnapshot (connectionString: string) (revision: int) (content: string) : Task =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            do! conn.ExecuteAsync(
                    "INSERT INTO snapshots (revision, content) VALUES (@revision, @content)",
                    {| revision = revision; content = content |})
                :> Task
        }

    /// Load the most recent snapshot, or None if no snapshots exist yet.
    let getLatestSnapshot (connectionString: string) : Task<SnapshotRow option> =
        task {
            use conn = getConnection connectionString
            do! conn.OpenAsync()
            let! row =
                conn.QueryFirstOrDefaultAsync<SnapshotRow>(
                    "SELECT revision, content FROM snapshots ORDER BY revision DESC LIMIT 1")
            return if obj.ReferenceEquals(row, null) then None else Some row
        }
