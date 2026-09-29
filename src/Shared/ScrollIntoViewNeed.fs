namespace Gambol.Shared

/// Pure vertical containment for Focus and Edit scroll.
/// `scrollIntoView` moves the window even when the line is already on screen.
[<RequireQualifiedAccess>]
module ScrollIntoViewNeed =
    type VerticalSpan = { top: float; bottom: float }

    type Query = {
        element: VerticalSpan
        view: VerticalSpan
        epsilon: float
    }

    /// Pixels to add to the scroller. Positive moves a line that sits below the view.
    /// Zero when the line is already inside, including the near-bottom Edit Up/Down case.
    let scrollDelta (q: Query) : float =
        let e = max 0. q.epsilon
        if q.element.bottom > q.view.bottom + e then
            q.element.bottom - q.view.bottom
        elif q.element.top < q.view.top - e then
            q.element.top - q.view.top
        else
            0.

    let isAlreadyVisible (q: Query) : bool =
        scrollDelta q = 0.

    let needsScroll (q: Query) : bool =
        not (isAlreadyVisible q)
