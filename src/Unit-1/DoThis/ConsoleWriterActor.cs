using System;

using Akka.Actor;

namespace WinTail
{
	internal class ConsoleWriterActor : UntypedActor
	{
		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case string str:
					Console.WriteLine(str);
					break;

				case Messages.SuccessInput successInput:
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"{Context.Sender.Path}: {successInput.Reason}");
					break;

				case Messages.ErrorInput error:
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"{Context.Sender.Path}: Error: {error.Reason}");
					break;
			}
			
			Console.ResetColor();
		}
	}
}
