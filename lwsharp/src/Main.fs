module lwsharp.Main

open System.IO
open Akka.Actor
open lwsharp.CliRunner
open lwsharp.ReplRunner
open lwsharp.ExecutionEvents

let printUsage () =
    printfn "Usage: lwsharp [file1] [file2] [file3] ..."
    printfn "Execute one or more lwsharp programs concurrently"
    printfn "Or run interactive REPL if no files provided"
    printfn "\nExample: lwsharp program1.lw program2.lw program3.lw"

[<EntryPoint>]
let main argv =
    let system = ActorSystem.Create("lwsharp")
    
    try
        printfn "\u001b[36m_______________\u001b[0m"
        printfn "\u001b[36m|               |\u001b[0m"
        printfn "\u001b[33m|   WHILE!       |\u001b[0m"
        printfn "\u001b[36m|_______________|\u001b[0m"
        printfn "\u001b[36m     \\\u001b[0m"
        printfn "\u001b[36m      \\\u001b[0m"
        printfn "\u001b[32m _______       🌀\u001b[0m"
        printfn "\u001b[32m|       |     ( )\u001b[0m"
        printfn "\u001b[35m|written in F#|-----( )---<\u001b[0m"
        printfn "\u001b[32m|_______|      🌀\u001b[0m"
        printfn "\u001b[32m /     \\\u001b[0m"
        printfn "\u001b[32m/       \\\u001b[0m"
        printfn "\u001b[33mLOOP LOOP LOOP\u001b[0m"
        
        if argv.Length = 0 then
            runRepl system |> Async.RunSynchronously
            system.Terminate().Wait()
            0
        else
            let filePaths = Array.toList argv
            let invalidFiles = filePaths |> List.filter (fun f -> not (File.Exists f))
            
            if not (List.isEmpty invalidFiles) then
                invalidFiles |> List.iter (printfn "Error: File not found: %s")
                1
            else
                let allComplete = new System.Threading.ManualResetEvent(false)

                executeProgramsReactive system filePaths
                |> Observable.subscribe (fun event ->
                    match event with
                    | FileCompleted (_, result) -> printResult result
                    | FileError (path, err) -> printfn $"Error: %s{path} - %A{err}"
                    | AllComplete -> 
                        printfn "Done"
                        allComplete.Set() |> ignore
                )
                |> ignore

                allComplete.WaitOne() |> ignore
                system.Terminate().Wait()
                0
    with ex ->
        printfn $"Fatal error: %s{ex.Message}"
        system.Terminate().Wait()
        1