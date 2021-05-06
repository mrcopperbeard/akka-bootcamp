using System;

using Akka.Actor;

namespace WinTail
{
	/// <summary>
	///     Actor responsible for reading FROM the console.
	///     Also responsible for calling <see cref="ActorSystem.Terminate" />.
	/// </summary>
	internal class ConsoleReaderActor : UntypedActor
	{
		public const string ExitCommand = "exit";

		private readonly IActorRef _validator;

		public ConsoleReaderActor(IActorRef validator)
		{
			_validator = validator;
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case Messages.Exit _:
					Context.System.Terminate();
					break;

				case Messages.Continue _:
					OnContinue();
					break;

				case Messages.Start _:
					PrintInstructions();
					OnContinue();
					break;

				default:
					Console.WriteLine($"{Self.Path}: Unknown message type {message.GetType()}");
					break;
			}
		}

		private static void PrintInstructions()
		{
			Console.WriteLine("Please provide the URI of a log file on disk.\n");
		}

		private void OnContinue()
		{
			var msg = Console.ReadLine();

			if (msg == ExitCommand)
			{
				Self.Tell(Messages.Exit.Value);

				return;
			}

			_validator.Tell(new Messages.ValidateRequest(msg));
		}
	}
}