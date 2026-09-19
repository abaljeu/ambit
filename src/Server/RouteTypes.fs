namespace Gambol.Server

type PersistenceContext =
    {
        DataDir: string
        DbStatus: DatabaseSetup.DbStatus
        Core: CoreRuntime
    }

type RouteAssets =
    {
        GambolHtml: string
        DefaultUserCss: string
        CommandDockSvg: string
    }

type BuildStamps =
    {
        DeployStamp: unit -> string
        PageBuildStamp: unit -> string
        PageBuildEpochSec: unit -> int
        DeployEpochSec: unit -> int
        InlineCommandDockSprite: unit -> string
    }

/// Host plus assets, stamps, and persistence for route registration.
type AppShellContext =
    {
        AmbitApp: AmbitApp
        Assets: RouteAssets
        Stamps: BuildStamps
        Persistence: PersistenceContext
    }
