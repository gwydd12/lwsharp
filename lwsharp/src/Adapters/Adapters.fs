module lwsharp.Adapters

open System.IO
open FParsec
open lwsharp.Core.Syntax
open lwsharp.Ports


type FileReader() =
    interface IFileReader with
        member _.ReadFile filePath =
            try
                if not (File.Exists filePath) then
                    Result.Error $"File not found: {filePath}"
                else
                    let content = File.ReadAllText filePath
                    if System.String.IsNullOrWhiteSpace content then
                        Result.Error "File is empty"
                    else
                        Result.Ok content
            with ex ->
                Result.Error ex.Message

type Parser() =
    let ws = skipMany (anyOf " \t")
    let ws_nl = skipMany (anyOf " \t\r\n")

    let expr, exprRef = createParserForwardedToRef<Expr, unit>()
    let stmt, stmtRef = createParserForwardedToRef<Stmt, unit>()

    let identifier : Parser<string, unit> =
        many1Satisfy2L System.Char.IsLetter System.Char.IsLetterOrDigit "variable" .>> ws

    let integer : Parser<int, unit> = pint32 .>> ws

    let lexeme p = p .>> ws

    let kw s = lexeme (pstring s)
    let op s = (pstring s .>> ws) >>% ()

    let exprTerm : Parser<Expr, unit> =
        choice [
            integer |>> Const
            identifier |>> Var
            kw "(" >>. expr .>> kw ")"
        ]

    let parseExpr () : Parser<Expr, unit> =
        let term = exprTerm
        let addOp = op "+" >>% (fun x y -> Add(x, y))
        let subOp = op "-" >>% (fun x y -> Sub(x, y))
        let mulOp = op "*" >>% (fun x y -> Mul(x, y))
        let divOp = op "/" >>% (fun x y -> Div(x, y))
        let binOp = choice [addOp; subOp; mulOp; divOp]
        
        chainl1 term binOp

    do exprRef.Value <- parseExpr ()

    let assignStmt : Parser<Stmt, unit> =
        pipe2 
            (identifier .>> kw ":=") 
            expr 
            (fun v e -> Assign(v, e))

    let skipStmt : Parser<Stmt, unit> =
        kw "skip" >>% Skip

    let primitiveStmt : Parser<Stmt, unit> =
        choice [
            attempt skipStmt
            attempt assignStmt
        ]

    let loopStmt : Parser<Stmt, unit> =
        pipe2
            (kw "LOOP" >>. identifier .>> kw "DO")
            (stmt .>> kw "END")
            (fun var body -> Loop(Var var, body))

    let whileStmt : Parser<Stmt, unit> =
        pipe2
            (kw "WHILE" >>. identifier .>> kw "DO")
            (stmt .>> kw "END")
            (fun var body -> While(Var var, body))

    let controlStmt : Parser<Stmt, unit> =
        choice [
            loopStmt
            whileStmt
        ]

    let singleStmt : Parser<Stmt, unit> =
        choice [
            controlStmt
            attempt primitiveStmt
        ]

    let separator : Parser<unit, unit> =
        skipMany1 (choice [
            pchar ';'
            anyOf "\r\n"
        ]) .>> ws_nl

    let statementSeq : Parser<Stmt, unit> =
        ws_nl >>. sepEndBy1 singleStmt separator
        |>> fun stmts ->
            match stmts with
            | [s] -> s
            | stmts -> Seq stmts

    do stmtRef.Value <- statementSeq

    let program : Parser<Stmt, unit> =
        ws_nl >>. statementSeq .>> ws_nl .>> eof

    interface IParser with
        member _.Parse input =
            match run program input with
            | Success(ast, _, _) -> Result.Ok ast
            | Failure(err, _, _) -> Result.Error err


type ConsoleReporter() =
    interface IResultReporter with
        member _.ReportSuccess (filePath, store) =
            printfn $"SUCCESS - {filePath}"
            if Map.isEmpty store then
                printfn "  (No variables)"
            else
                store |> Map.fold (fun _ k v -> printfn $"{k} = {v}") ()
            printfn ""

        member _.ReportError (filePath, error) =
            printfn $"FAILED - {filePath}"
            printfn $"  Error: {error}"
            printfn ""