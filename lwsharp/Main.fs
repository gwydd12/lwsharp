module lwsharp.Main

open lwsharp

let code = """
x0 := x0 + 1;
LOOP x1 DO
  x0 := x0 + 1;
  x0 := x0 + 2
END
"""

match Parser.parseProgram code with
| Ok ast ->
    printfn "Parsed AST: %A" ast
    let initialStore = Map.ofList [("x0", 0); ("x1", 5)]
    match InterpreterEffect.run (Interpreter.evalStmt ast) initialStore with
    | Ok (_, finalStore) -> printfn "Program executed successfully. Final store: %A" finalStore
    | Error runtimeErr -> printfn "Runtime error: %A" runtimeErr
| Error err -> printfn "Parse error: %s" err

