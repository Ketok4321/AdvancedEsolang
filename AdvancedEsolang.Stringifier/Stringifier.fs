module AdvancedEsolang.Stringifier.Stringifier

open AdvancedEsolang.Syntax

let private tab (s: string) = s.Split('\n') |> Array.map ((+) "    ") |> String.concat "\n"
let private list = String.concat ", "

let rec sCall objExpr methodName args =
    $"{sExprPF objExpr}.{methodName}({args |> List.map sExpr |> list})"

and sExpr = function
    | Get name -> name
    | GetF (objExpr, fieldName) -> $"{sExprPF objExpr}.{fieldName}"
    | CallExpr (objExpr, methodName, args) ->
        match methodName, args.Length with
        | "not", 0 ->
            match objExpr with
            | Equals (objExpr1, objExpr2) -> $"{sExprP objExpr1} != {sExprP objExpr2}"
            | _ -> $"!{sExprP objExpr}"
        | "+", 1 -> $"{sExprP objExpr} + {sExprP args[0]}"
        | "-", 1 -> $"{sExprP objExpr} - {sExprP args[0]}"
        | "*", 1 -> $"{sExprP objExpr} * {sExprP args[0]}"
        | "/", 1 -> $"{sExprP objExpr} / {sExprP args[0]}"
        | _ -> sCall objExpr methodName args
    | Is (objExpr, className) -> $"{sExprP objExpr} is {className}"
    | Equals (objExpr1, objExpr2) -> $"{sExprP objExpr1} = {sExprP objExpr2}"
    | String s -> $"\"{s}\""

and sExprP expr = // sExpr but parenthesizes the expression if it's not simple
    match expr with
    | Get _ | String _ -> sExpr expr
    | _ -> $"({sExpr expr})"

and sExprPF expr = // sExprP but doens't parenthesize the expression if it's GetF
    match expr with
    | GetF _ -> sExpr expr
    | _ -> sExprP expr

and sStmt = function
    | SetV (varName, value) -> $"{varName} = {sExpr value}"
    | SetF (objExpr, fieldName, value) -> $"{sExprPF objExpr}.{fieldName} = {sExpr value}"
    | CallStmt (objExpr, methodName, args) -> sCall objExpr methodName args
    | Return expr -> $"return {sExpr expr}"
    | If (cond, stmts) -> $"if {sExpr cond}:\n{sStmts stmts}\nend"
    | While (cond, stmts) -> $"while {sExpr cond}:\n{sStmts stmts}\nend"
    | Eval expr -> $"eval {sExpr expr}"

and sStmts = List.map sStmt >> String.concat "\n" >> tab

and sField (field: Field) = $"field {field.name}"

and sMethod (method: Method) =
    $"method {method.name}({method.parameters |> list})" +
    match method.body with
    | Some stmts -> ":\n" + sStmts stmts + "\nend"
    | None -> ""

and sClass (_class: Class) =
    String.concat "" [
        if _class.isAbstract then "abstract "
        $"class {_class.name}"
        match _class.parent with
        | Some parent -> $" extends {parent.name}:\n"
        | None -> ":\n"

        let fields, methods = _class.ownMembersOfType<Field> (), _class.ownMembersOfType<Method> ()
        
        if fields.Length > 0 then
            fields
                |> List.map sField
                |> String.concat "\n" |> tab
            tab "\n" + "\n"
        
        methods
            |> List.map sMethod
            |> String.concat "\n\n" |> tab
        
        "\nend\n"
    ]

and sLibrary program =
    let deps = program.dependencies |> List.filter (fun d -> d.name <> "@builtin")
    
    (deps |> List.map (fun d -> $"import {d.name}\n") |> String.concat "") +
    if deps.Length > 0 then "\n" else ""
    + (program.classes |> List.map sClass |> String.concat "\n")
