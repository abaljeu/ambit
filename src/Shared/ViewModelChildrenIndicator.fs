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
    let rowChildrenIndicator (node: Node) : RowChildrenIndicator =
        if not node.children.IsEmpty then
            RowChildrenIndicator.FoldChevron
        elif node.childrenStatus = Unloaded || node.documentState = Unparsed then
            RowChildrenIndicator.HollowCircle
        else
            RowChildrenIndicator.SolidCircle
