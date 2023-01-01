namespace AdvancedEsolang.Interpreter

open System.Collections.Generic
open AdvancedEsolang.Syntax
open AdvancedEsolang.Interpreter.Errors

type Object(_class: Class) =
    do if _class.isAbstract then
        errAbstract _class

    static member Null () = Object(BuiltinTypes.Null)

    member _._class = _class

    member this.is = this._class.is
    
    member val private fields = Dictionary<string, Object>(_class.fields.Length)
    
    member this.getField name =
        match this.fields.TryGetValue(name) with
        | true, value -> value
        | _ -> Object.Null ()
    
    member this.setField name value =
        assertExsists (_class.get<Field>(name)) (fun () -> errFieldUndef _class name) |> ignore
        
        this.fields[name] <- value

    override this.ToString() = this._class.name

type StringObj(value) =
    inherit Object(BuiltinTypes.String)
    member val value = value
    
    override this.ToString() = this.value