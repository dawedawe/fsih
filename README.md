Fsih
====

Fsih provides you with the `h` function, meant to be used in the F# repl [fsi](https://learn.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/).  
It's modeled after the `h` function in the Elixir [iex](https://hexdocs.pm/iex/1.16.0/IEx.html) repl.

To use it, just start an fsi session with `dotnet fsi`  
Load the package and open the namespace:
```fsharp
#r "nuget: Fsih";;
open Fsih;;
```

Apply h to any function wrapped in an FSharp [quotation](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/code-quotations) to get its documentation:
```fsharp
h <@ fst @>
```

```
Description:
Return the first element of a tuple, fst (a,b) = a.

Parameters:
- tuple: The input tuple.
Returns:
The first value.

Examples:
fst ("first", 2)  //  Evaluates to "first"

Full name: Microsoft.FSharp.Core.Operators.fst
Assembly: FSharp.Core.dll
```