module internal AdvancedEsolang.Interpreter.Errors

open AdvancedEsolang.Syntax

let error = failwith

let errNoProgram lib = error $"Cannot run program '{lib.name}': No program class found"
let errGet name = error $"Cannot get '{name}': No such variable or class"
let errCall (cls: Class) (mtd: string) msg = error $"Cannot call method '{mtd}' on an object of class '{cls.name}': {msg}"
let errCallUndef cls mtd = errCall cls mtd "Class doesn't define such method"
let errCallEmpty cls mtd = errCall cls mtd "Method's body is empty (unoverriden abstract method?)"
let errCallWrongArgC cls mtdN (mtd: Method) (argC: int) = errCall cls mtdN $"Wrong arg count. Expected {mtd.parameters.Length} ({System.String.Join(',', mtd.parameters)}), got {argC}"
let errCallNoProgram cls mtd = errCall cls mtd $"The 'program' field of the object is not set to the intance of the currently running program"
let errCallWrongProgram cls mtd = errCall cls mtd $"The object isn't the currently running program"
let errFieldUndef (cls: Class) fd = error $"Cannot set '{fd}' field of an object of class '{cls.name}': No such field"
let errIs obj cls = error $"Cannot check if '{obj}' is '{cls}': No such class"
let errNum n = error $"Cannot instantiate number '{n}': No class with that name was found (class_number not imported?)"
let errAbstract (cls: Class) = error $"Cannot instantiate an object of '{cls.name}' class: The class is abstract"

let errInternal err = error $"Internal: {err}"
let errBuiltinMtd () = errInternal "Cannot register a builtin method executor: Method doesn't exist"

let assertExsists option error =
    match option with
        | Some res -> res
        | None -> error ()
