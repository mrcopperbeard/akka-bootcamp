using System;
using Akka.Actor;

namespace WinTail
{
	public class ValidationActor : UntypedActor
	{
		private readonly IActorRef _writer;

		public ValidationActor(IActorRef writer)
		{
			_writer = writer;
		}

		/// <inheritdoc />
		protected override void OnReceive(object message)
		{
			if (message is Messages.ValidateRequest validationRequest)
			{
				var msg = validationRequest.Input;

				if (string.IsNullOrEmpty(msg))
				{
					_writer.Tell(Messages.ErrorInput.NullInput);
				}
				else if (msg.Contains('x'))
				{
					_writer.Tell(Messages.ErrorInput.InvalidInput);
				}
				else
				{
					_writer.Tell(new Messages.SuccessInput($"\"{msg}\" is valid"));
				}
			}
			else
			{
				Console.WriteLine($"{Self.Path}: Unknown message type {message.GetType()}");
			}

			Sender.Tell(Messages.Continue.Value);
		}
	}
}