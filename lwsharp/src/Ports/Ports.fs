module lwsharp.Ports

open lwsharp.Core.Syntax
open lwsharp.Core.State

type IFileReader =
    abstract ReadFile : string -> Result<string, string>

type IParser =
    abstract Parse : string -> Result<Stmt, string>

type IResultReporter =
    abstract ReportSuccess : filePath: string * store: Store -> unit
    abstract ReportError : filePath: string * error: string -> unit
