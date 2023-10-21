open System.IO

open System.CommandLine
open FParsec

open AdvancedEsolang.Syntax
open AdvancedEsolang.InterpreterCS
open AdvancedEsolang.Parser
open AdvancedEsolang.Stringifier

open Generators

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
    | Failure (message, error, _) -> failwith message

let read (path: string): Library =
    let depCache = System.Collections.Generic.Dictionary<string, Library>()

    let fpath = path |> Path.GetFullPath
    readD (fpath |> Path.GetDirectoryName) fpath depCache

[<EntryPoint>]
let main argv =   
    let rootCmd = RootCommand()

    let input = Argument<string>("input", "path of the input file.")    
    let output = Option<string>([|"--output"; "-o"|], "path of the output file.")
    
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
    
    for c in [runCmd; formatCmd; generateCmd; mergeCmd] do rootCmd.AddCommand(c)    
    
    runCmd.SetHandler(fun input ->
        let evalParser = System.Func<string, Statement seq>(fun text ->
            match runParserOnString Parsers.stmts BuiltinTypes.library "eval" (text + "\nend") with
            | Success (res, _, _) ->
                res
            | Failure (message, error, _) -> failwith message
        )
        
        let interpreter = AdvInterpreter(read input, evalParser)
        BuiltinMethods.AddAll(interpreter)
        interpreter.Run()
    , input)
    
    formatCmd.SetHandler(fun input ->
        let library = read input
        let code = Stringifier.sLibrary library
        File.WriteAllText(input, code)
    , input)
    
    generateCmd.SetHandler(fun name output number ->
        let watch = System.Diagnostics.Stopwatch()
        watch.Start()

        printfn "Generating..."
        let library =
            if name = "builtin" then
                BuiltinTypes.library
            else
                match generators.TryGetValue(name) with
                | true, gen -> gen number
                | false, _ -> failwithf "Unknown generator '%s'." name
        
        printfn "Generated in %i ms." watch.ElapsedMilliseconds
        watch.Restart()

        printfn "Stringifying..."
        let libraryStr = Stringifier.sLibrary library
        printfn "Stringified in %i ms." watch.ElapsedMilliseconds
        watch.Restart()

        printfn "Saving..."
        File.WriteAllText (output, libraryStr)
        printfn "Saved in %i ms." watch.ElapsedMilliseconds
        watch.Restart()
    , libName, output, number)
    
    mergeCmd.SetHandler(fun input output ->
        let lib = read input

        let merged = { lib with dependencies = [BuiltinTypes.library]; classes = (lib.fullDeps |> List.filter (fun d -> d <> BuiltinTypes.library) |> List.rev |> List.map (fun l -> l.classes) |> List.concat) }
        let code = Stringifier.sLibrary merged
        File.WriteAllText(output, code)
    , input, output)
    
    try
        rootCmd.Invoke(argv)
    with
    | :? AdvException as err ->
        eprintfn "%s" err.Message
        1