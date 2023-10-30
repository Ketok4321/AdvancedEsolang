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

let methodMapExprs (mapping: Expression -> Expression) (method: Method) =
    match method.body with
    | Some stmts -> Method(method.name, method.parameters, Some (stmts |> List.map (stmtMapExprs mapping)))
    | None -> method

let classMapExprs (mapping: Expression -> Expression) (_class: Class) =
    {
        _class with ownMembers = _class.ownMembers |> List.map (function :? Method as m -> methodMapExprs mapping m | f -> f)
    }