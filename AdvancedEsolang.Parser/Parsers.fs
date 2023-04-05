namespace AdvancedEsolang.Parser

open FParsec

open AdvancedEsolang.Syntax

module Parsers =
    let allowedChars = letter <|> digit <|> choice (['_'; '+'; '-'; '*'; '/'] |> List.map pchar)
    let allowedFilenameChars = allowedChars <|> choice (['\\'; '.'] |> List.map pchar)

    let comment: Parser<_, Library> = skipChar '#' .>> skipRestOfLine false
    let ws: Parser<_, Library> = attempt (skipMany (spaces .>> comment .>> spaces)) <|> spaces
    let ws1: Parser<_, Library> = skipChar ' ' .>> ws
    let name: Parser<_, Library> = many1Chars allowedChars
    let filename: Parser<_, Library> = many1Chars allowedFilenameChars
    let listOf p : Parser<_, Library> = between (skipChar '(') (skipChar ')') (sepBy p (skipChar ',' .>> ws))

    let expr, private exprImpl = createParserForwardedToRef<Expression, Library> ()
    let simpleExpr, private simpleExprImpl = createParserForwardedToRef<Expression, Library> ()
    let exprP = between (skipChar '(') (skipChar ')') expr <|> simpleExpr // Expression but parenthesized if it's not simple

    let stmt, private stmtImpl = createParserForwardedToRef<Statement, Library> ()
    let stmts = ws >>. manyTill (stmt .>> ws) (skipString "end")

    let call = exprP .>> skipChar '.' .>>. name .>>. listOf expr

    module Expression =
        let operator o = exprP .>> ws .>> skipString o .>> ws .>>. exprP
        
        let get = name |>> Get
        let readF = exprP .>> skipChar '.' .>>. name |>> GetF
        let call = call |>> fun ((objExpr, methodName), args) -> CallExpr(objExpr, methodName, args)
        let is = exprP .>> ws1 .>> skipString "is" .>> ws1 .>>. name |>> Is
        let equals = operator "=" |>> Equals
        let notEquals = operator "!=" |>> Equals |>> fun o -> CallExpr(o, "not", [])
        let _not = skipChar '!' >>. exprP |>> fun o -> CallExpr(o, "not", [])
        let string = skipChar '"' >>. manyCharsTill (anyChar) (skipChar '"') |>> String
        
        let userDefinedOp o = operator o |>> fun (o1, o2) -> CallExpr(o1, o, [o2])

        do exprImpl := choice [
            attempt call
            attempt readF
            attempt is
            attempt equals
            attempt notEquals
            attempt (userDefinedOp "+")
            attempt (userDefinedOp "-")
            attempt (userDefinedOp "*")
            attempt (userDefinedOp "/")
            simpleExpr
        ]

        do simpleExprImpl := choice [
            string
            _not
            get
        ]

    module Statement =
        let setV = skipString "variable" .>> ws1 >>. name .>> ws .>> skipString "=" .>> ws .>>. expr |>> SetV
        let setF = exprP .>> skipChar '.' .>>. name .>> ws .>> skipString "=" .>> ws .>>. expr |>> fun ((objExpr, fieldName: string), value) -> SetF(objExpr, fieldName, value)
        let call = call |>> fun ((objExpr, methodName), args) -> CallStmt(objExpr, methodName, args)
        let rtrn = skipString "return" .>> ws1 >>. expr |>> Return
        let _if = skipString "if" .>> ws1 >>. expr .>> skipChar ':' .>>. stmts |>> fun (cond, stmts) -> If(cond, stmts)
        let _while = skipString "while" .>> ws1 >>. expr .>> skipChar ':' .>>. stmts |>> fun (cond, stmts) -> While(cond, stmts)

        do stmtImpl := choice [
            setV
            rtrn
            _if
            _while
            attempt call
            setF
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
        |>> (fun ((((abst, name), parent), members), library) -> library, { name = name; parent = Some library.classDict[parent]; isAbstract = abst.IsSome; ownMembers = members  })
        >>= (fun (l, c) ->
            if l.classes |> List.exists (fun c2 -> c2.name = c.name) then
                fail $"Class named '%s{c.name}' is already defined"
            else
                updateUserState (fun p -> { p with classes = c :: p.classes }) >>. preturn c)
    
    let import depProvider = skipString "import" .>> ws1 >>. filename .>> newline >>= (fun i -> updateUserState (fun p -> { p with dependencies = depProvider i :: p.dependencies }))

    let library depProvider = ws .>> (many (import depProvider .>> ws)) .>> (many (_class .>> ws)) .>> eof >>. getUserState |>> fun p -> { p with classes = List.rev p.classes }