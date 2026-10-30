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
        if Node.documentStateEnum node = Unparsed then
            RowChildrenIndicator.SlashedHollowCircle
        else
            match GraphChildren.tryGet graph node.id with
            | None -> RowChildrenIndicator.HollowCircle
            | Some [] -> RowChildrenIndicator.SolidCircle
            | Some _ -> RowChildrenIndicator.FoldChevron

    /// Placeholder Bullet for an Absent Header. The row view paints it #444444.
    let placeholderBulletIndicator : RowChildrenIndicator =
        RowChildrenIndicator.SolidCircle

    /// Resident Header keeps its Bullet. An Absent Header is the placeholder Bullet.
    let rowChildrenIndicatorForId (graph: Graph) (nodeId: NodeId) : RowChildrenIndicator =
        match Map.tryFind nodeId graph.nodes with
        | None -> placeholderBulletIndicator
        | Some node -> rowChildrenIndicator graph node
