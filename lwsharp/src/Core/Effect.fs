module lwsharp.Core.Effect

open lwsharp.Core.Errors
open lwsharp.Core.State

type Computation<'a> = Store -> Result<'a * Store, RuntimeError>

let run f store : Result<'a * Store, RuntimeError> =
    f store

let returnValue (x: 'a) : Computation<'a> = fun store -> Ok (x, store)

let bind (m: Computation<'a>) (f: 'a -> Computation<'b>) : Computation<'b> =
    fun store ->
        match m store with
        | Error err -> Error err
        | Ok (value, newStore) -> f value newStore

let (>>=) m f = bind m f
let map f m = m >>= (f >> returnValue)

(**
readVar "x" // e.g Map ["x": 41] (Ok (41, store))
|> mapForShowCase (fun v -> v + 1)
Results in Ok (42, sameStore)
*)
let mapForShowCase f m : Computation<'b> =
    fun store ->
        match m store with
        | Error err -> Error err
        | Ok (value, newStore) -> Ok (f value, newStore)

let apply (mf: Computation<'a -> 'b>) (m: Computation<'a>) : Computation<'b> =
    fun store ->
        match mf store with
        | Error err -> Error err
        | Ok (f, store1) ->
            match m store1 with
            | Error err -> Error err
            | Ok (value, store2) -> Ok (f value, store2)

let (<*>) mf m = apply mf m

let pure' (x: 'a) : Computation<'a> = returnValue x

let readVar var : Computation<int> =
    fun store ->
        match Map.tryFind var store with
        | Some v -> Ok (v, store)
        | None -> Error (UndefinedVariable var)

let writeVar var value : Computation<unit> =
    fun store -> Ok ((), Map.add var value store)
