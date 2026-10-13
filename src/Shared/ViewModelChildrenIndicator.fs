namespace Gambol.Shared

/// Left-edge children indicator on an outline row.
[<RequireQualifiedAccess>]
type RowChildrenIndicator =
    | FoldChevron
    | SolidCircle
    | HollowCircle
    | SlashedHollowCircle

module ViewModelChildrenIndicator =

    /// Slashed hollow for Unparsed; hollow for Unloaded;
    /// solid for Loaded+Parsed leaves;
    /// chevron for Loaded+Parsed nodes with children.
    let rowChildrenIndicator (graph: Graph) (node: Node) : RowChildrenIndicator =
        if node.documentState = Unparsed then
            RowChildrenIndicator.SlashedHollowCircle
        else
            match GraphChildren.tryGet graph node.id with
            | None -> RowChildrenIndicator.HollowCircle
            | Some [] -> RowChildrenIndicator.SolidCircle
            | Some _ -> RowChildrenIndicator.FoldChevron
