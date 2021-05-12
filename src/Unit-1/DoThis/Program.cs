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

			myActorSystem.ActorOf(Props.Create<TailCoordinatorActor>(), "tailCoordinator");

			var validatorProps = Props.Create(() => new FileValidationActor(writer));
			myActorSystem.ActorOf(validatorProps, "validator");

			var readerProps = Props.Create(() => new ConsoleReaderActor());
			var reader = myActorSystem.ActorOf(readerProps, "reader");

			reader.Tell(Messages.Start.Value);
			myActorSystem.WhenTerminated.Wait();
		}
	}
}
