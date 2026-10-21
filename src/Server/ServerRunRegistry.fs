namespace Gambol.Server

open System.Threading.Tasks

/// One registered stop for a server started in this process.
[<RequireQualifiedAccess>]
type internal ServerRunStop =
    | Host of (unit -> unit)
    | ParseUnblock of (unit -> unit)
    | ParseJoin of (unit -> unit)
    | PersistUnblock of (unit -> unit)
    | PersistJoin of (unit -> unit)
    | Pool of (unit -> unit)
    | Mailbox of (unit -> unit)

/// Process-wide handles for servers and background work tests must stop.
module internal ServerRunRegistry =

    let private gate = obj ()
    let private stops = ref ([] : ServerRunStop list)
    let private tasks = ref ([] : Task list)

    let register (stop: ServerRunStop) =
        lock gate (fun () -> stops.Value <- stop :: stops.Value)

    let registerHost stop =
        register (ServerRunStop.Host stop)

    let registerParseUnblock stop =
        register (ServerRunStop.ParseUnblock stop)

    let registerParseJoin stop =
        register (ServerRunStop.ParseJoin stop)

    let registerPersistUnblock stop =
        register (ServerRunStop.PersistUnblock stop)

    let registerPersistJoin stop =
        register (ServerRunStop.PersistJoin stop)

    let registerPool stop =
        register (ServerRunStop.Pool stop)

    let registerMailbox stop =
        register (ServerRunStop.Mailbox stop)

    /// Remember a background task so reset can wait until it finishes.
    let track (task: Task) =
        lock gate (fun () ->
            let live =
                tasks.Value
                |> List.filter (fun item -> not item.IsCompleted)
            tasks.Value <- task :: live)

    let private takeStops () =
        lock gate (fun () ->
            let copy = stops.Value
            stops.Value <- []
            copy)

    let private takeTasks () =
        lock gate (fun () ->
            let copy = tasks.Value
            tasks.Value <- []
            copy)

    let private run
        (choose: ServerRunStop -> (unit -> unit) option)
        (items: ServerRunStop list)
        =
        items
        |> List.choose choose
        |> List.iter (fun stop -> stop ())

    let private waitTasks () =
        takeTasks ()
        |> List.iter (fun task ->
            try
                task.Wait()
            with _ -> ())

    /// Stop hosts, actors, and parse. Shut the mailbox while persist can still
    /// answer it, then stop persist. Actor bodies are cancelled, not awaited.
    let stopAll () =
        let items = takeStops ()
        run (function ServerRunStop.Host stop -> Some stop | _ -> None) items
        run (function ServerRunStop.Pool stop -> Some stop | _ -> None) items
        run (function ServerRunStop.ParseUnblock s -> Some s | _ -> None) items
        run (function ServerRunStop.ParseJoin stop -> Some stop | _ -> None) items
        run (function ServerRunStop.Mailbox s -> Some s | _ -> None) items
        run (function ServerRunStop.PersistUnblock s -> Some s | _ -> None) items
        run (function ServerRunStop.PersistJoin s -> Some s | _ -> None) items
        waitTasks ()
