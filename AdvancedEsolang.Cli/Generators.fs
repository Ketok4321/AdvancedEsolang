module Generators

open System.Collections.Generic

open AdvancedEsolang.Syntax

open BuiltinTypes
open MethodBodyUtil

type Generator = int -> Library

let generators = Dictionary<string, Generator>()

let private generator name deps f =
    let simpleLib classes = { name = name; classes = classes; dependencies = deps |> List.map (fun d -> { name = d; classes = []; dependencies = [] }) }
    generators.Add(name, f >> simpleLib)

let extParent n = Some { name = n; parent = None; isAbstract = true; ownMembers = [] }

generator "class_number" ["../std/number"] (fun n ->
    let ClassNumber = {
        name = "ClassNumber"
        parent = extParent "Number"
        isAbstract = true
        ownMembers = [
            Method ("isZero", [], Some [
                Return (_new False)
            ])

            Method ("toString", [], Some [
                Return (CallExpr(_new TypeManager, "typeName", [This]))
            ])
        ]
    }

    let numberClass n = {
        name = string n
        parent = Some ClassNumber
        isAbstract = false
        ownMembers = [
            Method ("++", [], Some [
                Return (Get (string (n + 1)))
            ])
            Method ("--", [], Some [
                Return (Get (string (n - 1)))
            ])
            if n = 0 then
                Method ("isZero", [], Some [
                    Return (_new True)
                ])
        ]
    }

    ClassNumber :: ([0..n] |> List.map numberClass)
)

generator "instance_number" ["../std/number"] (fun n ->
    let InstanceNumber = {
        name = "InstanceNumber"
        parent = extParent "Number"
        isAbstract = false
        ownMembers = [
            Field ("next")
            Field ("previous")
            
            Field ("string")
            
            Method ("++", [], Constant (this "next"))
            Method ("--", [], Constant (this "previous"))
                    
            Method ("isZero", [], Constant (Is ((this "previous"), "Null"))) // NOTE: The logic will need to be changed when implementing negative numbers

            Method ("toString", [], Constant (this "string"))
        ]
    }

    let Numbers = {
        name = "Numbers"
        parent = Some Object
        isAbstract = false
        ownMembers = [
            for i in [0..n] do
                Field (string i)
            
            Method ("init", [], Some [
                for i in [0..n] do
                    SetF (This, string i, CallExpr (This, "_next", [str (string i); if i = 0 then _new Null else this (string (i - 1))]))
            ])
            
            Method ("_next", ["string"; "previous"], Some [
                SetV ("result", _new InstanceNumber)
                SetF (Get "result", "previous", Get "previous")
                SetF (Get "result", "string", Get "string")
                If (not (Is (Get "previous", "Null")), [
                    SetF (Get "previous", "next", Get "result")
                ])
                Return (Get "result")
            ])
        ]
    }

    [InstanceNumber; Numbers]
)

generator "binary_number" ["../std/number"] (fun n ->
    let n = n - 1
    
    let rec generateInc n i =
        if n < 0 then
            [Return This]
        else
            let ns = string n
            [
                If(Is(this ns, if i then "False" else "True"), [
                    SetF(This, ns, _new (if i then True else False))
                    Return This
                ])

                If(Is(this ns, if i then "True" else "False"), [
                    SetF(This, ns, _new (if i then False else True))
                ] @ generateInc (n - 1) i)
            ]
    
    let rec generateString v c n =
        if n < 0 then
            [
                Return (str (string v))
            ]
        else
            let ns = string n
            [
                If(Is(this ns, "True"), generateString (v + c) (c * 2) (n - 1))
                If(Is(this ns, "False"), generateString (v) (c * 2) (n - 1))
            ]

    let BinaryNumber = {
        name = "BinaryNumber"
        parent = extParent "Number"
        isAbstract = false
        ownMembers = [
            for i in [0..n] do
                Field(string i)

            Method ("of", [0..n] |> List.map string, Some [
                for i in [0..n] do
                    SetF(This, string i, Get (string i))
                Return This             
            ])
            
            Method ("zero", [], Some [
                Return (CallExpr(This, "of", List.init (n + 1) (fun _ -> _new False) ))
            ])

            Method ("clone", [], Some [
                SetV("clone", Get "BinaryNumber")
                for i in [0..n] do
                    SetF(Get "clone", string i, this (string i))
                Return (Get("clone"))
            ])

            Method ("++", [], Some (generateInc n true))
            Method ("--", [], Some (generateInc n false))
                    
            Method ("isZero", [], Some [
                for i in [0..n] do
                    If(Is(this (string i), "True"), [
                        Return (_new False)
                    ])
                Return (_new True)
            ])

            Method ("toString", [], Some (generateString 0 1 n))
        ]
    }

    [BinaryNumber]
)

generator "array" ["class_number"] (fun n ->
    let Array = {
        name = "Array"
        parent = Some Object
        isAbstract = true
        ownMembers = [
            Method ("get", ["index"], Abstract)
            Method ("set", ["index"; "value"], Abstract)
            Method ("size", [], Abstract)
        ]
    }

    let arrayClass n = {
        name = "Array" + string n
        parent = Some Array
        isAbstract = false
        ownMembers = [
            for i in [0..n-1] do
                Field (string i)

            Method ("get", ["index"], Some [
                for i in [0..n-1] do
                    If(Is(Get("index"), string i), [
                        Return (this (string i))
                    ])
                //TODO: Error
            ])
            Method ("set", ["index"; "value"], Some [
                for i in [0..n-1] do
                    If(Is(Get("index"), string i), [
                        SetF(This, string i, Get("value"))
                    ])
                //TODO: Error
            ])
            Method ("size", [], Some [
                Return (Get (string n))
            ])
        ]
    }

    Array :: ([0..n] |> List.map arrayClass)
)