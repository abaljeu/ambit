namespace Gambol.Shared

[<RequireQualifiedAccess>]
type ExprSlotKind =
    | NameGlob
    | QuotedText
    | IntOrStar
    | Int

[<RequireQualifiedAccess>]
type ExprBoundSlot =
    | NoArgument
    | NameGlob of string
    | QuotedText of string
    | IntOrStar of int option
    | Int of int

type ExprCatalogRow =
    { spellings: string list
      slot: ExprSlotKind option
      signature: ExprSignature
      evaluate: ExprBoundSlot -> ExprEval.Predicate }

[<RequireQualifiedAccess>]
module ExprCatalog =
    type T = private | Catalog of Map<string, ExprCatalogRow>

    let empty = Catalog Map.empty

    let register (row: ExprCatalogRow) (Catalog catalog) : T =
        let withRow =
            row.spellings
            |> List.fold (fun acc spelling -> Map.add spelling row acc) catalog
        Catalog withRow

    let lookup (spelling: string) (Catalog catalog) : ExprCatalogRow option =
        Map.tryFind spelling catalog

    let invoke (bound: ExprBoundSlot) (row: ExprCatalogRow) (input: ExprAnswer) : ExprEval.Stream =
        row.evaluate bound input
