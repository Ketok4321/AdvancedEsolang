module internal AdvancedEsolang.Interpreter.BuiltinMethods

open System.Collections.Generic
open AdvancedEsolang.Interpreter.Errors
open AdvancedEsolang.Interpreter.RunUtil
open AdvancedEsolang.Syntax

type BuiltinMethod = RunCtx * Object * Object list -> Object

let private dict = Dictionary<Method, BuiltinMethod>()

let private register (_class: Class) method fn =
    dict[assertExsists (_class.get<Method> method) errBuiltinMtd] <- fn

let get method =
    dict.TryGetValue(method)

// Util
let requireProgram ctx program =
    if program <> ctx.programObj then
        failwith "Wrong Program object"

// Methods
register BuiltinTypes.Output "write" (fun (ctx, this, args) ->
    requireProgram ctx (this.getField "program")
    
    printfn $"%O{args[0]}"
    Object.Null ()
)

register BuiltinTypes.Input "read" (fun (ctx, this, args) ->
    requireProgram ctx (this.getField "program")
    printf "> "
    System.Console.ReadLine() |> string
)

register BuiltinTypes.String "equals" (fun (ctx, this, args) ->
    let str1 = (this :?> StringObj).value
    let str2 = (args[0] :?> StringObj).value

    str1 = str2 |> bool
)

register BuiltinTypes.String "getLength" (fun (ctx, this, args) ->
    let str = (this :?> StringObj).value
    str.Length |> int ctx
)

register BuiltinTypes.Program "error" (fun (ctx, this, args) ->
    requireProgram ctx this
    
    let str = (args[0] :?> StringObj).value
    
    failwith str
)

register BuiltinTypes.TypeUtils "create" (fun (ctx, this, args) ->
    let str = (args[0] :?> StringObj).value //TODO: Safety
    assertExsists (ctx.getClass str) (fun () -> errGet str) |> Object
)

register BuiltinTypes.TypeUtils "typeName" (fun (ctx, this, args) ->
    args[0]._class.name |> string
)
