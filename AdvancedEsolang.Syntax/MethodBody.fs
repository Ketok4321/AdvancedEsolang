namespace AdvancedEsolang.Syntax

type Expression =
    | Get of name: string
    | GetF of objExpr: Expression * fieldName: string
    | CallExpr of objExpr: Expression * methodName: string * args: Expression list
    | Is of objExpr: Expression * className: string
    | Equals of objExpr1: Expression * objExpr2: Expression
    | String of string

type Statement =
    | SetV of varName: string * value: Expression
    | SetF of objExpr: Expression * fieldName: string * value: Expression
    | CallStmt of objExpr: Expression * methodName: string * args: Expression list
    | Return of res: Expression
    | If of cond: Expression * stmts: Statement list
    | While of cond: Expression * stmts: Statement list
