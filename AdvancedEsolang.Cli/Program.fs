open System.IO

open FParsec
open Argu

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

[<CliPrefix(CliPrefix.Dash)>]
type RunArgs =
    | [<MainCommand; ExactlyOnce>] Path of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "path of the file to run."

and FormatArgs =
    | [<MainCommand; ExactlyOnce>] Path of string
    
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "path of the file to format."

and GenerateArgs =
    | [<MainCommand; ExactlyOnce>] Name of string
    | [<AltCommandLine("-c"); Unique>] Count of int
    | [<AltCommandLine("-o"); Unique>] Output of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Name _ -> "name of the generator."
            | Count _ -> "count of things to generate passed to the generator."
            | Output _ -> "path of the output file."

and MergeArgs =
    | [<MainCommand; ExactlyOnce>] Path of string
    | [<AltCommandLine("-o"); Unique>] Output of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "path of the file to merge."
            | Output _ -> "path of the output file."

type CliArguments =
    | [<AltCommandLine("-v")>] Version
    | [<CliPrefix(CliPrefix.None)>] Run of ParseResults<RunArgs>
    | [<CliPrefix(CliPrefix.None)>] Format of ParseResults<FormatArgs>
    | [<CliPrefix(CliPrefix.None); AltCommandLine("gen")>] Generate of ParseResults<GenerateArgs>
    | [<CliPrefix(CliPrefix.None)>] Merge of ParseResults<MergeArgs>
    
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Version -> "print the version of the interpreter."
            | Run _ -> "run the specified file."
            | Format _ -> "format the specified file."
            | Generate _ -> "generate a library."
            | Merge _ -> "merge a file with all its imports outputting a single-file program."

[<EntryPoint>]
let main argv =   
    let parser = ArgumentParser.Create<CliArguments>()

    try
        let result = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        match result.GetSubCommand() with
        | Version -> printfn "%s" (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())
        | Run r ->
            let evalParser = System.Func<string, Statement seq>(fun text ->
                match runParserOnString Parsers.stmts BuiltinTypes.library "eval" (text + "\nend") with
                | Success (res, _, _) ->
                    res
                | Failure (message, error, _) -> failwith message
            )
            
            let interpreter = AdvInterpreter(read (r.GetResult(RunArgs.Path)), evalParser)
            interpreter.Run()
        | Format r ->
            let path = r.GetResult(FormatArgs.Path)
            
            let library = read path
            let code = Stringifier.sLibrary library
            File.WriteAllText(path, code)
        | Generate r ->
            let name = r.GetResult(Name)
            
            let watch = System.Diagnostics.Stopwatch()
            watch.Start()

            printfn "Generating..."
            let library =
                if name = "builtin" then
                    BuiltinTypes.library
                else
                    let count = r.GetResult(Count)

                    match generators.TryGetValue(name) with
                    | true, gen -> gen count
                    | false, _ -> failwithf "Unknown generator '%s'." name
            
            printfn "Generated in %i ms." watch.ElapsedMilliseconds
            watch.Restart()

            printfn "Stringifying..."
            let libraryStr = Stringifier.sLibrary library
            printfn "Stringified in %i ms." watch.ElapsedMilliseconds
            watch.Restart()

            printfn "Saving..."
            File.WriteAllText (r.GetResult(GenerateArgs.Output, defaultValue = $"./{name}.adv"), libraryStr)
            printfn "Saved in %i ms." watch.ElapsedMilliseconds
            watch.Restart()
        | Merge r ->
            let path = r.GetResult(MergeArgs.Path)
            let output = r.GetResult(MergeArgs.Output)

            let lib = read path

            let merged = { lib with dependencies = [BuiltinTypes.library]; classes = (lib.fullDeps |> List.filter (fun d -> d <> BuiltinTypes.library) |> List.rev |> List.map (fun l -> l.classes) |> List.concat) }
            
            File.WriteAllText(output, Stringifier.sLibrary merged)
        0
    with
    | :? AdvException as err ->
        eprintfn "%s" err.Message
        1