module Fsih.Tests

open System.Collections.Generic
open FSharp.Quotations
open Xunit
open Fsih.Logic

type SeqTheoryTupleData<'t1, 't2>(data: IEnumerable<('t1 * 't2)>) =
    inherit TheoryData<'t1, 't2>()

    do
        for (d1, d2) in data do
            base.Add(d1, d2)

let expressions =
    SeqTheoryTupleData<Expr, string>(
        [ <@ Option.isNone @> :> Expr, "Microsoft.FSharp.Core.OptionModule.isNone"
          <@ None @> :> Expr, "Microsoft.FSharp.Core.FSharpOption`1.None"
          <@ Seq.allPairs @> :> Expr, "Microsoft.FSharp.Collections.SeqModule.allPairs"
          <@ Seq.append @> :> Expr, "Microsoft.FSharp.Collections.SeqModule.append"
          <@ Seq.iter @> :> Expr, "Microsoft.FSharp.Collections.SeqModule.iter"
          <@ CompilerServices.RuntimeHelpers.EnumerateTryWith @> :> Expr,
          "Microsoft.FSharp.Core.CompilerServices.RuntimeHelpers.EnumerateTryWith"
          <@ printf @> :> Expr, "Microsoft.FSharp.Core.ExtraTopLevelOperators.printf"
          <@ [] @> :> Expr, "Microsoft.FSharp.Collections.FSharpList`1.Empty"
          <@ 1 :: [] @> :> Expr, "Microsoft.FSharp.Collections.FSharpList`1.Cons"
          <@ List.isEmpty @> :> Expr, "Microsoft.FSharp.Collections.ListModule.isEmpty"
          <@ [].IsEmpty @> :> Expr, "Microsoft.FSharp.Collections.FSharpList`1.IsEmpty"
          <@ Map @> :> Expr, "Microsoft.FSharp.Collections.FSharpMap`2.#ctor"
          <@ Array2D.blit @> :> Expr, "Microsoft.FSharp.Collections.Array2DModule.blit"
          <@ HashIdentity.Reference @> :> Expr, "Microsoft.FSharp.Collections.HashIdentity.Reference"
          <@ Array.Parallel.tryFind @> :> Expr, "Microsoft.FSharp.Collections.ArrayModule.Parallel.tryFind"
          <@ (|>) @> :> Expr, "Microsoft.FSharp.Core.Operators.op_PipeRight" ]
    )

[<Theory>]
[<MemberData(nameof (expressions))>]
let ``fetching and parsing docs works for expressions`` ((expr, fullName): (Expr * string)) =
    let doc = Quoted.tryGetDocumentation expr

    match doc with
    | None -> Assert.False(true, sprintf "no docs for %s" fullName)
    | Some d -> Assert.Equal(fullName, d.FullName)

[<Fact>]
let ``full info is as expected for Seq.splitInto`` () =
    let doc = Quoted.tryGetDocumentation <@ Seq.splitInto @>

    match doc with
    | Some { Summary = summary
             Remarks = Some remarks
             Parameters = parameters
             Returns = Some returns
             Exceptions = exceptions
             Examples = examples
             FullName = fullName
             Assembly = assembly } ->
        Assert.Equal("Splits the input sequence into at most count chunks.", summary)

        Assert.Equal(
            "This function returns a sequence that digests the whole initial sequence as soon as that\nsequence is iterated. As a result this function should not be used with large or infinite sequences.",
            remarks.ReplaceLineEndings("\n")
        )

        Assert.Equal(2, parameters.Length)
        Assert.Equal("count", fst parameters[0])
        Assert.Equal("The maximum number of chunks.", snd parameters[0])
        Assert.Equal("source", fst parameters[1])
        Assert.Equal("The input sequence.", snd parameters[1])
        Assert.Equal("The sequence split into chunks.", returns)
        Assert.Equal(2, exceptions.Length)
        Assert.Equal("System.ArgumentNullException", fst exceptions[0])
        Assert.Equal("Thrown when the input sequence is null.", snd exceptions[0])
        Assert.Equal("System.ArgumentException", fst exceptions[1])
        Assert.Equal("Thrown when count is not positive.", snd exceptions[1])
        Assert.Equal(2, examples.Length)

        Assert.Equal(
            "let inputs = [1; 2; 3; 4; 5]\n\ninputs |> Seq.splitInto 3",
            (fst examples[0]).ReplaceLineEndings("\n")
        )

        Assert.Equal("Microsoft.FSharp.Collections.SeqModule.splitInto", fullName)
        Assert.Equal("FSharp.Core.dll", assembly)

    | Some _ -> Assert.False(true, "unexpected help")
    | None -> Assert.False(true, "no docs for Seq.splitInto")

[<Fact>]
let ``returns is as expected for HashIdentity.FromFunctions`` () =
    let doc = Quoted.tryGetDocumentation <@ HashIdentity.FromFunctions @>

    match doc with
    | Some { Returns = Some returns } ->
        Assert.Equal(
            "An object implementing System.Collections.Generic.IEqualityComparer using the given functions.",
            returns
        )
    | Some _ -> Assert.False(true, "unexpected help")
    | None -> Assert.False(true, "no docs for HashIdentity.FromFunctions")

[<Fact>]
let ``remarks is as expected for List.reduce`` () =
    let doc = Quoted.tryGetDocumentation <@ List.reduce @>

    match doc with
    | Some { Remarks = Some remarks } -> Assert.Equal("Raises System.ArgumentException if list is empty", remarks)
    | Some _ -> Assert.False(true, "unexpected help")
    | None -> Assert.False(true, "no docs for List.reduce")

[<Fact>]
let ``summary is as expected for Array.sortDescending`` () =
    let doc = Quoted.tryGetDocumentation <@ Array.sortDescending @>

    match doc with
    | Some { Summary = summary } ->
        Assert.Equal(
            "Sorts the elements of an array, in descending order, returning a new array. Elements are compared using Microsoft.FSharp.Core.Operators.compare.",
            summary
        )
    | None -> Assert.False(true, "no docs for Array.sortDescending")

[<Fact>]
let ``ReflectedDefinition works as expected`` () =
    let docReflected = TryGetDocumentation(id)
    let docQuoted = Quoted.tryGetDocumentation <@ id @>

    match docReflected, docQuoted with
    | Some r, Some q -> Assert.True((r = q))
    | _ -> Assert.False(true, "no docs for id")
