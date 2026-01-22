module lwsharp.Core.Errors

type RuntimeError =
    | DivisionByZero
    | UndefinedVariable of string
    | NegativeLoopBound
