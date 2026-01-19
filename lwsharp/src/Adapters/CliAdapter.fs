module lwsharp.Adapters.CliAdapter

open Akka.Actor
open lwsharp.Parser
open lwsharp.ProgramExecutor
open lwsharp.Ports
open lwsharp.StoreActor
open lwsharp.Errors

type CliAdapter(system: ActorSystem) =
    interface IExecutionMode with
        member _.ExecuteFile filePath =
            let storeActor = createStoreActor system
            executeProgramFile storeActor filePath
        
        member _.ExecuteStatement source =
            let storeActor = createStoreActor system
            async {
                match parseProgram source with
                | Error err -> return Error (UndefinedVariable err)
                | Ok ast -> return! executeAst storeActor ast
            }