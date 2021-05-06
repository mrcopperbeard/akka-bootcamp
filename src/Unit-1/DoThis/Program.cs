﻿using Akka.Actor;

namespace WinTail
{
	internal class Program
	{
		private static void Main(string[] _)
		{
			var myActorSystem = ActorSystem.Create("my-actor-system");

			var writerProps = Props.Create<ConsoleWriterActor>();
			var writer = myActorSystem.ActorOf(writerProps, "writer");

			var tailCoordinator = myActorSystem.ActorOf(Props.Create<TailCoordinatorActor>(), "tailCoordinator");

			var validatorProps = Props.Create(() => new FileValidationActor(writer, tailCoordinator));
			var validator = myActorSystem.ActorOf(validatorProps, "validator");

			var readerProps = Props.Create(() => new ConsoleReaderActor(validator));
			var reader = myActorSystem.ActorOf(readerProps, "reader");

			reader.Tell(Messages.Start.Value);
			myActorSystem.WhenTerminated.Wait();
		}
	}
}
