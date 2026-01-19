module lwsharp.Ports

open lwsharp.Errors
open lwsharp.Syntax

type ProgramResult =
    { FilePath: string
      Success: bool
      Store: State.Store
      Error: RuntimeError option }

type IFileReader =
    abstract ReadFile : filePath: string -> Result<string, string>

type IStoreManager =
    abstract GetState : unit -> Async<State.Store>
    abstract SetVariable : name: string -> value: int -> Async<Result<unit, RuntimeError>>
    abstract GetVariable : name: string -> Async<Result<int, RuntimeError>>

type ILogger =
    abstract LogResult : result: ProgramResult -> Async<unit>
    abstract LogOutput : message: string -> Async<unit>

type IProgramExecutor =
    abstract Execute : ast: Stmt -> Async<Result<State.Store, RuntimeError>>

type IExecutionMode =
    abstract ExecuteFile : filePath: string -> Async<ProgramResult>
    abstract ExecuteStatement : source: string -> Async<Result<State.Store, RuntimeError>>