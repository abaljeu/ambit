namespace Gambol.Shared

type DesktopImportPackage =
    { sourcePath: string
      isDirectory: bool
      topLevelIds: NodeId list
      ops: Op list }
