module AdvancedEsolang.Interpreter.BuiltinTypes

open AdvancedEsolang.Syntax
open MethodBodyUtil

let Object = {
    name = "Object"
    parent = None
    isAbstract = false
    ownMembers = []
}

let Null = {
    name = "Null"
    parent = None
    isAbstract = false
    ownMembers = []
}

let String = {
    name = "String"
    parent = Some Object  //TODO: ?
    isAbstract = false
    ownMembers = [
        Method("equals", ["s"], Builtin)
        Method("getLength", [], Builtin)
    ]
}

let Program = {
    name = "Program"
    parent = Some Object
    isAbstract = true
    ownMembers = [
        Method("main", [], Abstract)

        Method("error", ["message"], Builtin)
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

// Utils

let TypeUtils = {
    name = "TypeUtils"
    parent = Some Object
    isAbstract = false
    ownMembers = [
        Method("create", ["typeName"], Builtin)
        Method("typeName", ["obj"], Builtin)
    ]
}

let library = {
    name = "@builtin"
    classes = [Object; Null; Program; Input; Output; Boolean; True; False; TypeUtils]
    dependencies = []
}