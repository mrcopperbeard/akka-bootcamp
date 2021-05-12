using System;
using System.IO;

using Akka.Actor;

namespace WinTail
{
	public class FileValidationActor : UntypedActor
	{
		private readonly IActorRef _writer;

		public FileValidationActor(IActorRef writer)
		{
			_writer = writer;
		}

		/// <inheritdoc />
		protected override void OnReceive(object message)
		{
			var (path, error) = OnReceiveInternal(message);

			if (error != null)
			{
				_writer.Tell(error);
				Sender.Tell(Messages.Continue.Value);
			}
			else
			{
				var startTail = new TailCoordinatorActor.StartTail(path, _writer);

				Context.ActorSelection("akka://my-actor-system/user/tailCoordinator").Tell(startTail);
			}
		}

		private (string path, Messages.ErrorInput error) OnReceiveInternal(object message)
		{
			if (message is Messages.ValidateRequest validationRequest)
			{
				var path = validationRequest.Input;

				if (string.IsNullOrEmpty(path))
				{
					return (null, new Messages.ErrorInput("Path is null"));
				}

				if (!File.Exists(path))
				{
					return (null, new Messages.ErrorInput($"Path {path} does not exists"));
				}

				_writer.Tell(new Messages.SuccessInput($"Starting processing {path}"));

				return (path, null);
			}

			return (null, new Messages.ErrorInput($"{Self.Path}: Unknown message type {message.GetType()}"));
		}
	}
}