module lwsharp.Adapters.ReplAdapter

open Akka.Actor
open lwsharp.Parser
open lwsharp.Pipeline
open lwsharp.Ports

type ReplAdapter(storeActor: IActorRef) =
    interface IExecutionMode with
        member _.ExecuteFile filePath =
            async {
                let! result = executeProgramFile storeActor filePath
                return result
            }
        
        member _.ExecuteStatement source =
            async {
                match parseProgram source with
                | Error err -> return Error (Parse (SyntaxError err))
                | Ok ast ->
                    let! result = execute storeActor ast
                    return result
            }
