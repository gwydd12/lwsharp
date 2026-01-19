module lwsharp.Parser

open FParsec
open Syntax
open System

// Whitespace
let ws = skipMany (anyOf " \t")
let ws_nl = skipMany (anyOf " \t\r\n")

// Forward declarations for mutual recursion
let expr, exprRef = createParserForwardedToRef<Expr, unit>()
let stmt, stmtRef = createParserForwardedToRef<Stmt, unit>()

// Identifier: letter followed by letters/digits
let identifier : Parser<string, unit> =
    many1Satisfy2L Char.IsLetter Char.IsLetterOrDigit "variable" .>> ws

// Integer constant
let integer : Parser<int, unit> = pint32 .>> ws

// Lexeme: parse p and skip trailing whitespace
let lexeme p = p .>> ws

// Keywords and operators
let kw s = lexeme (pstring s)
let op s = (pstring s .>> ws) >>% ()

// Basic expression term (variable or integer)
let exprTerm : Parser<Expr, unit> =
    choice [
        integer |>> Const
        identifier |>> Var
        kw "(" >>. expr .>> kw ")"
    ]

// Expression: parse terms with binary operators left-associatively
let parseExpr () : Parser<Expr, unit> =
    let term = exprTerm
    let addOp = op "+" >>% (fun x y -> Add(x, y))
    let subOp = op "-" >>% (fun x y -> Sub(x, y))
    let mulOp = op "*" >>% (fun x y -> Mul(x, y))
    let divOp = op "/" >>% (fun x y -> Div(x, y))
    let binOp = choice [addOp; subOp; mulOp; divOp]
    
    chainl1 term binOp

do exprRef.Value <- parseExpr ()

// Assignment: x := expr
let assignStmt : Parser<Stmt, unit> =
    pipe2 
        (identifier .>> kw ":=") 
        expr 
        (fun v e -> Assign(v, e))

// Skip statement
let skipStmt : Parser<Stmt, unit> =
    kw "skip" >>% Skip

// Primitive statements (no control flow)
let primitiveStmt : Parser<Stmt, unit> =
    choice [
        attempt skipStmt
        attempt assignStmt
    ]

// LOOP statement
let loopStmt : Parser<Stmt, unit> =
    pipe2
        (kw "LOOP" >>. identifier .>> kw "DO")
        (stmt .>> kw "END")
        (fun var body -> Loop(Var var, body))

// WHILE statement
let whileStmt : Parser<Stmt, unit> =
    pipe2
        (kw "WHILE" >>. identifier .>> kw "DO")
        (stmt .>> kw "END")
        (fun var body -> While(Var var, body))

// Control flow statements
let controlStmt : Parser<Stmt, unit> =
    choice [
        loopStmt
        whileStmt
    ]

// Any single statement
let singleStmt : Parser<Stmt, unit> =
    choice [
        controlStmt
        attempt primitiveStmt
    ]

// Statement separator: semicolon and/or newline
let separator : Parser<unit, unit> =
    skipMany1 (choice [
        pchar ';'
        anyOf "\r\n"
    ]) .>> ws_nl

// Statement sequence separated by semicolons and/or newlines
let statementSeq : Parser<Stmt, unit> =
    ws_nl >>. sepEndBy1 singleStmt separator
    |>> fun stmts ->
        match stmts with
        | [s] -> s
        | stmts -> Seq stmts

do stmtRef.Value <- statementSeq

// Program: optional whitespace, statements, optional whitespace, EOF
let program : Parser<Stmt, unit> =
    ws_nl >>. statementSeq .>> ws_nl .>> eof

let parseProgram (input: string) : Result<Stmt, string> =
    match run program input with
    | Success(ast, _, _) -> Result.Ok ast
    | Failure(err, _, _) -> Result.Error err