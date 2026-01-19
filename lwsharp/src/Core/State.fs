module lwsharp.State

type Store = Map<Syntax.Var, Syntax.Value>
let emptyStore : Store = Map.empty