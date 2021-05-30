namespace ChartApp

open System.Collections.Generic
open System.Windows.Forms.DataVisualization.Charting
open Akka.Actor
open Akka.FSharp

[<AutoOpen>]
module Messages =
    type ChartMessage =
        | InitializeChart of initialSeries: Map<string, Series>
        | AddSeries of Series

/// Actors used to intialize chart data
[<AutoOpen>]
module Actors =
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
                not <| System.String.IsNullOrEmpty series.Name &&
                not <| mapping.ContainsKey series.Name ->
                    let newMapping = mapping.Add (series.Name, series)
                    chart.Series.Add series
                    return! charting newMapping
        }

        charting Map.empty<string, Series>



