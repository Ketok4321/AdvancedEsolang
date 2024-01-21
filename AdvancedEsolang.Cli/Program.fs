open System.IO

open System.CommandLine
open FParsec

open AdvancedEsolang.Syntax
open AdvancedEsolang.InterpreterCS
open AdvancedEsolang.Parser
open AdvancedEsolang.Stringifier

open MethodBodyUtil
open Generators

// For aot compatibility
let printfn (str: string) = System.Console.WriteLine(str)

let error (str: string) =
    System.Console.ForegroundColor <- System.ConsoleColor.Red
    System.Console.Error.WriteLine(str)
    System.Console.ResetColor()
    System.Environment.Exit(1)
    failwith "say gex"

let rec readD (perspective: string) (path: string) (depCache: System.Collections.Generic.Dictionary<string, Library>): Library =
    if File.Exists(path) then
        let library = {
            name = Path.GetRelativePath(perspective, if path.EndsWith(".adv") then path.Substring(0, path.Length - 4) else path);
            classes = [];
            dependencies = [BuiltinTypes.library]
        }
        
        let dir = Path.GetDirectoryName(path)
        
        let depProvider p =
            let fp = Path.GetFullPath(Path.Combine(dir, p + ".adv"))
            match depCache.TryGetValue(fp) with
            | true, v -> v
            | false, _ ->
                let r = readD dir fp depCache
                depCache[fp] <- r
                r

        match runParserOnFile (Parsers.library depProvider) library path System.Text.Encoding.Default with
        | Success (res, _, _) ->
            res
        | Failure (message, err, _) -> error message
    else
        error $"Cannot import `{path}`: No such file"

let read (path: FileInfo): Library =
    let depCache = System.Collections.Generic.Dictionary<string, Library>()

    readD path.DirectoryName path.FullName depCache

