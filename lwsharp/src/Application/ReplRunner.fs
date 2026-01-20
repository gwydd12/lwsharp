module lwsharp.ReplRunner

open Akka.Actor
open lwsharp.Adapters.ReplAdapter
open lwsharp.StoreActor
open lwsharp.Ports

(**
Runs a REPL for interactive execution of statements.
It has a tail call to allow continuous input until "exit" is typed.
Meaning that the function reuses the same stack frame for each iteration of the loop.
This is an important optimization for long-running REPL sessions to prevent stack overflow errors.
*)
let runRepl (system: ActorSystem) : Async<unit> =
    async {
        let storeActor = createStoreActor system
        let execAdapter = ReplAdapter(storeActor)

        let rec readLoop () =
            async {
                printf "> "
                let input = System.Console.ReadLine()
                if input <> "exit" then
                    let! result = (execAdapter :> IExecutionMode).ExecuteStatement input
                    match result with
                    | Ok _ ->
                        let! store = getState storeActor
                        printfn "Store: %A" store
                    | Error err -> printfn "Error: %A" err
                    return! readLoop () (* Tail call, explicitly tell the runtime to remove fomr *)
            }

        return! readLoop ()
    }