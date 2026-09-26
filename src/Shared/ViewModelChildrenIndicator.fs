namespace Gambol.Shared

/// Left-edge children indicator on an outline row (chevron vs solid/hollow circle).
[<RequireQualifiedAccess>]
type RowChildrenIndicator =
    | FoldChevron
    | SolidCircle
    | HollowCircle

module ViewModelChildrenIndicator =

    /// Hollow for Unloaded or Unparsed leaves; solid for Loaded+Parsed leaves;
    /// chevron when resident children are present.
    let rowChildrenIndicator (graph: Graph) (node: Node) : RowChildrenIndicator =
        let kids = GraphChildren.get graph node.id
        if not kids.IsEmpty then
            RowChildrenIndicator.FoldChevron
        elif
            not (GraphChildren.isLoaded graph node.id)
            || node.documentState = Unparsed then
            RowChildrenIndicator.HollowCircle
        else
            RowChildrenIndicator.SolidCircle
