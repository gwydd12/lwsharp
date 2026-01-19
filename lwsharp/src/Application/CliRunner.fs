module lwsharp.CliRunner

open Akka.Actor

open lwsharp.Adapters.CliAdapter
open lwsharp.Ports

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
                    printResult result
                    return result
                })

        let! results = Async.Parallel tasks
        return List.ofArray results
    }