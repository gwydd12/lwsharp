module lwsharp.CliRunner

open System
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

let outputLock = obj()
let executeProgramsReactive (system: ActorSystem) (filePaths: string list) : IObservable<ExecutionEvent> =
    { new IObservable<ExecutionEvent> with
        member _.Subscribe(observer: IObserver<ExecutionEvent>) : IDisposable =
            let cts = System.Threading.CancellationTokenSource()
            
            Async.Start(
                async {
                    try
                        let adapter = CliAdapter system
                        let tasks =
                            filePaths
                            |> List.map (fun filePath ->
                                async {
                                    let! result = (adapter :> IExecutionMode).ExecuteFile filePath
                                    match result with
                                    | Ok programResult ->
                                        lock outputLock (fun () ->
                                            observer.OnNext(FileCompleted (filePath, programResult))
                                        )
                                    | Error err ->
                                        lock outputLock (fun () ->
                                            observer.OnNext(FileError (filePath, err))
                                        )
                                })

                        do! Async.Parallel tasks |> Async.Ignore
                        lock outputLock (fun () ->
                            observer.OnNext(AllComplete)
                            observer.OnCompleted()
                        )
                    with ex ->
                        observer.OnError(ex)
                },
                cts.Token
            )
            
            { new IDisposable with
                member _.Dispose() = cts.Cancel()
            }
    }