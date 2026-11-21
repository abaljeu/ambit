namespace Gambol.Server

open System.Threading.Tasks

/// One reset between tests. Stops the previous server, then clears storage.
[<RequireQualifiedAccess>]
module ServerTestReset =

    /// Stop the previous server, wait until its persist thread has logged,
    /// then clear the database and process-wide caches.
    let resetBetweenTests (connectionString: string) : Task<unit> =
        task {
            ServerRunRegistry.stopAll ()
            DatabaseSetup.resetAgentCacheForTest ()
            do! Database.resetStoredTables connectionString
            return ()
        }
