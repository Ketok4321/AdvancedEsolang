module AdvancedEsolang.Interpreter.RunUtil

open System.Collections.Generic
open AdvancedEsolang.Interpreter.Errors
open AdvancedEsolang.Syntax

type RunCtx = {
    classes: IDictionary<string, Class>
    programObj: Object
}
with
    member this.getClass name =
        match this.classes.TryGetValue(name) with
        | true, v -> Some v
        | false, _ -> None

let bool b =
    Object (
        match b with
        | true -> BuiltinTypes.True
        | false -> BuiltinTypes.False
    )

let string s = StringObj(s) :> Object

let int (ctx: RunCtx) i = assertExsists (ctx.getClass(i.ToString())) (fun () -> errNum i) |> Object