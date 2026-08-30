namespace Gambol.Shared

type AmbleNumber =
    | Int of int64
    | Float of decimal

type AmbleExpr =
    | Ref of PathExpr
    | Str of string
    | Num of AmbleNumber
    | FunCall of fn: string * args: AmbleExpr list
    | Paren of AmbleExpr
    | Cmd of AmbleStage list

and AmbleShellWord =
    | WordRef of PathExpr
    | WordBare of string
    | WordStr of string
    | WordExpr of AmbleExpr

and AmbleStagePart =
    | ShellWord of AmbleShellWord
    | RedirIn of AmbleExpr
    | RedirOut of AmbleExpr
    | RedirAppend of AmbleExpr

and AmbleStage = AmbleStagePart list

type AmbleStatement =
    | Assign of name: string * ref: AmbleExpr
    | ExprStmt of AmbleExpr
