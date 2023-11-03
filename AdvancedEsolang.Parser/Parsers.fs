namespace AdvancedEsolang.Parser

open FParsec

open AdvancedEsolang.Syntax

module Parsers =
    let rec private undotify (e: Expression) (s: string) =
        if s.Contains(".") then
            let rest, last = s.Substring(0, s.LastIndexOf('.')), s.Substring(s.LastIndexOf('.') + 1)
            GetF(undotify e rest, last)
        else
            GetF(e, s)
    
    let allowedChars = List.concat [[letter; digit]; (['_'; '+'; '-'; '*'; '/'] |> List.map pchar)] |> choice
    let allowedFilenameChars = allowedChars <|> choice (['\\'; '.'] |> List.map pchar)

    let comment: Parser<_, Library> = skipChar '#' .>> skipRestOfLine false
    let ws: Parser<_, Library> = attempt (skipMany (spaces .>> comment .>> spaces)) <|> spaces
    let ws1: Parser<_, Library> = skipChar ' ' .>> ws
    let name: Parser<_, Library> = many1Chars allowedChars
    let named: Parser<_, Library> = many1Chars (allowedChars <|> pchar '.')
    let filename: Parser<_, Library> = many1Chars allowedFilenameChars
    let listOf p : Parser<_, Library> = between (skipChar '(') (skipChar ')') (sepBy p (skipChar ',' .>> ws))

    let expr, private exprImpl = createParserForwardedToRef<Expression, Library> ()
    let simpleExpr, private simpleExprImpl = createParserForwardedToRef<Expression, Library> ()
    let exprP = between (skipChar '(') (skipChar ')') expr <|> simpleExpr // Expression but parenthesized if it's not simple

    let stmt, private stmtImpl = createParserForwardedToRef<Statement, Library> ()
    let stmts = ws >>. manyTill (stmt .>> ws) (skipString "end")

    module Expression =
        let operator o = exprP .>>? ws .>>? skipString o .>> ws .>>. exprP
        
        let get = name |>> Get
        let readF = exprP .>>? skipChar '.' .>>. named |>> (fun (objExpr, fieldName) -> undotify objExpr fieldName)
        let call = exprP .>>? skipChar '.' .>>. named .>>.? listOf expr |>> fun ((objExpr, methodName), args) ->
            match undotify objExpr methodName with
            | GetF (objExpr, fieldName) -> CallExpr(objExpr, fieldName, args)
            | _ -> failwith "Unreachable code"
        let is = exprP .>>? ws1 .>>? skipString "is" .>> ws1 .>>. name |>> Is
        let equals = operator "=" |>> Equals
        let notEquals = operator "!=" |>> Equals |>> fun o -> CallExpr(o, "not", [])
        let _not = skipChar '!' >>. exprP |>> fun o -> CallExpr(o, "not", [])
        let string = skipChar '"' >>. manyCharsTill (anyChar) (skipChar '"') |>> String
        
        let userDefinedOp o = operator o |>> fun (o1, o2) -> CallExpr(o1, o, [o2])

        do exprImpl := choice [
            call
            readF
            is
            equals
            notEquals
            userDefinedOp "+"
            userDefinedOp "-"
            userDefinedOp "*"
            userDefinedOp "/"
            simpleExpr
        ]

        do simpleExprImpl := choice [
            string
            _not
            get
        ]

    module Statement =
        let setV = name .>> ws .>>? skipString "=" .>> ws .>>. expr |>> SetV
        let setF = exprP .>>? skipChar '.' .>>. named .>> ws .>>? skipString "=" .>> ws .>>. expr |>> fun ((objExpr, fieldName: string), value) ->
            match undotify objExpr fieldName with
            | GetF (objExpr, fieldName) -> SetF(objExpr, fieldName, value)
            | _ -> failwith "Unreachable code"
        let call = Expression.call |>> function
            | CallExpr (objExpr, methodName, expressions) -> CallStmt(objExpr, methodName, expressions)
            | _ -> failwith "Unreachable code"
        let rtrn = skipString "return" .>>? ws1 >>. expr |>> Return
        let _if = skipString "if" .>>? ws1 >>. expr .>> skipChar ':' .>>. stmts |>> fun (cond, stmts) -> If(cond, stmts)
        let _while = skipString "while" .>>? ws1 >>. expr .>> skipChar ':' .>>. stmts |>> fun (cond, stmts) -> While(cond, stmts)
        let eval = skipString "eval" .>>? ws1 >>. expr |>> Eval

        do stmtImpl := choice [
            setF
            setV
            rtrn
            _if
            _while
            eval
            call
        ]
    
    let method = skipString "method" .>> ws1 >>. name .>>. listOf name .>>. opt (skipChar ':' >>. stmts) |>> fun ((name, prms), body) -> Method(name, prms, body)
    let field = skipString "field" .>> ws1 >>. name |>> Field
    let classMember = choice [
        field |>> fun f -> f :> ClassMember
        method |>> fun m -> m :> ClassMember
    ]

    let _class =
        opt (skipString "abstract" .>> ws1) .>>
        skipString "class" .>> ws1 .>>. name .>> ws1 .>> skipString "extends" .>> ws1 .>>. name .>> skipChar ':' .>> ws .>>.
        manyTill (classMember .>> ws) (skipString "end") .>>.
        getUserState
        |>> (fun ((((abst, name), parent), members), library) -> library, { name = name; parent = Some (library.getClass(parent).Value); isAbstract = abst.IsSome; ownMembers = members  })
        >>= (fun (l, c) ->
            if l.getClass c.name <> None then
                fail $"Duplicate '%s{c.name}' class definition"
            else if c.ownMembers |> List.exists (fun x -> c.ownMembers |> List.exists (fun y -> not (LanguagePrimitives.PhysicalEquality x y) && x.name = y.name)) then
                fail $"Duplicate class member definition inside '%s{c.name}' class"
            else
                updateUserState (fun p -> { p with classes = c :: p.classes }) >>. preturn c)
    
    let import depProvider = skipString "import" .>> ws1 >>. filename .>> newline >>= (fun i -> updateUserState (fun p -> { p with dependencies = depProvider i :: p.dependencies }))

    let library depProvider = ws .>> (many (import depProvider .>> ws)) .>> (many (_class .>> ws)) .>> eof >>. getUserState |>> fun p -> { p with classes = List.rev p.classes }
