﻿module AdvancedEsolang.Interpreter.Runner

open System.Collections.Generic

open AdvancedEsolang.Interpreter.Errors
open AdvancedEsolang.Interpreter.RunUtil
open AdvancedEsolang.Syntax

let rec runMethod (ctx: RunCtx) (this: Object) (method: Method) (args: Object list) =
    let mutable result = None
    
    match method.body with
        | Some stmts ->
            let vars = Dictionary<string, Object>()
            
            for i, arg in args |> List.indexed do
                let name = method.parameters[i]
                vars[name] <- arg
            
            let rec runExpr = function
                | Get name ->
                    if name = "this" then
                        this
                    else
                        match vars.TryGetValue name with
                        | true, v -> v
                        | false, _ ->
                            match ctx.getClass name with
                            | Some _class -> Object(_class)
                            | None -> errGet name
                | CallExpr (objExpr, methodName, args) ->
                    let obj = runExpr objExpr
                    let method = assertExsists (obj._class.get<Method>(methodName)) (fun () -> errCallUndef obj._class methodName)
                    
                    if args.Length = method.parameters.Length then
                        runMethod ctx obj method (args |> List.map runExpr)
                    else
                        errCallWrongArgC obj._class methodName method args.Length
                | GetF (objExpr, fieldName) ->
                    let obj = runExpr objExpr
                    obj.getField(fieldName)
                | Is (objExpr, className) ->
                    let obj = runExpr objExpr
                    match ctx.getClass className with
                    | Some _class -> obj.is(_class) |> bool
                    | None -> errIs obj className
                | Equals (objExpr1, objExpr2) ->
                    let obj1 = runExpr objExpr1
                    let obj2 = runExpr objExpr2
                    obj1 = obj2 |> bool
                | String string ->
                    StringObj(string)
                       
            let rec runStmt = function
                | SetV (varName, value) ->
                    vars[varName] <- runExpr value
                    false
                | SetF (objExpr, fieldName, value) ->
                    let object = runExpr objExpr
                    object.setField fieldName (runExpr value)
                    false
                | CallStmt (objExpr, methodName, args) ->
                    runExpr (CallExpr (objExpr, methodName, args)) |> ignore
                    false
                | Return res ->
                    result <- Some (runExpr res)
                    true
                | If (cond, stmts) ->
                    if (runExpr cond).is(BuiltinTypes.True) then
                        runStmts stmts
                    else
                        false
                | While (cond, stmts) ->
                    if (runExpr cond).is(BuiltinTypes.True) then
                        if runStmts stmts then
                            true
                        else
                            runStmt (While (cond, stmts))
                    else
                        false

            and runStmts: Statement list -> bool = function
                | [] -> false
                | stmt :: tail ->
                    if runStmt stmt then
                        true
                    else
                        runStmts tail
            
            runStmts stmts |> ignore
                    
        | None ->
            match BuiltinMethods.get method with
            | true, mtd -> result <- Some (mtd (ctx, this, args))
            | false, _ -> errCallEmpty this._class method.name
    
    match result with
    | Some r -> r
    | None -> Object.Null ()

let run (programLib: Library) =
    let programClass = programLib.classes |> List.tryFind (fun _class -> _class.is BuiltinTypes.Program && not _class.isAbstract)
    match programClass with
    | Some programC ->
        let programObj = Object(programC)
        
        let mainMethod = programC.get<Method>("main")
        
        let ctx = {
            classes = programLib.classDict
            programObj = programObj
        }
        
        runMethod ctx programObj mainMethod.Value [] // mainMethod is never None because it's defined in the parent class
    | None -> errNoProgram programLib