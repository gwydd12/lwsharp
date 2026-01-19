module lwsharp.Errors

type RuntimeError =
    | UndefinedVariable of string
    | NegativeLoopBound
    | NonTerminatingWhile