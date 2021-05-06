using System;
using System.IO;

using Akka.Actor;

namespace WinTail
{
	public class FileValidationActor : UntypedActor
	{
		private readonly IActorRef _writer;
		private readonly IActorRef _tailCoordinator;

		public FileValidationActor(IActorRef writer, IActorRef tailCoordinator)
		{
			_writer = writer;
			_tailCoordinator = tailCoordinator;
		}

		/// <inheritdoc />
		protected override void OnReceive(object message)
		{
			var error = OnReceiveInternal(message);

			if (error != null)
			{
				_writer.Tell(error);
				Sender.Tell(Messages.Continue.Value);
			}
		}

		private Messages.ErrorInput OnReceiveInternal(object message)
		{
			if (message is Messages.ValidateRequest validationRequest)
			{
				var path = validationRequest.Input;

				if (string.IsNullOrEmpty(path))
				{
					return new Messages.ErrorInput("Path is null");
				}

				if (!File.Exists(path))
				{
					return new Messages.ErrorInput($"Path {path} does not exists");
				}

				_writer.Tell(new Messages.SuccessInput($"Starting processing {path}"));
				_tailCoordinator.Tell(new TailCoordinatorActor.StartTail(path, _writer));

				return null;
			}

			return new Messages.ErrorInput($"{Self.Path}: Unknown message type {message.GetType()}");
		}
	}
}