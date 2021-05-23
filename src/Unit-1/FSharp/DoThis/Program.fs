open System
open Akka.FSharp
open Akka.FSharp.Spawn
open Akka.Actor
open WinTail

[<EntryPoint>]
let main argv = 
    // initialize an actor system
    // YOU NEED TO FILL IN HERE
    let myActorSystem =
        Configuration.load () 
        |> System.create "my-actor-system"

    let writer = actorOf Actors.consoleWriterActor |> spawn myActorSystem "writer"
    let validator = actorOf2 (Actors.validationActor writer) |> spawn myActorSystem "validator"
    let reader = actorOf2 (Actors.consoleReaderActor validator) |> spawn myActorSystem "reader"

    reader <! Actors.Start

    myActorSystem.WhenTerminated.Wait ()
    0
