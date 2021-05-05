using System;

using Akka.Actor;

namespace WinTail
{
	/// <summary>
	///     Actor responsible for serializing message writes to the console.
	///     (write one message at a time, champ :)
	/// </summary>
	internal class ConsoleWriterActor : UntypedActor
	{
		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case Messages.SuccessInput successInput:
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"{Self.Path}: Correct! {successInput.Reason}");
					break;

				case Messages.ErrorInput error:
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"{Self.Path}: Error: {error.Reason}");
					break;
			}
			
			Console.ResetColor();
		}
	}
}
