module Fsih.Parser

open System
open System.IO
open System.Xml
open Spectre.Console

type Help =
    { Summary: string
      Remarks: string option
      Parameters: (string * string) list
      Returns: string option
      Exceptions: (string * string) list
      Examples: (string * string) list
      FullName: string
      Assembly: string }

    member this.Print() =
        let parameters =
            this.Parameters
            |> List.map (fun (name, description) -> sprintf "- %s: %s" name description)
            |> String.concat "\n"

        AnsiConsole.Markup("\n[bold]Description:[/]\n{0}\n", Markup.Escape(this.Summary))

        match this.Remarks with
        | Some r -> AnsiConsole.Markup("\n[bold]Remarks:[/]\n{0}\n", Markup.Escape(r))
        | None -> ()

        if not (String.IsNullOrWhiteSpace(parameters)) then
            AnsiConsole.Markup("\n[bold]Parameters:[/]\n{0}\n", Markup.Escape(parameters))

        match this.Returns with
        | Some r -> AnsiConsole.Markup("[bold]Returns:[/]\n{0}\n", Markup.Escape(r))
        | None -> ()

        if not this.Exceptions.IsEmpty then
            AnsiConsole.Markup("\n[bold]Exceptions:[/]\n")

            for (exType, exDesc) in this.Exceptions do
                AnsiConsole.Markup("[blue]{0}[/]: ", Markup.Escape(exType))
                printfn $"%s{exDesc}"

        if not this.Examples.IsEmpty then
            AnsiConsole.Markup("\n[bold]Examples:[/]\n")

            for example, desc in this.Examples do
                AnsiConsole.Markup("[blue]{0}[/]\n", Markup.Escape(example))

                if not (String.IsNullOrWhiteSpace(desc)) then
                    printfn $"""// {desc.Replace("\n", "\n// ")}"""

                printfn ""

        AnsiConsole.Markup("[bold]Full name:[/] {0}\n", Markup.Escape(this.FullName))
        AnsiConsole.Markup("[bold]Assembly:[/] {0}\n", Markup.Escape(this.Assembly))
        printfn ""

let cleanupXmlContent (s: string) = s.Replace("\n ", "\n").Trim() // some stray whitespace from the XML

// remove any leading `X:` and trailing `N
let trimDotNet (s: string) =
    let s = if s.Length > 2 && s[1] = ':' then s.Substring(2) else s
    let idx = s.IndexOf('`')
    let s = if idx > 0 then s.Substring(0, idx) else s
    s

// WTF, seems like we loose inner xml (example content) if we cache an XmlDocument
let xmlDocCache = Collections.Generic.Dictionary<string, string>()

let getXmlDocument xmlPath =
#if DEBUG
    printfn $"xml file: %s{xmlPath}"
#endif
    match xmlDocCache.TryGetValue(xmlPath) with
    | true, value ->
        let xmlDocument = XmlDocument()
        xmlDocument.LoadXml(value)
        xmlDocument
    | _ ->
        let rawXml = File.ReadAllText(xmlPath)
        let xmlDocument = XmlDocument()
        xmlDocument.LoadXml(rawXml)
        xmlDocCache.Add(xmlPath, rawXml)
        xmlDocument

let getTexts (node: Xml.XmlNode) =
    seq {
        for child in node.ChildNodes do
            if child.Name = "#text" then
                yield child.Value

            if child.Name = "c" then
                yield child.InnerText

            if child.Name = "see" then
                let cref = child.Attributes.GetNamedItem("cref")

                if not (isNull cref) then
                    yield cref.Value |> trimDotNet
    }
    |> String.concat ""

let helpText (xmlPath: string) (assembly: string) (modName: string) (implName: string) (sourceName: string) =
    let sourceName = sourceName.Replace('.', '#') // for .ctor
    let implName = implName.Replace('.', '#') // for .ctor
    let xmlName = $"{modName}.{implName}"
    let xmlDocument = getXmlDocument xmlPath

    let node =
        let toTry =
            [ $"""/doc/members/member[contains(@name, ":{xmlName}`")]"""
              $"""/doc/members/member[contains(@name, ":{xmlName}(")]"""
              $"""/doc/members/member[contains(@name, ":{xmlName}")]""" ]

        seq {
            for t in toTry do
#if DEBUG
                printfn "trying xpath: %s" t
#endif
                let node = xmlDocument.SelectSingleNode(t)
                if not (isNull node) then Some node else None
        }
        |> Seq.tryPick id

    match node with
    | None ->
#if DEBUG
        printfn $"// No node found for {xmlName}"
#endif
        None
    | Some n ->
        let summary =
            n.SelectSingleNode("summary")
            |> Option.ofObj
            |> Option.map getTexts
            |> Option.map cleanupXmlContent

        let remarks =
            n.SelectSingleNode("remarks")
            |> Option.ofObj
            |> Option.map getTexts
            |> Option.map cleanupXmlContent

        let parameters =
            n.SelectNodes("param")
            |> Seq.cast<XmlNode>
            |> Seq.map (fun n -> n.Attributes.GetNamedItem("name").Value.Trim(), n.InnerText.Trim())
            |> List.ofSeq

        let returns =
            n.SelectSingleNode("returns")
            |> Option.ofObj
            |> Option.map (fun n -> getTexts(n).Trim())

        let exceptions =
            n.SelectNodes("exception")
            |> Seq.cast<XmlNode>
            |> Seq.map (fun n ->
                let exType = n.Attributes.GetNamedItem("cref").Value
                let idx = exType.IndexOf(':')
                let exType = if idx >= 0 then exType.Substring(idx + 1) else exType
                exType.Trim(), n.InnerText.Trim())
            |> List.ofSeq

        let examples =
            n.SelectNodes("example")
            |> Seq.cast<XmlNode>
            |> Seq.map (fun n ->
                let codeNode = n.SelectSingleNode("code")

                let code =
                    if isNull codeNode then
                        ""
                    else
                        n.RemoveChild(codeNode) |> ignore
                        cleanupXmlContent codeNode.InnerText

                code, cleanupXmlContent n.InnerText)
            |> List.ofSeq

        match summary with
        | Some s ->
            { Summary = s
              Remarks = remarks
              Parameters = parameters
              Returns = returns
              Exceptions = exceptions
              Examples = examples
              FullName = $"{modName}.{sourceName}" // the long ident as users see it
              Assembly = assembly }
            |> Some
        | None -> None
