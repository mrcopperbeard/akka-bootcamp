namespace ChartApp

open System
open System.Collections.Generic
open System.Diagnostics
open System.Drawing
open System.Windows.Forms.DataVisualization.Charting

open Akka.Actor
open Akka.FSharp

[<AutoOpen>]
module Messages =
    type ChartMessage =
        | InitializeChart of initialSeries: Map<string, Series>
        | AddSeries of Series
        | RemoveSeries of string
        | Metric of series: string * counterValue: float

    type CounterType =
        | Cpu
        | Memory
        | Disc

    type CounterMessage =
        | Subscribe of IActorRef
        | Unsubscribe of IActorRef
        | GatherMetric

    type CoordinatorMessage =
        | Watch of CounterType
        | Unwatch of CounterType

    type ButtomnMessage = Toggle

/// Actors used to intialize chart data
[<AutoOpen>]
module Actors =
    let performanceCounterActor
        (seriesName: string)
        (performanceCounterGenerator: unit -> PerformanceCounter)
        (mailbox: Actor<_>) =

        let counter = performanceCounterGenerator()
        let gather =
            mailbox.Context.System.Scheduler.ScheduleTellRepeatedlyCancelable (
                TimeSpan.FromMilliseconds 250.,
                TimeSpan.FromMilliseconds 250.,
                mailbox.Self,
                GatherMetric,
                ActorRefs.NoSender)

        mailbox.Defer(fun () ->
            gather.Cancel()
            counter.Dispose())

        let rec loop subscriptions = actor {
            let! message = mailbox.Receive()
            match box message :?> CounterMessage with
            | Subscribe(subscriber) ->
                return! subscriptions
                |> List.filter (fun s -> s <> subscriber)
                |> fun tail -> subscriber :: tail
                |> loop
            | Unsubscribe(subscriber) ->
                return! subscriptions
                |> List.filter (fun s -> s <> subscriber)
                |> loop
            | GatherMetric ->
                let msg = Metric(seriesName, counter.NextValue() |> float)
                subscriptions |> List.iter (fun s -> s <! msg)
                return! loop subscriptions
        }

        loop []

    let performanceCounterCoordinatorActor chartingActor (mailbox: Actor<_>) =
        let counterGenerators = Map.ofList [
            Cpu, fun () -> new PerformanceCounter("Processor", "% Processor Time", "_Total", true)
            Memory, fun () -> new PerformanceCounter("Memory", "% Committed Bytes In Use", true)
            Disc, fun () -> new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)
        ]

        let counterSeries = Map.ofList [
            Cpu, fun _ -> new Series(string Cpu, ChartType = SeriesChartType.SplineArea, Color = Color.DarkGreen)
            Memory, fun _ -> new Series(string Memory, SeriesChartType.FastLine, Color = Color.MediumBlue)
            Disc, fun _ -> new Series(string Disc, ChartType = SeriesChartType.SplineArea, Color = Color.DarkRed)
        ]

        let rec loop (counterActors: Map<CounterType, IActorRef>) = actor {
            let! message = mailbox.Receive()
            match message with
            | Watch counter when counterActors.ContainsKey(counter) |> not ->
                let counterName = string counter
                let counterActor =
                    performanceCounterActor counterName counterGenerators.[counter]
                    |> spawn mailbox.Context $"counter-{counterName}"
                let newCounterActors = counterActors.Add(counter, counterActor)
                chartingActor <! (AddSeries <| counterSeries.[counter]())
                counterActor <! Subscribe(chartingActor)
                return! loop newCounterActors
            | Watch counter ->
                chartingActor <! (AddSeries <| counterSeries.[counter]())
                counterActors.[counter] <! Subscribe(chartingActor)
            | Unwatch counter ->
                counterSeries.[counter]()
                |> fun s -> s.Name
                |> RemoveSeries
                |> fun m -> chartingActor <! m
                counterActors.[counter] <! Unsubscribe(chartingActor)
            return! loop counterActors
        }

        loop Map.empty<CounterType, IActorRef>

    let chartingActor (chart: Chart) (mailbox: Actor<_>) =
        let rec charting (mapping: Map<string, Series>) = actor {
            let! message = mailbox.Receive()
            match message with
            | InitializeChart series ->
                chart.Series.Clear ()
                series |> Map.iter (fun k v ->
                    v.Name <- k
                    chart.Series.Add(v))
                return! charting series
            | AddSeries series when
                not <| String.IsNullOrEmpty series.Name &&
                not <| mapping.ContainsKey series.Name ->
                    let newMapping = mapping.Add (series.Name, series)
                    chart.Series.Add series
                    return! charting newMapping
        }

        charting Map.empty<string, Series>



