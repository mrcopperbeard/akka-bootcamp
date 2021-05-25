open System
open Akka.FSharp
open Akka.FSharp.Spawn
open Akka.Actor
open WinTail

[<EntryPoint>]
let main argv = 
    let myActorSystem = Configuration.load () |> System.create "my-actor-system"
    let strategy =
        fun (ex: exn) ->
            match ex with
            | :? ArithmeticException -> Directive.Resume
            | :? NotSupportedException -> Directive.Stop
            | _ -> Directive.Restart
        |> fun cb -> Strategy.OneForOne(cb, 10, TimeSpan.FromSeconds(30.))

    let tailCoordinator = spawnOpt myActorSystem "tail-coordinator" (actorOf2 Actors.tailCoordinatorActor) [
        SpawnOption.SupervisorStrategy(strategy)
    ]

    let writer = actorOf Actors.consoleWriterActor |> spawn myActorSystem "writer"
    let validator = actorOf2 (Actors.fileValidationActor writer) |> spawn myActorSystem "validator"
    let reader = actorOf2 (Actors.consoleReaderActor validator) |> spawn myActorSystem "reader"

    reader <! Actors.Start

    myActorSystem.WhenTerminated.Wait ()
    0
