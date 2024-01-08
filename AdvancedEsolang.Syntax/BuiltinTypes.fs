module AdvancedEsolang.Syntax.BuiltinTypes

open AdvancedEsolang.Syntax
open MethodBodyUtil

let Object = {
    name = "Object"
    parent = None
    isAbstract = false
    ownMembers = [
        Method("throw", ["s"], Builtin)
    ]
}

let Null = {
    name = "Null"
    parent = None
    isAbstract = false
    ownMembers = []
}

let String = {
    name = "String"
    parent = None
    isAbstract = false
    ownMembers = [
        Method("char", ["n"], Builtin)
        
        Method("equals", ["s"], Builtin)
        Method("+", ["s"], Builtin)
        Method("at", ["index"], Builtin)
    ]
}

let Program = {
    name = "Program"
    parent = Some Object
    isAbstract = true
    ownMembers = [
        Method("main", [], Abstract)
    ]
}

let Input = {
    name = "Input"
    parent = Some Object
    isAbstract = false
    ownMembers = [
        Field("program")
        Method("read", [], Builtin)
    ]
}

let Output = {
    name = "Output"
    parent = Some Object
    isAbstract = false
    ownMembers = [
        Field("program")
        Method("write", ["text"], Builtin)
    ]
}

let Boolean = {
    name = "Boolean"
    parent = Some Object
    isAbstract = true
    ownMembers = [
        Method("not", [], Abstract)
        Method("and", ["b"], Abstract)
        Method("or", ["b"], Abstract)
    ]
}

let True = {
    name = "True"
    parent = Some Boolean
    isAbstract = false
    ownMembers = [
        Method("not", [], Constant (Get "False"))
        Method("and", ["b"], Constant (Get "b"))
        Method("or", ["b"], Constant (Get "True"))
    ]
}

let False = {
    name = "False"
    parent = Some Boolean
    isAbstract = false
    ownMembers = [
        Method("not", [], Constant (Get "True"))
        Method("and", ["b"], Constant (Get "False"))
        Method("or", ["b"], Constant (Get "b"))
    ]
}

let library = {
    name = "@builtin"
    classes = [Object; Null; String; Program; Input; Output; Boolean; True; False]
    dependencies = []
}
