using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Akka.Actor;
using ChartApp.Actors;
using ChartApp.Messages;

namespace ChartApp
{
	public partial class Main : Form
	{
		private IActorRef _coordinatorActor;
		private IActorRef _chartActor;
		private readonly Dictionary<CounterType, IActorRef> _buttonToggleActors = new();

		public Main()
		{
			InitializeComponent();
		}

		private void Main_Load(object sender, EventArgs e)
		{
			_chartActor = Program.ChartActors.ActorOf(Props.Create(() => new ChartingActor(sysChart)), "charting");
			_chartActor.Tell(new ChartingActor.InitializeChart(null));

			var coordinatorProps = Props.Create(() => new PerformanceCounterCoordinatorActor(_chartActor));

			_coordinatorActor = Program.ChartActors.ActorOf(coordinatorProps, "counter-coordinator");

			_buttonToggleActors[CounterType.Cpu] = CreateBtnActor(CounterType.Cpu, cpuBtn);
			_buttonToggleActors[CounterType.Memory] = CreateBtnActor(CounterType.Memory, memoryBtn);
			_buttonToggleActors[CounterType.Disk] = CreateBtnActor(CounterType.Disk, discBtn);
		}

		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
			//shut down the charting actor
			_chartActor.Tell(PoisonPill.Instance);

			//shut down the ActorSystem
			Program.ChartActors.Terminate();
		}

		private IActorRef CreateBtnActor(CounterType counterType, Control button)
			=> Program.ChartActors.ActorOf(Props
				.Create(() => new ButtonToggleActor(counterType, _coordinatorActor, button))
				.WithDispatcher("akka.actor.synchronized-dispatcher"));

		private void cpuBtn_Click(object sender, EventArgs e)
		{
			_buttonToggleActors[CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
		}

		private void memoryBtn_Click(object sender, EventArgs e)
		{
			_buttonToggleActors[CounterType.Memory].Tell(new ButtonToggleActor.Toggle());
		}

		private void discBtn_Click(object sender, EventArgs e)
		{
			_buttonToggleActors[CounterType.Disk].Tell(new ButtonToggleActor.Toggle());
		}
	}
}
