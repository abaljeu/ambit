namespace Gambol.Shared

/// Pure helpers for picking client (x, y) hit points inside a DOMRect for caret APIs.

[<RequireQualifiedAccess>]
module ClientRectCaret =
    let clamp (lo: float) (hi: float) (v: float) : float =
        let a = min lo hi
        let b = max lo hi
        max a (min b v)

    let private nonNegativeSpan (lo: float) (hi: float) : float =
        max 0. (hi - lo)
    /// Client (x, y) for caretRangeFromPoint near the top-left inside the element rect.

    let probeFirstVisualLine
        (left: float)
        (top: float)
        (right: float)
        (bottom: float)
        (inset: float)
        : float * float =
        let w = nonNegativeSpan left right
        let h = nonNegativeSpan top bottom
        let ins = min inset (max 1. (min w h / 4.))
        let x = clamp left right (left + ins)
        let y = clamp top bottom (top + ins)
        (x, y)
    /// Client (x, y) near the bottom-left inside the element rect (last visual line).

    let probeLastVisualLine
        (left: float)
        (top: float)
        (right: float)
        (bottom: float)
        (inset: float)
        : float * float =
        let w = nonNegativeSpan left right
        let h = nonNegativeSpan top bottom
        let ins = min inset (max 1. (min w h / 4.))
        let x = clamp left right (left + ins)
        let y = clamp top bottom (bottom - ins)
        (x, y)
