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
			Console.WriteLine("Write whatever you want into the console!");
			Console.Write("Some lines will appear as");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write(" red ");
			Console.ResetColor();
			Console.Write(" and others will appear as");
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write(" green! ");
			Console.ResetColor();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine($"Type '{ExitCommand}' to quit this application at any time.\n");
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