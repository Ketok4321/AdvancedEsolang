module internal AdvancedEsolang.Interpreter.BuiltinMethods

open AdvancedEsolang.Interpreter.Errors
open AdvancedEsolang.Interpreter.RunUtil
open AdvancedEsolang.Syntax

let write (args: Object list) =
    printfn $"%O{args[0]}"

let read () =
    printf "> "
    System.Console.ReadLine() |> string

let strEquals (this: Object) (args: Object list) =
    let str1 = (this :?> StringObj).value
    let str2 = (args[0] :?> StringObj).value

    str1 = str2 |> bool

let strGetLength (ctx: RunCtx) (this: Object) =
    let str = (this :?> StringObj).value
    str.Length |> int ctx

let error (args: Object list) =   
    let str = (args[0] :?> StringObj).value
    
    failwith str

let createObj (ctx: RunCtx) (args: Object list) =
    let str = (args[0] :?> StringObj).value //TODO: Safety
    assertExsists (ctx.getClass str) (fun () -> errGet str) |> Object

let typeName (args: Object list) =
    args[0]._class.name |> string
