namespace AdvancedEsolang.Syntax

type ClassTree = ClassTree of Class * ClassTree list

module ClassTree =
    let rec make (top: ClassTree) =
        match top with
        | ClassTree ({ parent = Some p }, _) -> make <| ClassTree (p, [top])
        | _ -> top

    let rec concat list =
        list |> List.groupBy (function ClassTree (c, _) -> c) |> List.map (fun (p, c) -> ClassTree (p, c |> List.map (function ClassTree (_, t) -> t) |> List.concat |> concat))

    let create classes =
        classes |> List.map (fun c -> ClassTree (c, [])) |> List.map make |> concat
