module lwsharp.Main

open lwsharp.Interpreter
open lwsharp.InterpreterEffect
open lwsharp.StoreActor
open lwsharp.Parser

// More complex example demonstrating loop parsing
let loopProgramExample () =
    let programSource = """
        count := 1;
        LOOP count DO
            count := count + 1
        END
    """
    
    match parseProgram programSource with
    | Error err ->
        printfn $"Parse error: %s{err}"
        Map.empty
        
    | Ok ast ->
        let storeActor = createStoreActor ()
        
        let result =
            async {
                let (Computation comp) = evalStmt ast
                let! evalResult = comp storeActor
                
                match evalResult with
                | Ok () ->
                    let! finalStore = getState storeActor
                    printfn "✓ Loop program completed!"
                    finalStore |> Map.iter (printfn "  %s = %d")
                    return finalStore
                    
                | Error err ->
                    printfn $"Error: %A{err}"
                    return Map.empty
            } |> Async.RunSynchronously
        
        result

// While loop example
let whileProgramExample () =
    let programSource = """
        x := 10;
x2 := 100;
        WHILE x DO
            x2 := x2 - 1;
x := x - 1
        END
    """
    
    match parseProgram programSource with
    | Error err ->
        printfn $"Parse error: %s{err}"
        Map.empty
        
    | Ok ast ->
        let storeActor = createStoreActor ()
        
        let result =
            async {
                let (Computation comp) = evalStmt ast
                let! evalResult = comp storeActor
                
                match evalResult with
                | Ok () ->
                    let! finalStore = getState storeActor
                    printfn "✓ While program completed!"
                    finalStore |> Map.iter (printfn "  %s = %d")
                    return finalStore
                    
                | Error err ->
                    printfn $"Error: %A{err}"
                    return Map.empty
            } |> Async.RunSynchronously
        
        result

[<EntryPoint>]
let main argv =
    printfn "=== Basic Program Example ==="
    // Example program as source code
    let programSource = """
        x := 5;
        y := x + 3;
        z := y * 2
    """

    // Parse the program
    match parseProgram programSource with
    | Error parseErr ->
        printfn $"Parse error: %s{parseErr}"
        1

    | Ok ast ->
        let storeActor = createStoreActor ()

        let result =
            async {
                let (Computation comp) = evalStmt ast
                let! evalResult = comp storeActor

                match evalResult with
                | Ok () ->
                    // Get the final state
                    let! finalStore = getState storeActor
                    printfn "✓ Program executed successfully!"
                    printfn "\nFinal variable store:"
                    finalStore
                    |> Map.iter (printfn "  %s = %d")
                    return 0

                | Error err ->
                    printfn $"✗ Runtime error: %A{err}"
                    return 1
            } |> Async.RunSynchronously

        if result = 0 then
            printfn "\n=== Loop Program Example ==="
            let _ = loopProgramExample ()
            
            printfn "\n=== While Program Example ==="
            let _ = whileProgramExample ()
            
            0
        else
            result

