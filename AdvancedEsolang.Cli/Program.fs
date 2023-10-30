open System.IO

open System.CommandLine
open FParsec

open AdvancedEsolang.Syntax
open AdvancedEsolang.InterpreterCS
open AdvancedEsolang.Parser
open AdvancedEsolang.Stringifier

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
    let library = {
        name = Path.GetRelativePath(perspective, if path.EndsWith(".adv") then path.Substring(0, path.Length - 4) else path);
        classes = [];
        dependencies = [BuiltinTypes.library]
    }
    
    let dir = Path.GetDirectoryName(path)
    
    let depProvider p =
        match depCache.TryGetValue(p) with
        | true, v -> v
        | false, _ ->
            let r = readD dir (Path.Combine(dir, p + ".adv")) depCache
            depCache[p] <- r
            r

    match runParserOnFile (Parsers.library depProvider) library path System.Text.Encoding.Default with
    | Success (res, _, _) ->
        res
    | Failure (message, err, _) -> error message

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
            
    for c in [runCmd; formatCmd; generateCmd; mergeCmd; prefixCmd] do rootCmd.AddCommand(c)    
    
    runCmd.SetHandler(fun input ->
        let evalParser = System.Func<string, Statement seq>(fun text ->
            match runParserOnString Parsers.stmts BuiltinTypes.library "eval" (text + "\nend") with
            | Success (res, _, _) ->
                res
            | Failure (message, err, _) -> error message
        )
        
        let interpreter = AdvInterpreter(read input, evalParser)
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
        
        let allClasses = lib.fullDeps |> List.filter (fun d -> d <> BuiltinTypes.library) |> List.rev |> List.map (fun l -> l.classes) |> List.concat

        let mutable usedNames = Set.empty<string>
        
        let finalClasses =
            if strip then            
                let mapper expr =
                    match expr with
                    | Get s ->
                        usedNames <- usedNames.Add(s)
                        expr
                    | _ -> expr
                    
                for c in allClasses do
                    match c.parent with
                    | Some p -> usedNames <- usedNames.Add(p.name)
                    | None -> ()
                    
                    Mapping.classMapExprs mapper c |> ignore
                
                allClasses |> List.filter (fun c -> c.is(BuiltinTypes.Program) || usedNames.Contains(c.name))
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
    
    rootCmd.Invoke(argv)