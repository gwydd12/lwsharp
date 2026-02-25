module lwsharp.Main

open System.IO
open Akka.Actor
open Akka.FSharp
open lwsharp.Adapters
open lwsharp.Actors
open lwsharp.Ports

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
        
        let fileReader = FileReader() :> IFileReader // Type casting to interface
        let parser = Parser() :> IParser
        let reporter = ConsoleReporter() :> IResultReporter
        
        match argv |> Array.toList with
            | ["repl"] ->
                spawn system "repl" (createReplActor parser) |> ignore
                system.WhenTerminated.Wait()
                0
            | "parallel" :: filePaths ->
               let invalidFiles = filePaths |> List.filter (fun f -> not (File.Exists f))
               if not (List.isEmpty invalidFiles) then
                invalidFiles |> List.iter (printfn "Error: File not found: %s")
                system.Terminate() |> ignore
                1
               else
                let coordinator = createCoordinator system fileReader parser reporter
                coordinator <! StartFiles filePaths
                system.WhenTerminated.Wait()
                0
            | _ ->
                printf "Usage: lwsharp [repl | parallel <file1> <file2> ...]\n"
                1
    with ex ->
        printfn $"Unexpected error: %s{ex.Message}"
        system.Terminate() |> ignore
        1