namespace Gambol.Shared

/// Left-edge children indicator on an outline row (chevron vs solid/hollow circle).
[<RequireQualifiedAccess>]
type RowChildrenIndicator =
    | FoldChevron
    | SolidCircle
    | HollowCircle

module ViewModelChildrenIndicator =

    /// Hollow for Unloaded or Unparsed; solid for Loaded+Parsed leaves;
    /// chevron for Loaded+Parsed Nodes with Children.
    let rowChildrenIndicator (graph: Graph) (node: Node) : RowChildrenIndicator =
        if node.documentState = Unparsed then
            RowChildrenIndicator.HollowCircle
        else
            match GraphChildren.tryGet graph node.id with
            | None -> RowChildrenIndicator.HollowCircle
            | Some [] -> RowChildrenIndicator.SolidCircle
            | Some _ -> RowChildrenIndicator.FoldChevron
