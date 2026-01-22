module lwsharp.Pipeline

open lwsharp.Core.Syntax
open lwsharp.Core.State
open lwsharp.Core.Effect
open lwsharp.Core.Interpreter
open lwsharp.Ports

type ProgramResult =
    {
        FilePath: string
        Success: bool
        Store: Store
        Error: string option
    }

let (>>=) result binder =
    match result with
    | Ok value -> binder value
    | Error err -> Error err

let readFile (fileReader: IFileReader) (filePath: string) =
    fileReader.ReadFile filePath
    |> Result.mapError (fun err -> $"File error: {err}")

let parseProgram (parser: IParser) source =
    parser.Parse source
    |> Result.mapError (fun err -> $"Parse error: {err}")

let executeProgram (ast: Stmt) : Result<Store, string> =
    let computation = evalStmt ast
    match run computation Map.empty with
    | Ok ((), store) -> Ok store
    | Error err -> Error $"Runtime error: {err}"

let executeProgramFile (fileReader: IFileReader) (parser: IParser) (filePath: string) : Result<ProgramResult, string> =
    readFile fileReader filePath
    >>= fun source ->
        parseProgram parser source
        >>= fun ast ->
            executeProgram ast
            |> Result.map (fun store -> 
                { FilePath = filePath
                  Success = true
                  Store = store
                  Error = None })