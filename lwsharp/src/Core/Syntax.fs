module lwsharp.Syntax

type Var = string
type Value = int

type Expr =
    | Const of Value
    | Var of Var
    | Add of Expr * Expr
    | Sub of Expr * Expr
    | Mul of Expr * Expr
    | Div of Expr * Expr
    
type Stmt =
    | Skip
    | Assign of Var * Expr
    | Seq of Stmt list
    | Loop of Expr * Stmt
    | While of Expr * Stmt
        
       

