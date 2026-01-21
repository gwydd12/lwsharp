module lwsharp.InterpreterEffect

open Akka.Actor

type Computation<'a> =
    Computation of (IActorRef -> Async<Result<'a, Errors.RuntimeError>>)
    
let run (Computation f) (storeActor: IActorRef) : Async<Result<'a, Errors.RuntimeError>> =
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
    
let (>>=) = bind
    
let apply (mf: Computation<'a -> 'b>) (mx: Computation<'a>) : Computation<'b> =
    mf |> bind (fun f ->
    mx |> bind (fun x ->
    returnValue (f x)))

let (<*>) = apply

let map (f: 'a -> 'b) (mx: Computation<'a>) : Computation<'b> =
    mx |> bind (fun x -> returnValue(f x))

let id (x: 'a) : 'a = x // our identity function

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