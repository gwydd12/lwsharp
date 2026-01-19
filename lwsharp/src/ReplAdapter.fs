module lwsharp.Adapters.ReplAdapter

open Akka.Actor
open lwsharp.Errors
open lwsharp.Parser
open lwsharp.ProgramExecutor
open lwsharp.Ports

type ReplAdapter(storeActor: IActorRef) =
    interface IExecutionMode with
        member _.ExecuteFile filePath =
            async {
                return! executeProgramFile storeActor filePath
            }
        
        member _.ExecuteStatement source =
            async {
                match parseProgram source with
                | Error err -> return Error (UndefinedVariable err)
                | Ok ast -> return! executeAst storeActor ast
            }
