namespace AdvancedEsolang.Syntax

type Expression =
    | Get of name: string
    | GetF of objExpr: Expression * fieldName: string
    | CallExpr of objExpr: Expression * methodName: string * args: Expression list
    | Is of objExpr: Expression * className: string
    | Equals of objExpr1: Expression * objExpr2: Expression
    | String of text: string

type Statement =
    | SetV of varName: string * value: Expression
    | SetF of objExpr: Expression * fieldName: string * value: Expression
    | CallStmt of objExpr: Expression * methodName: string * args: Expression list
    | Return of result: Expression
    | If of condition: Expression * stmts: Statement list
    | While of condition: Expression * stmts: Statement list
    | Eval of expr: Expression
