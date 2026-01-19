module lwsharp.InterpreterEffect

type Computation<'a> =
    Computation of (State.Store -> Result<'a * State.Store, Errors.RuntimeError>)
    
let run (Computation f) state =
    f state
    
let returnValue x =
    Computation (fun state -> Ok (x, state))
    
let bind f (Computation g) =
    Computation (fun state ->
        match g state with
        | Error err -> Error err
        | Ok (value, newState) ->
            let (Computation h) = f value
            h newState
            )