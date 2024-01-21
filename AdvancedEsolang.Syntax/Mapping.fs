module AdvancedEsolang.Syntax.Mapping

let rec exprMapExprs (mapping: Expression -> Expression) (expr: Expression) =
    mapping <| match expr with
                | Get _ | String _ -> expr
                | GetF (objExpr, fieldName) -> GetF (exprMapExprs mapping objExpr, fieldName)
                | CallExpr (objExpr, methodName, args) -> CallExpr(exprMapExprs mapping objExpr, methodName, args |> List.map (exprMapExprs mapping))
                | Is (objExpr, className) -> Is (exprMapExprs mapping objExpr, className)
                | Equals (objExpr1, objExpr2) -> Equals (exprMapExprs mapping objExpr1, exprMapExprs mapping objExpr2)

let rec stmtMapExprs (mapping: Expression -> Expression) (stmt: Statement) =
    match stmt with
    | SetV (varName, value) -> SetV (varName, exprMapExprs mapping value)
    | SetF (objExpr, fieldName, value) -> SetF (exprMapExprs mapping objExpr, fieldName, exprMapExprs mapping value)
    | CallStmt (objExpr, methodName, args) -> CallStmt (exprMapExprs mapping objExpr, methodName, args |> List.map (exprMapExprs mapping))
    | Return result -> Return (exprMapExprs mapping result)
    | If (condition, stmts) -> If (exprMapExprs mapping condition, stmts |> List.map (stmtMapExprs mapping))
    | While (condition, stmts) -> While (exprMapExprs mapping condition, stmts |> List.map (stmtMapExprs mapping))
    | Eval expr -> Eval (exprMapExprs mapping expr)

let rec stmtMapStmts (mapping: Statement -> Statement) (stmt: Statement) =
    match stmt with
    | If (condition, stmts) -> mapping <| If (condition, stmts |> List.map (stmtMapStmts mapping))
    | While (condition, stmts) -> mapping <| While (condition, stmts |> List.map (stmtMapStmts mapping))
    | _ -> mapping stmt

let methodMapStmts (mapping: Statement -> Statement) (method: Method) =
    match method.body with
    | Some stmts -> Method(method.name, method.parameters, Some (stmts |> List.map (stmtMapStmts mapping)))
    | None -> method

let methodMapExprs (mapping: Expression -> Expression) (method: Method) =
    match method.body with
    | Some stmts -> Method(method.name, method.parameters, Some (stmts |> List.map (stmtMapExprs mapping)))
    | None -> method

let classMapStmts (mapping: Statement -> Statement) (_class: Class) =
    {
        _class with ownMembers = _class.ownMembers |> List.map (function :? Method as m -> methodMapStmts mapping m | f -> f)
    }

let classMapExprs = stmtMapExprs >> classMapStmts
