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
    let buttonToggleActor coordinatorActor (button: Windows.Forms.Button) counterType (mailbox: Actor<_>) =
        let flip isOn =
            let toggled = not isOn
            button.Text <- sprintf "%s (%s)" (counterType.ToString().ToUpperInvariant()) (if toggled then "ON" else "OFF")
            toggled
        
        let rec loop isToggled = actor {
            let! message = mailbox.Receive()
            match message with
            | Toggle when isToggled -> coordinatorActor <! Unwatch counterType
            | Toggle when not isToggled -> coordinatorActor <! Watch counterType
            | unhandled -> mailbox.Unhandled unhandled

            return! isToggled |> flip |> loop
        }

        loop false

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
            | Subscribe subscriber ->
                return! subscriptions
                |> List.filter (fun s -> s <> subscriber)
                |> fun tail -> subscriber :: tail
                |> loop
            | Unsubscribe subscriber ->
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
            Memory, fun _ -> new Series(string Memory, ChartType = SeriesChartType.FastLine, Color = Color.MediumBlue)
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
        let maxPoints = 250

        let setChartBoundaries (mapping: Map<string, Series>) (numberOfPoints: int) =
            let allPoints =
                    mapping
                    |> Map.toList
                    |> Seq.collect (fun (_, series) -> series.Points)
                    |> (fun points -> HashSet<DataPoint>(points))
                    
            if allPoints |> Seq.length > 2 then
                let yValues = allPoints |> Seq.collect (fun p -> p.YValues) |> Seq.toList
                chart.ChartAreas.[0].AxisX.Maximum <- float numberOfPoints
                chart.ChartAreas.[0].AxisX.Minimum <- (float numberOfPoints - float maxPoints)
                chart.ChartAreas.[0].AxisY.Maximum <- if List.length yValues > 0 then Math.Ceiling(List.max yValues) else 1.
                chart.ChartAreas.[0].AxisY.Minimum <- if List.length yValues > 0 then Math.Floor(List.min yValues) else 0.

        let rec charting (mapping: Map<string, Series>) (numberOfPoints: int) = actor {
            let! message = mailbox.Receive()

            let isNewSeries (series: string) =
                not <| String.IsNullOrEmpty series &&
                not <| mapping.ContainsKey series 

            match message with
            | InitializeChart series ->
                chart.Series.Clear ()
                series |> Map.iter (fun k v ->
                    v.Name <- k
                    chart.Series.Add(v))
                return! charting series numberOfPoints
            | AddSeries series when isNewSeries series.Name ->
                    let newMapping = mapping.Add (series.Name, series)
                    chart.Series.Add series
                    setChartBoundaries newMapping numberOfPoints

                    return! charting newMapping numberOfPoints
            | RemoveSeries series ->
                chart.Series.Remove(mapping.[series]) |> ignore
                let newMapping = mapping.Remove series
                setChartBoundaries newMapping numberOfPoints

                return! charting newMapping numberOfPoints
            | Metric(seriesName, counterValue) ->
                let newNumPofPoints = numberOfPoints + 1
                let series = mapping.[seriesName]
                series.Points.AddXY (numberOfPoints, counterValue) |> ignore
                while (series.Points.Count > maxPoints) do series.Points.RemoveAt 0
                setChartBoundaries mapping newNumPofPoints

                return! charting mapping newNumPofPoints
            | m -> mailbox.Unhandled m
        }

        charting Map.empty<string, Series> 0



