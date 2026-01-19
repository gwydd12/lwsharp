module lwsharp.StoreActor

open lwsharp.Syntax
open lwsharp.Errors

// Messages that can be sent to the store actor
type StoreMessage =
    | ReadVar of var: string * replyChannel: AsyncReplyChannel<Result<Value, RuntimeError>>
    | WriteVar of var: string * value: Value * replyChannel: AsyncReplyChannel<unit>
    | GetState of replyChannel: AsyncReplyChannel<State.Store>

// Create and start the store actor using F# mailbox
let createStoreActor () : MailboxProcessor<StoreMessage> =
    MailboxProcessor.Start(fun inbox ->
        let rec loop (store: State.Store) =
            async {
                let! msg = inbox.Receive()
                match msg with
                | ReadVar (var, replyChannel) ->
                    let result =
                        match Map.tryFind var store with
                        | Some v -> Ok v
                        | None -> Error (UndefinedVariable var)
                    replyChannel.Reply(result)
                    return! loop store
                    
                | WriteVar (var, value, replyChannel) ->
                    let newStore = Map.add var value store
                    replyChannel.Reply(())
                    return! loop newStore
                    
                | GetState replyChannel ->
                    replyChannel.Reply(store)
                    return! loop store
            }
        loop State.emptyStore
    )

// Helper functions to interact with the actor
let readVar (actor: MailboxProcessor<StoreMessage>) (var: string) : Async<Result<Value, RuntimeError>> =
    actor.PostAndAsyncReply(fun replyChannel -> ReadVar(var, replyChannel))

let writeVar (actor: MailboxProcessor<StoreMessage>) (var: string) (value: Value) : Async<unit> =
    actor.PostAndAsyncReply(fun replyChannel -> WriteVar(var, value, replyChannel))

let getState (actor: MailboxProcessor<StoreMessage>) : Async<State.Store> =
    actor.PostAndAsyncReply(fun replyChannel -> GetState(replyChannel))