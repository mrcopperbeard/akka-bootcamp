using System;
using Akka.Actor;

namespace WinTail
{
	public class TailCoordinatorActor : UntypedActor
	{
		/// <inheritdoc />
		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case StartTail start:
					var tailProps = Props.Create(() => new TailActor(start.FilePath, start.ReporterActor));
					Context.ActorOf(tailProps); // Sends message to itself in ctor...
					break;
				case EndTail _:
					break;
			}
		}

		protected override SupervisorStrategy SupervisorStrategy()
			=> new OneForOneStrategy(
				10,
				2000,
				err => err switch
				{
					NotSupportedException => Directive.Stop,
					_ => Directive.Restart,
				});

		public class StartTail
		{
			public StartTail(string filePath, IActorRef reporterActor)
			{
				FilePath = filePath;
				ReporterActor = reporterActor;
			}

			public string FilePath { get; }

			public IActorRef ReporterActor { get; }
		}

		public class EndTail
		{
			public EndTail(string filePath)
			{
				FilePath = filePath;
			}

			public string FilePath { get; }
		}
	}
}