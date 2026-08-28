namespace Gambol.Shared

[<StructuralEquality; StructuralComparison>]
[<RequireQualifiedAccess>]
type ClusterStep =
    | Root
    | Structural of string
    | Content of string
    | StructuralUp
    | DirectoryUp
    | Tree
    | ChildAt of int option
    | SiblingAt of int option

type PathCluster = ClusterStep list

[<StructuralEquality; StructuralComparison>]
[<RequireQualifiedAccess>]
type ExprTerm =
    | Word of string * string option
    | Cluster of PathCluster * string option

type ExprSeq = ExprTerm list
