// Interpreter.fs
module lwsharp.Interpreter

open lwsharp.Syntax
open lwsharp.InterpreterEffect
open lwsharp.Errors

let rec evalExpr (expr: Expr) : Computation<int> =
    match expr with
    | Const v -> 
        returnValue v
        
    | Var x ->
        readVarComp x
        
    | Add (a, b) ->
        bind (fun x ->
        bind (fun y ->
            returnValue (x + y)) (evalExpr b)) (evalExpr a)
            
    | Sub (a, b) ->
        bind (fun x ->
        bind (fun y ->
            returnValue (x - y)) (evalExpr b)) (evalExpr a)
            
    | Mul (a, b) ->
        bind (fun x ->
        bind (fun y ->
            returnValue (x * y)) (evalExpr b)) (evalExpr a)
            
    | Div (a, b) ->
        bind (fun x ->
        bind (fun y ->
            returnValue (x / y)) (evalExpr b)) (evalExpr a)

let rec evalStmt (stmt: Stmt) : Computation<unit> =
    match stmt with
    | Skip -> 
        returnValue ()
        
    | Assign (x, e) ->
        bind (writeVarComp x) (evalExpr e)
            
    | Seq stmts ->
        List.fold (fun acc s ->
            bind (fun () -> evalStmt s) acc
        ) (returnValue ()) stmts
        
    | Loop (e, body) ->
        bind (fun n ->
            if n < 0 then
                Computation (fun _ -> async { return Error NegativeLoopBound })
            else
                let rec loop i =
                    if i = 0 then returnValue ()
                    else bind (fun () -> loop (i-1)) (evalStmt body)
                loop n) (evalExpr e)
                
    | While (cond, body) ->
        let rec loop () =
            bind (fun v ->
                if v = 0 then returnValue ()
                else bind (fun () -> loop ()) (evalStmt body)
            ) (evalExpr cond)
        loop ()