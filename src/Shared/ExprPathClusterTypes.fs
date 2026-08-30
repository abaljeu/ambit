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
    /// A quoted string in Expression position: yields that Text from any input.
    | Text of string

[<StructuralEquality; StructuralComparison>]
[<RequireQualifiedAccess>]
type Expr =
    | Term of ExprTerm
    | Pipe of Expr list
    | Not of Expr
    | Outer of Expr
    | If of Expr
    | And of Expr * Expr
    | Or of Expr * Expr
    | Is of Expr * Expr
