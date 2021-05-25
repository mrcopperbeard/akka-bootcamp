namespace WinTail

open System
open System.IO
open Akka.Actor
open Akka.FSharp
open Messages

module Actors =
    type Command = 
    | Start
    | Continue
    | Message of string
    | Exit

    // At the top of Actors.fs, before consoleReaderActor
    // Print instructions to the console
    let doPrintInstructions () =
        Console.WriteLine "Please provide the URI of a log file on disk.\n"

    let (|Message|Exit|) (str:string) =
        match str.ToLower() with
        | "exit" -> Exit
        | _ -> Message(str)

    let fileValidationActor (consoleWriter: IActorRef) (mailbox: Actor<_>) path =
        let (|IsFileExists|_|) path = if File.Exists path then Some path else None

        let (|EmptyMessage|Message|) (str: string) =
            match str.Length with
            | 0 -> EmptyMessage
            | _ -> Message(str)

        match path with
        | EmptyMessage ->
            consoleWriter <! ErrorInput ("Input is blank", Null)
            mailbox.Sender () <! Continue
        | IsFileExists _ ->
            consoleWriter <! ValidInput $"Starting process {path}"
            select "/user/tail-coordinator" mailbox.Context <! StartTail(path, consoleWriter)
        | _ ->
            consoleWriter <! ErrorInput($"Path {path} is not existing path", Validation)
            mailbox.Sender () <! Continue

    let consoleReaderActor (validationActor: IActorRef) (mailbox: Actor<_>) message =
        let getAndValidateInput () =
            let line = Console.ReadLine ()
            match line with
            | Exit -> mailbox.Context.System.Terminate () |> ignore
            | Message input -> validationActor <! input
                
        match box message with
        | :? Command as command ->
            match command with
            | Start -> doPrintInstructions ()
            | _ -> ()
        | _ -> ()

        getAndValidateInput ()

    let consoleWriterActor message = 
        let printInColor color message =
            Console.ForegroundColor <- color
            Console.WriteLine (message.ToString ())
            Console.ResetColor ()

        match box message with
        | :? InputResult as input ->
            match input with
            | ValidInput(msg) -> printInColor ConsoleColor.Green msg
            | ErrorInput(err, _) -> printInColor ConsoleColor.Red err
        | other -> printInColor ConsoleColor.Yellow (other.ToString())

    let tailActor path (reporterActor: IActorRef) (mailbox: Actor<_>) =
        let fullPath = Path.GetFullPath (path)
        let observer = new FileUtility.FileObserver(mailbox.Self, fullPath)
        observer.Start()
        let fileStream = new FileStream (fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        let streamReader = new StreamReader (fileStream)
        let text = streamReader.ReadToEnd()
        mailbox.Self <! InitialRead(path, text)
        mailbox.Defer <| fun () ->
            streamReader.Dispose()
            fileStream.Dispose()
            (observer :> IDisposable).Dispose()
            reporterActor <! "Disposed"

        let rec loop () = actor {
            let! message = mailbox.Receive()
            match (box message) :?> FileCommand with
            | FileWrite _ ->
                let text = streamReader.ReadToEnd()
                if String.IsNullOrEmpty text then ()
                else reporterActor <! text
            | FileError (_, reason) -> reporterActor <! ErrorInput(reason, ReadError)
            | InitialRead(_, initial)  -> reporterActor <! initial

            return! loop()
        }

        loop()

    let tailCoordinatorActor (mailbox: Actor<_>) message =
        match message with
        | StartTail(path, reporter) ->
            tailActor path reporter
            |> spawn mailbox.Context "tail"
            |> ignore // Вызывает Initial Read при старте
        | StopTail(_) ->
            mailbox.Context.GetChildren()
            |> Seq.iter mailbox.Context.Stop