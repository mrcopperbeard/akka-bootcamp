namespace ChartApp

open Akka.Actor
open Akka.FSharp
open System.Drawing
open System.Windows.Forms
open System.Windows.Forms.DataVisualization.Charting

open Akka.Util.Internal

[<AutoOpen>]
module Form =
    let inline (^) f x y = f x y
    let sysChart = new Chart(Name = "sysChart", Text = "sysChart", Dock = DockStyle.Fill, Location = Point(0, 0), Size = Size(484, 446), TabIndex = 0)
    let form = new Form(Name = "Main", Visible = true, Text = "System Metrics", AutoScaleDimensions = SizeF(6.F, 13.F), AutoScaleMode = AutoScaleMode.Font, ClientSize = Size(684, 446))
    let chartArea1 = new ChartArea(Name = "ChartArea1")
    let legend1 = new Legend(Name = "Legend1")

    let btnAddSeries = new Button(Name = "btnAddSeries", Text = "Add Series", Location = Point(540, 300), Size = Size(100, 40), TabIndex = 1, UseVisualStyleBackColor = true)
    let btnCpu = new Button(Name = "btnCpu", Text = "CPU (ON)", Location = Point(540, 275), Size = Size(100, 40), TabIndex = 1, UseVisualStyleBackColor = true)
    let btnMemory = new Button(Name = "btnMemory", Text = "MEMORY (OFF)", Location = Point(540, 320), Size = Size(100, 40), TabIndex = 2, UseVisualStyleBackColor = true)
    let btnDisk = new Button(Name = "btnDisk", Text = "DISK (OFF)", Location = Point(540, 365), Size = Size(100, 40), TabIndex = 3, UseVisualStyleBackColor = true)

    sysChart.BeginInit ()
    form.SuspendLayout ()
    sysChart.ChartAreas.Add chartArea1
    sysChart.Legends.Add legend1

    form.Controls.Add sysChart
    form.Controls.Add btnCpu
    form.Controls.Add btnMemory
    form.Controls.Add btnDisk
    sysChart.EndInit ()
    form.ResumeLayout false

    let load (myActorSystem:ActorSystem) =
        let chartActor = chartingActor sysChart |> spawn myActorSystem "charting"
        let coordinatorActor = performanceCounterCoordinatorActor chartActor |> spawn myActorSystem "coordinator"
        let toggleActors = 
            [(Cpu, btnCpu); (Memory, btnMemory); (Disc, btnDisk)]
            |> List.map ^ fun (t, btn) ->
                let actorName = $"toggle-{t.ToString().ToLowerInvariant()}"
                let actor = 
                    buttonToggleActor coordinatorActor btn t 
                    |> fun a -> spawnOpt myActorSystem actorName a [Dispatcher("akka.actor.synchronized-dispatcher")]

                (t, actor)
            |> Map.ofList

        toggleActors.[Cpu] <! Toggle

        btnCpu.Click.Add (fun _ -> toggleActors.[Cpu] <! Toggle )
        btnMemory.Click.Add (fun _ -> toggleActors.[Memory] <! Toggle )
        btnDisk.Click.Add (fun _ -> toggleActors.[Disc] <! Toggle )
        form