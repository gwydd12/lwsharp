module lwsharp.InterpreterService

open lwsharp.InterpreterEffect

type InterpreterServices =
    { EvalStmt : Syntax.Stmt -> Computation<unit>
      EvalExpr : Syntax.Expr -> Computation<int> }

