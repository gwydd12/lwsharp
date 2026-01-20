module lwsharp.Errors

type RuntimeError =
    | DivisionByZero
    | UndefinedVariable of string
    | NegativeLoopBound
    | NonTerminatingWhile
    | ParseError of string