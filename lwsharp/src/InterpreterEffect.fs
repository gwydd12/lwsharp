// InterpreterEffect.fs
module lwsharp.InterpreterEffect

open lwsharp.Syntax

// New computation type that carries the actor reference
type Computation<'a> =
    Computation of (MailboxProcessor<StoreActor.StoreMessage> -> Async<Result<'a, Errors.RuntimeError>>)

let run (Computation f) (storeActor: MailboxProcessor<StoreActor.StoreMessage>) : Async<Result<'a, Errors.RuntimeError>> =
    f storeActor

let returnValue (x: 'a) : Computation<'a> =
    Computation (fun _ -> async { return Ok x })

let bind (f: 'a -> Computation<'b>) (Computation g) : Computation<'b> =
    Computation (fun storeActor ->
        async {
            let! result = g storeActor
            match result with
            | Error err -> 
                return Error err
            | Ok value ->
                let (Computation h) = f value
                return! h storeActor
        })

// Helper to lift async operations into our Computation
let liftAsync (f: MailboxProcessor<StoreActor.StoreMessage> -> Async<Result<'a, Errors.RuntimeError>>) : Computation<'a> =
    Computation f

// Helper to wrap store actor calls
let readVarComp (var: string) : Computation<int> =
    Computation (fun storeActor ->
        async {
            return! StoreActor.readVar storeActor var
        })

let writeVarComp (var: string) (value: int) : Computation<unit> =
    Computation (fun storeActor ->
        async {
            do! StoreActor.writeVar storeActor var value
            return Ok ()
        })