namespace Fsih

module Expr =

    open System
    open System.IO
    open System.Reflection
    open Microsoft.FSharp.Quotations.DerivedPatterns
    open Microsoft.FSharp.Quotations.Patterns

    let tryGetSourceName (methodInfo: MethodInfo) =
        try
            let attr = methodInfo.GetCustomAttribute<CompilationSourceNameAttribute>()
            Some attr.SourceName
        with _ ->
            None

    let getInfos (declaringType: Type) (sourceName: string option) (implName: string) =
        let xmlPath = Path.ChangeExtension(declaringType.Assembly.Location, ".xml")
        let xmlDoc = Parser.tryGetXmlDocument xmlPath
        let assembly = Path.GetFileName(declaringType.Assembly.Location)

        // for FullName cases like Microsoft.FSharp.Core.FSharpOption`1[System.Object]
        let fullName =
            let idx = declaringType.FullName.IndexOf('[')

            if idx >= 0 then
                declaringType.FullName.Substring(0, idx)
            else
                declaringType.FullName

        let fullName = fullName.Replace('+', '.') // for FullName cases like Microsoft.FSharp.Collections.ArrayModule+Parallel

        (xmlDoc, assembly, fullName, implName, sourceName |> Option.defaultValue implName)

    let rec exprNames expr =
        match expr with
        | Call(exprOpt, methodInfo, _exprList) ->
            match exprOpt with
            | Some _ -> None
            | None ->
                let sourceName = tryGetSourceName methodInfo
                getInfos methodInfo.DeclaringType sourceName methodInfo.Name |> Some
        | Lambda(_param, body) -> exprNames body
        | Let(_, _, body) -> exprNames body
        | Value(_o, t) -> getInfos t (Some t.Name) t.Name |> Some
        | DefaultValue t -> getInfos t (Some t.Name) t.Name |> Some
        | PropertyGet(_o, info, _) -> getInfos info.DeclaringType (Some info.Name) info.Name |> Some
        | NewUnionCase(info, _exprList) -> getInfos info.DeclaringType (Some info.Name) info.Name |> Some
        | NewObject(ctorInfo, _e) -> getInfos ctorInfo.DeclaringType (Some ctorInfo.Name) ctorInfo.Name |> Some
        | NewArray(t, _exprs) -> getInfos t (Some t.Name) t.Name |> Some
        | NewTuple _ ->
            let ty = typeof<_ * _>
            getInfos ty (Some ty.Name) ty.Name |> Some
        | NewStructTuple _ ->
            let ty = typeof<struct (_ * _)>
            getInfos ty (Some ty.Name) ty.Name |> Some
        | _ -> None

[<AutoOpen>]
module Logic =

    open Expr
    open Parser

    module Quoted =
        let tryGetDocumentation expr =
            match exprNames expr with
            | Some(xmlPath, assembly, modName, implName, sourceName) ->
                tryMkHelp xmlPath assembly modName implName sourceName
            | _ -> ValueNone

        let h (expr: Quotations.Expr) =
            match tryGetDocumentation expr with
            | ValueNone -> printfn "unable to get documentation"
            | ValueSome d -> d.Print()

    [<AutoOpen>]
    type H() =
        static member h([<ReflectedDefinition>] expr: Quotations.Expr<_>) = Quoted.h expr

        static member TryGetDocumentation([<ReflectedDefinition>] expr: Quotations.Expr<_>) =
            Quoted.tryGetDocumentation expr

module Program =

    [<EntryPoint>]
    let main _argv =
        h id

        0
