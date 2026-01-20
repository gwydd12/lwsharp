module lwsharp.Adapters.CliAdapter

open Akka.Actor
open lwsharp.Parser
open lwsharp.Ports
open lwsharp.StoreActor
open lwsharp.Pipeline

type CliAdapter(system: ActorSystem) =
    interface IExecutionMode with
        member _.ExecuteFile filePath =
            let storeActor = createStoreActor system
            async {
                let! result = executeProgramFile storeActor filePath
                return result
            }
            
        member _.ExecuteStatement source =
            let storeActor = createStoreActor system
            async {
                match parseProgram source with
                | Error err -> return Error (Parse (SyntaxError err))
                | Ok ast ->
                    let! result = execute storeActor ast
                    return result
            }