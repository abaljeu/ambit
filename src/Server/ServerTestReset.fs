namespace Gambol.Server

open System.Threading.Tasks

/// One reset between tests. Stops the previous server, then clears storage.
[<RequireQualifiedAccess>]
module ServerTestReset =

    /// Stop the previous server and its background threads, wait until they
    /// finish, then clear the database and process-wide caches.
    let resetBetweenTests (connectionString: string) : Task<unit> =
        task {
            ServerRunRegistry.stopAll ()
            DatabaseSetup.resetAgentCacheForTest ()
            do! Database.resetStoredTables connectionString
            return ()
        }
