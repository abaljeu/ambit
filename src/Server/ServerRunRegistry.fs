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
    let private persistQueued = ref 0

    /// A persist request was pushed.
    let notePersistQueued () =
        System.Threading.Interlocked.Increment persistQueued |> ignore

    /// That request has left the persist thread.
    let notePersistFinished () =
        System.Threading.Interlocked.Decrement persistQueued |> ignore

    let persistPendingCount () = persistQueued.Value

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
        |> List.iter (fun stop ->
            try
                stop ()
            with _ -> ())

    /// Abandoned appends can track a follower after the first wait starts.
    let private waitOutstanding () =
        let rec wait () =
            let pending =
                takeTasks ()
                |> List.filter (fun task -> not task.IsCompleted)
            if List.isEmpty pending then
                ()
            else
                pending
                |> List.iter (fun task ->
                    try
                        task.Wait()
                    with _ -> ())
                wait ()
        wait ()

    let private restoreHosts (hosts: ServerRunStop list) =
        lock gate (fun () -> stops.Value <- hosts @ stops.Value)

    /// Mailbox first, while persist can still log. Then join persist so a
    /// late notice cannot be written after this returns.
    let private finishWorkers (items: ServerRunStop list) =
        run (function ServerRunStop.Pool stop -> Some stop | _ -> None) items
        run (function
            | ServerRunStop.ParseUnblock stop -> Some stop
            | _ -> None) items
        run (function ServerRunStop.ParseJoin stop -> Some stop | _ -> None) items
        run (function ServerRunStop.Mailbox stop -> Some stop | _ -> None) items
        run (function
            | ServerRunStop.PersistUnblock stop -> Some stop
            | _ -> None) items
        run (function ServerRunStop.PersistJoin stop -> Some stop | _ -> None) items
        waitOutstanding ()

    /// Finish parse, mailbox, and persist. Host stops stay registered.
    let drainBackground () =
        let hosts, workers =
            takeStops ()
            |> List.partition (function
                | ServerRunStop.Host _ -> true
                | _ -> false)
        finishWorkers workers
        restoreHosts hosts

    /// Stop background work, then the host. Actor bodies are cancelled.
    let stopAll () =
        drainBackground ()
        let items = takeStops ()
        run (function ServerRunStop.Host stop -> Some stop | _ -> None) items
        waitOutstanding ()
