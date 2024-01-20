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
        let assembly = Path.GetFileName(declaringType.Assembly.Location)

        if Path.Exists(xmlPath) then
            // for FullName cases like Microsoft.FSharp.Core.FSharpOption`1[System.Object]
            let fullName =
                let idx = declaringType.FullName.IndexOf('[')

                if idx >= 0 then
                    declaringType.FullName.Substring(0, idx)
                else
                    declaringType.FullName

            let fullName = fullName.Replace('+', '.') // for FullName cases like Microsoft.FSharp.Collections.ArrayModule+Parallel

            Some(xmlPath, assembly, fullName, implName, sourceName |> Option.defaultValue implName)
        else
#if DEBUG
            printfn $"xml file not found: {xmlPath}"
#endif
            None

    let rec exprNames expr =
        match expr with
        | SpecificCall <@@ (+) @@> (_, _, exprList) -> exprNames exprList.Tail.Head
        | Call(exprOpt, methodInfo, _exprList) ->
            match exprOpt with
            | Some _ -> None
            | None ->
                let sourceName = tryGetSourceName methodInfo
                getInfos methodInfo.DeclaringType sourceName methodInfo.Name
        | Lambda(_param, body) -> exprNames body
        | Let(_, _, body) -> exprNames body
        | Value(_o, t) -> getInfos t (Some t.Name) t.Name
        | DefaultValue t -> getInfos t (Some t.Name) t.Name
        | PropertyGet(_o, info, _) -> getInfos info.DeclaringType (Some info.Name) info.Name
        | NewUnionCase(info, _exprList) -> getInfos info.DeclaringType (Some info.Name) info.Name
        | NewObject(ctorInfo, _e) -> getInfos ctorInfo.DeclaringType (Some ctorInfo.Name) ctorInfo.Name
        | NewArray(t, _exprs) -> getInfos t (Some t.Name) t.Name
        | NewTuple _ ->
            let x = (23, 42)
            let t = x.GetType()
            getInfos t (Some t.Name) t.Name
        | NewStructTuple _ ->
            let x = struct (23, 42)
            let t = x.GetType()
            getInfos t (Some t.Name) t.Name
        | _ ->
#if DEBUG
            printfn $"unsupported expr: {expr}"
#endif
            None

[<AutoOpen>]
module Logic =

    open Expr
    open Parser

    module Quoted =
        let tryGetDocumentation expr =
            match exprNames expr with
            | Some(xmlPath, assembly, modName, implName, sourceName) ->
                helpText xmlPath assembly modName implName sourceName
            | _ -> None

        let h (expr: Quotations.Expr) =
            match tryGetDocumentation expr with
            | None -> printfn "unable to get documentation"
            | Some d -> d.Print()

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
