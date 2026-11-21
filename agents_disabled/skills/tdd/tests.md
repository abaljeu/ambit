# Test examples

Companion examples for [[SKILL.md]] (normative rules stay there). xUnit / F# shapes used in Shared.Tests.

## Behavior through the public seam

```fsharp
[<Fact>]
let ``checkout confirms a valid cart`` () =
    let cart = Cart.create () |> Cart.add product
    let result = Checkout.run cart paymentMethod
    Assert.Equal Confirmed result.Status
```

## Independent expected value

```fsharp
[<Fact>]
let ``total sums line item prices`` () =
    let items = [ { Price = 10 }; { Price = 5 } ]
    Assert.equal 15 (Totals.sum items)
```

Avoid recomputing the expected value the same way the production code does; use a known literal or worked example from the spec.
