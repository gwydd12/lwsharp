module lwsharp.Pipeline

open System.IO
open Akka.Actor
open lwsharp.InterpreterEffect
open lwsharp.Parser
open lwsharp.Interpreter
open lwsharp.Errors
open lwsharp.StoreActor
open lwsharp.Syntax

// ============================================================================
// ERROR TYPES AT EACH PIPELINE STAGE
// ============================================================================


type ParsePosition =
    { Line: int64
      Column: int64 }

type ParseError =
    | EmptyProgram
    | SyntaxError of string

type ExecutionError =
    | RuntimeFailure of message: string
    | DivisionByZero
    | StackOverflowError

type PipelineError =
    | FileError of path: string * message: string
    | Parse of ParseError
    | Runtime of ExecutionError
    
type ProgramResult =
    { FilePath: string
      Success: bool
      Store: State.Store
      Error: PipelineError option }

let readFile (filePath: string) : Result<string, PipelineError> =
    try
        if not (File.Exists filePath) then
            Error (FileError (filePath, $"File not found: {filePath}"))
        else
            let content = File.ReadAllText filePath
            if System.String.IsNullOrWhiteSpace content then
                Error (FileError (filePath, "File is empty"))
            else
                Ok content
    with ex ->
        Error (FileError (filePath, ex.Message))


let parse (source: string) : Result<Stmt, PipelineError> =
    if System.String.IsNullOrWhiteSpace source then
        Error (Parse EmptyProgram)
    else
        match parseProgram source with
        | Ok ast -> Ok ast
        | Error err -> Error (Parse (SyntaxError err))

// ============================================================================
// STAGE 3: EXECUTION
// ============================================================================

let execute (storeActor: IActorRef) (ast: Stmt) 
    : Async<Result<State.Store, PipelineError>> =
    async {
        try
            let (Computation comp) = evalStmt ast
            let! evalResult = comp storeActor
            match evalResult with
            | Ok () ->
                let! finalStore = getState storeActor
                return Ok finalStore
            | Error err ->
                return Error (Runtime (RuntimeFailure (sprintf "%A" err)))
        with
        | ex ->
            return Error (Runtime (RuntimeFailure ex.Message))
    }

let bind (f: 'a -> Result<'b, 'e>) (input: Result<'a, 'e>) : Result<'b, 'e> =
    match input with
    | Ok x -> f x
    | Error e -> Error e

let map (f: 'a -> 'b) (input: Result<'a, 'e>) : Result<'b, 'e> =
    match input with
    | Ok x -> Ok (f x)
    | Error e -> Error e


let asyncBind (f: 'a -> Async<Result<'b, 'e>>) (input: Result<'a, 'e>) : Async<Result<'b, 'e>> =
    match input with
    | Ok x -> f x
    | Error e -> async { return Error e }

let executeProgramFile (storeActor: IActorRef) (filePath: string)
    : Async<Result<ProgramResult, PipelineError>> =
    async {
        let! result =
            readFile filePath
            |> bind parse
            |> asyncBind (execute storeActor)
        return
            match result with
            | Ok store -> Ok { FilePath = filePath; Success = true; Store = store; Error = None }
            | Error err -> Error err
    }