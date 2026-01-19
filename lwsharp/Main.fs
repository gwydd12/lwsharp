module lwsharp.Main

open System
open System.IO
open Akka.Actor
open lwsharp.CliRunner

let printUsage () =
    printfn "Usage: lwsharp <file1> [file2] [file3] ..."
    printfn "Execute one or more lwsharp programs concurrently"
    printfn "\nExample: lwsharp program1.lw program2.lw program3.lw"

[<EntryPoint>]
let main argv =
    if argv.Length = 0 then
        printUsage()
        1
    else
        let filePaths = Array.toList argv
        
        let invalidFiles = filePaths |> List.filter (fun f -> not (File.Exists f))
        if not (List.isEmpty invalidFiles) then
            invalidFiles
            |> List.iter (printfn "Error: File not found: %s")
            1
        else
            let system = ActorSystem.Create("lwsharp")
            
            try
                printfn "\n╔════════════════════════════════════════╗"
                printfn "║     lwsharper - LOOP WHILE Interpreter ║"
                printfn "╚════════════════════════════════════════╝\n"
                let results =
                    executeProgramsParallel system filePaths
                    |> Async.RunSynchronously
                
                let allSucceeded = results |> List.forall _.Success
                system.Terminate().Wait()
                if allSucceeded then 0 else 1
            with ex ->
                printfn $"Fatal error: %s{ex.Message}"
                system.Terminate().Wait()
                1