namespace AdvancedEsolang.Syntax

[<AbstractClass>]
type ClassMember(name: string) =
    member this.name = name

type Method(name: string, parameters: string list, body: Statement list option) =
    inherit ClassMember(name)
    member this.parameters = parameters
    member this.body = body
    
    override this.ToString() =
        let prms = this.parameters |> String.concat ", "
        let body = sprintf "%A" this.body
        $"{this.name}({prms}): {body}"

type Field(name: string) =
    inherit ClassMember(name)
    
    override this.ToString() = this.name

[<CustomEquality>]
[<NoComparison>]
type Class = {
    name: string
    parent: Class option
    isAbstract: bool
    ownMembers: ClassMember list
}
with
    member this.is _class =
        if this = _class then
            true
        else
            match this.parent with
            | Some parent -> parent.is(_class)
            | None -> false
    
    member this.ownMembersOfType<'t when 't :> ClassMember> () =
        this.ownMembers |> Seq.filter (fun x -> x :? 't) |> Seq.cast<'t> |> Seq.toList
    
    member this.allMembersOfType<'t when 't :> ClassMember> () =
            match this.parent with
            | Some parent -> (this.ownMembersOfType<'t>() @ parent.allMembersOfType<'t>()) |> List.distinctBy (fun m -> m.name)
            | None -> this.ownMembersOfType<'t>()

    member this.getMember name =
        match this.ownMembers |> List.tryFind (fun m -> m.name = name) with
        | Some m -> Some m
        | None ->            
            match this.parent with
            | Some parent -> parent.getMember name
            | None -> None

    member this.get<'a when 'a :> ClassMember> name =
        match this.getMember name with
        | Some mem ->
            match mem with
            | :?'a as matchedMem -> Some matchedMem
            | _ -> None
        | None -> None
    
    override this.Equals obj =
        match obj with
        | :? Class as _class -> this.name = _class.name
        | _ -> false
    
    override this.GetHashCode() = this.name.GetHashCode()

type Library = {
    name: string
    classes: Class list
    dependencies: Library list
}
with
    member this.fullDeps =
        List.distinct <| List.append (this.dependencies |> List.map (fun d -> d.fullDeps) |> List.concat) [this]
    
    member this.classDict =
        let result = System.Collections.Generic.Dictionary<string, Class>()
        
        for dep in this.fullDeps do
            for _class in dep.classes do
                result[_class.name] <- _class
        
        result
        
    member this.getClass name =
        match this.classes |> List.tryFind (fun c -> c.name = name) with
        | Some _class -> Some _class
        | None ->
            this.dependencies |> List.tryPick (fun d -> d.getClass(name))
