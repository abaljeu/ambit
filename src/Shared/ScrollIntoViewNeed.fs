namespace Gambol.Shared

/// Pure vertical containment used to skip `scrollIntoView` when Focus is on-screen.
[<RequireQualifiedAccess>]
module ScrollIntoViewNeed =
    type VerticalSpan = { top: float; bottom: float }

    type Query = {
        element: VerticalSpan
        view: VerticalSpan
        epsilon: float
    }

    let isAlreadyVisible (q: Query) : bool =
        let e = max 0. q.epsilon
        q.element.top >= q.view.top - e
        && q.element.bottom <= q.view.bottom + e

    let needsScroll (q: Query) : bool =
        not (isAlreadyVisible q)
