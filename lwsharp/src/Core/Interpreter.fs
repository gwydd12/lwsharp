module lwsharp.Core.Interpreter

open lwsharp.Core.Syntax
open lwsharp.Core.Effect
open lwsharp.Core.Errors

(*I had to write a Monoid in my project so here we go. Probably we have other ^^*)
let add a b = a + b

(**
Nesting problem without bind :(
| Add (a, b) ->
    fun store ->
        match evalExpr a store with
        | Ok (x, store1) ->
            match evalExpr b store1 with
            | Ok (y, store2) ->
                Ok (add x y, store2)
            | Error err -> Error err
        | Error err -> Error err
*)
let rec evalExpr (expr: Expr) : Computation<int> =
    match expr with
    | Const v -> returnValue v
    | Var x -> readVar x
    | Add (a, b) ->
        evalExpr a >>= fun x ->
        evalExpr b >>= fun y ->
        returnValue (add x y)
    | Sub (a, b) ->
        evalExpr a >>= fun x ->
        evalExpr b >>= fun y ->
        returnValue (x - y)
    | Mul (a, b) ->
        evalExpr a >>= fun x ->
        evalExpr b >>= fun y ->
        returnValue (x * y)
    | Div (a, b) ->
        evalExpr a >>= fun x ->
        evalExpr b >>= fun y ->
        if y = 0 then fun _ -> Error DivisionByZero
        else returnValue (x / y)

// Evaluate statements
let rec evalStmt (stmt: Stmt) : Computation<unit> =
    match stmt with
    | Skip -> returnValue ()
    | Assign (x, e) ->
        evalExpr e >>= writeVar x
    | Seq stmts ->
        stmts |> List.fold (fun acc s -> acc >>= fun () -> evalStmt s) (returnValue ())
    | Loop (e, body) ->
        evalExpr e >>= fun n ->
        if n < 0 then fun _ -> Error NegativeLoopBound
        else
            let rec loop i =
                if i = 0 then returnValue ()
                else evalStmt body >>= fun () -> loop (i - 1)
            loop n
    | While (cond, body) ->
        let rec whileLoop () =
            evalExpr cond >>= fun v ->
            if v = 0 then returnValue ()
            else evalStmt body >>= fun () -> whileLoop ()
        whileLoop ()
