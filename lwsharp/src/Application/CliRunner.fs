module lwsharp.CliRunner

open Akka.Actor
open lwsharp.Adapters.CliAdapter
open lwsharp.Pipeline
open lwsharp.Ports
open lwsharp.ExecutionEvents

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

let executeProgramsParallel (system: ActorSystem) (filePaths: string list) : Async<ProgramResult list> =
    async {
        let adapter = CliAdapter system
        let tasks =
            filePaths
            |> List.map (fun filePath ->
                async {
                    let! result = (adapter :> IExecutionMode).ExecuteFile filePath
                    match result with
                    | Ok programResult ->
                        printResult programResult
                        return programResult
                    | Error err ->
                        let failed = { FilePath = filePath; Success = false; Store = Map.empty; Error = Some err }
                        printResult failed
                        return failed
                })

        let! results = Async.Parallel tasks
        return List.ofArray results
    }
    
let outputLock = System.Object()

let safeWrite (observer: ExecutionObserver) (event: ExecutionEvent) : unit =
    lock outputLock (fun () ->
        observer.OnNext event
    )

let executeProgramsReactive (system: ActorSystem) (filePaths: string list) (observer: ExecutionObserver) : Async<unit> =
    async {
        let adapter = CliAdapter system
        let tasks =
            filePaths
            |> List.map (fun filePath ->
                async {
                    let! result = (adapter :> IExecutionMode).ExecuteFile filePath
                    match result with
                    | Ok programResult ->
                        safeWrite observer (FileCompleted (filePath, programResult))
                    | Error err ->
                        safeWrite observer (FileError (filePath, err))
                })

        do! Async.Parallel tasks |> Async.Ignore
        safeWrite observer AllComplete
    }