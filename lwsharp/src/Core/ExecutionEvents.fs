module lwsharp.ExecutionEvents

open lwsharp.Pipeline

type ExecutionEvent =
    | FileCompleted of filePath: string * result: ProgramResult
    | FileError of filePath: string * error: PipelineError
    | AllComplete

type ExecutionObserver =
    {
        OnNext: ExecutionEvent -> unit
        OnError: exn -> unit
        OnCompleted: unit -> unit
    }