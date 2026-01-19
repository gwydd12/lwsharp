module lwsharp.ProgramExecutor

open System.IO
open lwsharp.Ports
open lwsharp.Parser
open lwsharp.Syntax
open lwsharp.Interpreter
open lwsharp.InterpreterEffect
open lwsharp.Errors
open lwsharp.StoreActor

// Pure function: Read file
let readFile (filePath: string) : Result<string, string> =
    try
        Ok (File.ReadAllText(filePath))
    with ex ->
        Error ex.Message

// Pure function: Parse source code
let parseSource (source: string) : Result<Stmt, string> =
    match parseProgram source with
    | Error err -> Error err
    | Ok ast -> Ok ast

// Pure function: Execute AST (requires storeActor passed in)
let executeAst (storeActor: Akka.Actor.IActorRef) (ast: Stmt) : Async<Result<State.Store, RuntimeError>> =
    async {
        let (Computation comp) = evalStmt ast
        let! evalResult = comp storeActor
        match evalResult with
        | Ok () ->
            let! finalStore = getState storeActor
            return Ok finalStore
        | Error err ->
            return Error err
    }

// Pure function: Orchestrate program execution (composition of pure functions)
let executeProgramFile (storeActor: Akka.Actor.IActorRef) (filePath: string) : Async<ProgramResult> =
    async {
        match readFile filePath with
        | Error readErr ->
            return { FilePath = filePath; Success = false; Store = Map.empty; Error = Some (UndefinedVariable readErr) }
        | Ok source ->
            match parseSource source with
            | Error parseErr ->
                return { FilePath = filePath; Success = false; Store = Map.empty; Error = Some (UndefinedVariable parseErr) }
            | Ok ast ->
                let! execResult = executeAst storeActor ast
                match execResult with
                | Ok store ->
                    return { FilePath = filePath; Success = true; Store = store; Error = None }
                | Error err ->
                    return { FilePath = filePath; Success = false; Store = Map.empty; Error = Some err }
    }