[<EntryPoint>]
let main argv =   
    let rootCmd = RootCommand()

    let input = Argument<FileInfo>("input", "path of the input file.")
    input.AddValidator(fun res ->
        let file = res.GetValueOrDefault<FileInfo>() 
        if not file.Exists then
            res.ErrorMessage <- $"File '{file}' does not exist"
        else
            ()
    )
    let output = Option<FileInfo>([|"--output"; "-o"|], "path of the output file.")
    let strip = Option<bool>("--strip", "if unused code should be stripped.")
    let prefix = Option<string>([|"--namespace"; "-n"|], "the namespace used as a prefix.")
        
    let libName = Argument<string>("name", "name of the library to generate.")
    let number = Option<int>([|"--number"; "-n"|], "number passed as an argument to the generator.")
    
    let runCmd = Command("run", "run the specified file.")
    runCmd.AddArgument(input)
    
    let formatCmd = Command("format", "format the specified file.")
    formatCmd.AddArgument(input)
    
    let generateCmd = Command("generate", "generate a library.")
    generateCmd.AddAlias("gen")
    generateCmd.AddArgument(libName)
    generateCmd.AddOption(output)
    generateCmd.AddOption(number)
    
    let mergeCmd = Command("merge", "merge a file with all its imports outputting a single-file program.")
    mergeCmd.AddArgument(input)
    mergeCmd.AddOption(output)
    mergeCmd.AddOption(strip)

    let prefixCmd = Command("prefix", "prefix all classes in a file with a given string.")
    prefixCmd.AddArgument(input)
    prefixCmd.AddOption(output)
    prefixCmd.AddOption(prefix)
            
    let mirrorCmd = Command("mirror", "generate reflection metadata for a library.")
    mirrorCmd.AddArgument(input)
    mirrorCmd.AddOption(output)
            
    for c in [runCmd; formatCmd; generateCmd; mergeCmd; prefixCmd; mirrorCmd] do rootCmd.AddCommand(c)
    
    runCmd.SetHandler(fun input ->
        let evalParser = System.Func<string, Statement seq>(fun text ->
            match runParserOnString Parsers.stmts BuiltinTypes.library "eval" (text + "\nend") with
            | Success (res, _, _) ->
                res
            | Failure (message, err, _) -> error message
        )
        
        let lib = read input
        
        let program =
            match AdvInterpreter.GetPrograms(lib) |> Seq.toList with
            | [p] -> p
            | [] -> error $"Cannot run '${lib.name}': No program class found"
            | l ->
                printfn "Choose the program to run:"
                for i, p in l |> List.indexed do
                    printfn $"{i + 1}) {p}"
                
                System.Console.Write("> ")
                let input = System.Console.ReadLine()
                match System.Int32.TryParse(input) with
                | true, num ->
                    if num >= 1 && num <= l.Length then
                        l[num - 1]
                    else
                        error $"Cannot run a program with index '{input}': Index out of range"
                | _ -> error $"Cannot run a program with index '{input}': Not a number"
        
        let interpreter = AdvInterpreter(lib, program, evalParser)
        BuiltinMethods.AddAll(interpreter)
        try
            interpreter.Run()        
        with
        | :? AdvException as err ->
            error err.Message
    , input)
    
    formatCmd.SetHandler(fun input ->
        let library = read input
        let code = Stringifier.sLibrary library
        File.WriteAllText(input.FullName, code)
    , input)
    
    generateCmd.SetHandler(fun name (output: FileInfo) number ->
        let watch = System.Diagnostics.Stopwatch()
        watch.Start()

        printfn "Generating..."
        let library =
            if name = "builtin" then
                BuiltinTypes.library
            else
                match generators.TryGetValue(name) with
                | true, gen -> gen number
                | false, _ -> error $"Unknown generator '{name}'."
        
        printfn $"Generated in %i{watch.ElapsedMilliseconds} ms."
        watch.Restart()

        printfn "Stringifying..."
        let libraryStr = Stringifier.sLibrary library
        printfn $"Stringified in %i{watch.ElapsedMilliseconds} ms."
        watch.Restart()

        printfn "Saving..."
        File.WriteAllText (output.FullName, libraryStr)
        printfn $"Saved in %i{watch.ElapsedMilliseconds} ms."
        watch.Restart()
    , libName, output, number)
    
    mergeCmd.SetHandler(fun input (output: FileInfo) strip ->
        let lib = read input

        let allClasses = lib.fullDeps |> List.filter (fun d -> d <> BuiltinTypes.library) |> List.map (fun l -> l.classes) |> List.concat
        
        let finalClasses =
            if strip then
                let classDict = lib.classDict
                
                let rec references (_class: Class) (referenced: System.Collections.Generic.HashSet<string>) (referencedMembers: System.Collections.Generic.HashSet<string>) =
                    if referenced.Add(_class.name) then
                        match _class.parent with
                        | Some p ->
                            references p referenced referencedMembers
                        | None -> ()

                        let mutable quene = []
                        
                        let mapper expr =
                            match expr with
                            | Get s ->
                                match classDict.TryGetValue s with
                                | true, c ->
                                    quene <- c :: quene
                                | false, _ -> ()
                            | CallExpr (_, methodName, _) ->
                                referencedMembers.Add(methodName) |> ignore
                            | GetF (_, fieldName) ->
                                referencedMembers.Add(fieldName) |> ignore
                            | _ -> ()
                            expr

                        let mapperStmt stmt =
                            match stmt with
                            | CallStmt (_, methodName, _) ->
                                referencedMembers.Add(methodName) |> ignore
                            | _ -> () 
                            stmt
                        
                        _class |> Mapping.classMapExprs mapper |> ignore
                        _class |> Mapping.classMapStmts mapperStmt |> ignore

                        for c in quene do
                            references c referenced referencedMembers
                    else
                        ()
                
                let root = lib.classes |> List.filter (fun c -> c.is(BuiltinTypes.Program) && not c.isAbstract)
                
                let keep = (System.Collections.Generic.HashSet<string>())
                let keepMembers = (System.Collections.Generic.HashSet<string>(["main"; "toString"]))
                for c in root do
                    references c keep keepMembers
                
                allClasses |> List.filter (fun c -> keep.Contains(c.name)) |> List.map(fun c -> { c with ownMembers = c.ownMembers |> List.filter (fun m -> keepMembers.Contains(m.name))})
            else
                allClasses

        let merged = { lib with dependencies = [BuiltinTypes.library]; classes = finalClasses }
        let code = Stringifier.sLibrary merged
        File.WriteAllText(output.FullName, code)
    , input, output, strip)
    
    prefixCmd.SetHandler(fun input (output: FileInfo) prefix ->
        let lib = read input
        
        let mapper _class =
            let fromThisLib name = lib.classes |> List.exists (fun c -> c.name = name) // This could theoretically cause issues if there was a variable called the same as a class
            {
                (_class |> Mapping.classMapExprs (
                    function
                    | Get name ->
                        if fromThisLib name then
                            Get (prefix + name)
                        else
                            Get name
                    | Is (obj, className) ->
                        if fromThisLib className then
                            Is (obj, prefix + className)
                        else
                            Is (obj, className)
                    | e -> e
                ))
                with
                    name = prefix + _class.name
                    parent =
                        match _class.parent with
                        | Some p ->
                            if fromThisLib p.name then
                                Some { p with name = prefix + p.name }
                            else
                                Some p
                        | None -> None
            }

        let merged = { lib with classes = lib.classes |> List.map mapper }
        let code = Stringifier.sLibrary merged
        File.WriteAllText(output.FullName, code)
    , input, output, prefix)

    mirrorCmd.SetHandler(fun input (output: FileInfo) ->
            let lib = read input
            
            let allClasses = lib.fullDeps |> List.map (fun l -> l.classes) |> List.concat // How will it behave with naming collisions?
            let tree = ClassTree.create allClasses

            let accessTree tree (generator: Class -> Statement list) =
                let rec generatePart ct =
                    match ct with
                    | ClassTree (_class, children) ->
                        [If(
                            Is(Get "obj", _class.name),
                            (children |> List.map generatePart |> List.concat) @ (generator _class)
                        )]

                tree |> List.map generatePart |> List.concat

            let mirror = {
                name = "mirror"
                dependencies = [BuiltinTypes.library]
                classes = [
                    {
                        name = "Mirror"
                        parent = Some BuiltinTypes.Object
                        isAbstract = false
                        
                        ownMembers = [
                            Field("toReflect")
                                                        
                            Method("instantiate", ["typeName"], Some [
                                for _class in allClasses do
                                    If(
                                        CallExpr(Get "typeName", "equals", [str _class.name]),
                                        [
                                            Return(_new _class)
                                        ]
                                    )
                            ])

                            Method("typeName", ["obj"], Some (accessTree tree (fun _class -> [
                                Return(str _class.name)
                            ])))
                            
                            Method("reflecting", ["typeName"], Some [
                                SetF(This, "toReflect", Get "typeName")
                                Return(This)
                            ])

                            Method("parent", [], Some [
                                for _class in allClasses do
                                    If(
                                        CallExpr(GetF (This, "toReflect"), "equals", [str _class.name]),
                                        [
                                            Return <| match _class.parent with //TODO: optimise for common parents
                                                      | Some p -> str p.name
                                                      | None -> _new BuiltinTypes.Null
                                        ]
                                    )
                            ])

                            Method("isAbstract", [], Some [
                                for _class in allClasses do
                                    if _class.isAbstract then
                                        If(
                                            CallExpr(GetF (This, "toReflect"), "equals", [str _class.name]),
                                            [
                                                Return(_new BuiltinTypes.True)
                                            ]
                                        )
                                Return(_new BuiltinTypes.False)
                            ])

                            Method("field", ["index"], Some [
                                for _class in allClasses do
                                    if not (_class.ownMembersOfType<Field>() |> List.isEmpty) then
                                        If(
                                            CallExpr(GetF (This, "toReflect"), "equals", [str _class.name]),
                                            [
                                                for i, field in _class.ownMembersOfType<Field>() |> List.indexed do
                                                    If(
                                                        CallExpr(Get "index", "equalsStr", [str <| i.ToString()]),
                                                        [
                                                            Return(str field.name)
                                                        ]
                                                    )
                                            ]
                                        )
                            ])

                            Method("method", ["index"], Some [
                                for _class in allClasses do
                                    if not (_class.ownMembersOfType<Method>() |> List.isEmpty) then
                                        If(
                                            CallExpr(GetF (This, "toReflect"), "equals", [str _class.name]),
                                            [
                                                for i, method in _class.ownMembersOfType<Method>() |> List.indexed do
                                                    If(
                                                        CallExpr(Get "index", "equalsStr", [str <| i.ToString()]),
                                                        [
                                                            Return(str method.name)
                                                        ]
                                                    )
                                            ]
                                        )
                            ])
                        ]
                    }
                ];
            }
                 
            File.WriteAllText(output.FullName, Stringifier.sLibrary mirror)
    , input, output)
    
    rootCmd.Invoke(argv)
