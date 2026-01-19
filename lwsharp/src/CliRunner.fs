module lwsharp.CliRunner

open System
open System.IO
open Akka.Actor
open Akka.FSharp
open lwsharp.Parser
open lwsharp.Interpreter
open lwsharp.InterpreterEffect
open lwsharp.StoreActor
open lwsharp.Ports
open lwsharp.Errors

type CoordinatorMessage =
    | ExecuteProgram of filePath: string


let printResult (result: ProgramResult) : unit =
    let status = if result.Success then "SUCCESS" else "FAILED"
    printfn $"%s{status} - %s{result.FilePath}"

    if result.Success then
        if Map.isEmpty result.Store then
            printfn "  (No variables)"
        else
            result.Store
            |> Map.iter (fun var value ->
                printfn $"    %s{var} = %d{value}")
    else
        match result.Error with
        | Some err -> printfn $"  Error: %A{err}"
        | None -> printfn "  Error: Unknown"

    printfn ""
    
   
type LoggerMessage =
    | LogResult of ProgramResult
    | Shutdown

let createLoggerActor (system: ActorSystem) : IActorRef =
    let loggerName = "logger-" + Guid.NewGuid().ToString("N")
    spawn system loggerName (fun mailbox ->
        let rec loop () =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | LogResult result -> printResult result
                | Shutdown -> mailbox.Context.Stop(mailbox.Self)
                return! loop ()
            }
        loop ()
    )

let createProgramCoordinator (system: ActorSystem) (logger: IActorRef) : IActorRef =
    let coordinatorName = "coordinator-" + Guid.NewGuid().ToString("N")
    spawn system coordinatorName (fun mailbox ->
        let rec loop (results: ProgramResult list) =
            actor {
                let! msg = mailbox.Receive()

                match msg with
                | ExecuteProgram filePath ->
                    let programResult =
                        try
                            let source = File.ReadAllText(filePath)
                            match parseProgram source with
                            | Error parseErr ->
                                { FilePath = filePath
                                  Success = false
                                  Store = Map.empty
                                  Error = Some (UndefinedVariable parseErr) }
                            | Ok ast ->
                                let storeActor = createStoreActor system
                                let execResult =
                                    async {
                                        let (Computation comp) = evalStmt ast
                                        let! evalResult = comp storeActor
                                        match evalResult with
                                        | Ok () ->
                                            let! finalStore = getState storeActor
                                            return Ok finalStore
                                        | Error err ->
                                            return Error err
                                    } |> Async.RunSynchronously

                                match execResult with
                                | Ok store ->
                                    { FilePath = filePath
                                      Success = true
                                      Store = store
                                      Error = None }
                                | Error err ->
                                    { FilePath = filePath
                                      Success = false
                                      Store = Map.empty
                                      Error = Some err }
                        with ex ->
                            { FilePath = filePath
                              Success = false
                              Store = Map.empty
                              Error = Some (UndefinedVariable ex.Message) }
                    logger <! LogResult programResult
                    return! loop (programResult :: results)
            }
        loop []
    )

let executeProgramsParallel (system: ActorSystem) (filePaths: string list) : Async<ProgramResult list> =
    async {
        let logger = createLoggerActor system
        let tasks =
            filePaths
            |> List.map (fun filePath ->
                async {
                    let coordinator = createProgramCoordinator system logger
                    let! result =
                        coordinator.Ask<ProgramResult>(ExecuteProgram filePath)
                        |> Async.AwaitTask
                    return result
                })

        let! results = Async.Parallel tasks

        // Wait for logger to finish
        do! logger.Ask<unit>(Shutdown) |> Async.AwaitTask

        // Shut down the entire actor system
        system.Terminate() |> Async.AwaitTask |> ignore
        return List.ofArray results
    }