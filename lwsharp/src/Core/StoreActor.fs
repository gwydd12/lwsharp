module lwsharp.StoreActor

open Akka.Actor
open Akka.FSharp
open Errors

type StoreMessage =
    | ReadVar of string
    | WriteVar of string * int
    | GetState

let createStoreActor (system: ActorSystem) : IActorRef =
    let actorName = "store" + System.Guid.NewGuid().ToString("N")
    spawn system actorName (fun mailbox ->
        let rec loop (store: State.Store) =
            actor {
                let! msg = mailbox.Receive()
                let sender = mailbox.Sender()

                match msg with
                | ReadVar var ->
                    let result =
                        match Map.tryFind var store with
                        | Some v -> Ok v
                        | None -> Error (UndefinedVariable var)
                    sender <! result
                    return! loop store

                | WriteVar (var, value) ->
                    let newStore = Map.add var value store
                    sender <! ()
                    return! loop newStore

                | GetState ->
                    sender <! store
                    return! loop store 
            }
        loop State.emptyStore
    )


let readVar (actor: IActorRef) (var: string)
    : Async<Result<int, RuntimeError>> =
    actor.Ask<Result<int, RuntimeError>>(ReadVar var)
    |> Async.AwaitTask

let writeVar (actor: IActorRef) (var: string) (value: int)
    : Async<unit> =
    actor.Ask<unit>(WriteVar (var, value))
    |> Async.AwaitTask

let getState (actor: IActorRef)
    : Async<State.Store> =
    actor.Ask<State.Store>(GetState)
    |> Async.AwaitTask