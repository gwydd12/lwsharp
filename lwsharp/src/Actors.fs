module lwsharp.Actors

open Akka.Actor
open Akka.FSharp
open lwsharp.Core.State
open lwsharp.Core.Interpreter
open lwsharp.Core.Effect
open lwsharp.Pipeline
open lwsharp.Ports

type FileExecutorMessage =
    | ExecuteFile of filePath: string

type CoordinatorMessage =
    | StartFiles of filePaths: string list
    | FileComplete

let createFileExecutor (fileReader: IFileReader) (parser: IParser) (reporter: IResultReporter) =
    fun (mailbox: Actor<FileExecutorMessage>) ->
        let rec loop () = actor {
            let! msg = mailbox.Receive()
            match msg with
            | ExecuteFile filePath ->
                let result = executeProgramFile fileReader parser filePath

                match result with
                | Ok programResult ->
                    reporter.ReportSuccess (programResult.FilePath, programResult.Store)
                | Error err ->
                    reporter.ReportError (filePath, err)

                mailbox.Context.Parent <! FileComplete
                return! loop ()
        }
        loop ()

        
let createCoordinator (system: ActorSystem) (fileReader: IFileReader) (parser: IParser) (reporter: IResultReporter) =
    spawn system "coordinator" (fun mailbox ->
        let rec loop (pending: int) =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | StartFiles filePaths ->
                    let count = List.length filePaths
                    filePaths
                    |> List.iteri (fun i filePath ->
                        let executor = spawn mailbox.Context $"executor-{i}" (createFileExecutor fileReader parser reporter)
                        executor <! ExecuteFile filePath
                    )
                    return! loop count

                | FileComplete ->
                    let remaining = pending - 1
                    if remaining = 0 then
                        printfn "Done"
                        system.Terminate() |> ignore
                    return! loop remaining
            }
        loop 0
    )

let createReplActor (parser: IParser)=
    fun (mailbox: Actor<string>) ->
        let rec readLoop (store: Store) = actor {
            printf "> "
            let input = System.Console.ReadLine()
            if input = "exit" then
                mailbox.Context.System.Terminate() |> ignore
                return ()
            else
                return! executeLoop store input
        }
        and executeLoop (store: Store) (input: string) = actor {
            match parser.Parse input with
            | Error err ->
                printfn $"Parse Error: %s{err}"
                return! readLoop store
            | Ok ast ->
                let computation = evalStmt ast
                match run computation store with
                | Error err ->
                    printfn $"Runtime Error: %A{err}"
                    return! readLoop store
                | Ok (_, newStore) ->
                    printfn $"Store: %A{newStore}"
                    return! readLoop newStore
        }
        readLoop Map.empty