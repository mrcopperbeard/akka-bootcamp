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
					break;
				case EndTail end:
					break;
			}
		}

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