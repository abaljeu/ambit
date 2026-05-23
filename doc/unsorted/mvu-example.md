MVU
```F#
type Model = {
    Count: int
    Message: string
}
```
## View
The View is a function that takes the Model as input and produces the UI. In F#, this is often done using a declarative approach, where the UI is described as a series of nested functions or components.
```F#
let view model dispatch =
    div [] [
        h1 [] [ str model.Message ]
        button [ onClick (fun _ -> dispatch Increment) ] [ str "Increment" ]
        button [ onClick (fun _ -> dispatch Decrement) ] [ str "Decrement" ]
    ]
```
## Update
The Update function is responsible for handling messages and updating the Model. It takes the current Model and a message as input and returns a new Model.
```F#
type Msg =
    | Increment
    | Decrement

let update msg model =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }
    | Decrement -> { model with Count = model.Count - 1 }
```