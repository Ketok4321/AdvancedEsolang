module AdvancedEsolang.Interpreter.MethodBodyUtil

open AdvancedEsolang.Syntax

let This = Get "this"

let Abstract = None
let Builtin = None
let Constant v = Some [Return (v)]

let this field = GetF (This, field)
let str = Expression.String
let _new (_class: Class) = Get _class.name
let not expr = CallExpr(expr, "not", []) 