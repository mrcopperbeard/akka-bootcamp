namespace WinTail

open System
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
        Console.WriteLine "Write whatever you want into the console!"
        Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
        Console.WriteLine "Type 'exit' to quit this application at any time.\n"

    let (|Message|Exit|) (str:string) =
        match str.ToLower() with
        | "exit" -> Exit
        | _ -> Message(str)

    let validationActor (consoleWriter: IActorRef) (mailbox: Actor<_>) message =
        let (|EmptyMessage|EvenMessage|OddMessage|) (str: string) =
            match str.Length, str.Length % 2 with
            | 0, _ -> EmptyMessage
            | _, 0 -> EvenMessage
            | _ -> OddMessage

        match message with
        | EmptyMessage -> consoleWriter <! ErrorInput ("Input is empty", Null)
        | EvenMessage -> consoleWriter <! ValidInput (sprintf "Input %s is correct!" message)
        | OddMessage -> consoleWriter <! ErrorInput ($"Input {message} has odd number of symbols", Validation)

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
