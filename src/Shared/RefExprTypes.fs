namespace Gambol.Shared

type ExprAnchor =
    | Context
    | WorkspaceRoot
    | GlobalRoot
    | CurrentDir
    | Structural
    | Tagged

type ExprStep =
    | DirStep of string
    | FileStep of string
    | TagStep of string
    | MultiWild

type PathExpr =
    | AnchorOnly of ExprAnchor
    | Path of ExprAnchor * ExprStep list

type RefContext =
    { contextNode: NodeId
      workspaceRoot: NodeId option
      currentDir: NodeId option
      structural: NodeId option
      tagged: NodeId option }
