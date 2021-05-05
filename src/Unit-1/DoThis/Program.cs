using System;

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

			var validatorProps = Props.Create(() => new ValidationActor(writer));
			var validator = myActorSystem.ActorOf(validatorProps, "validator");

			var readerProps = Props.Create(() => new ConsoleReaderActor(validator));
			var reader = myActorSystem.ActorOf(readerProps, "reader");

			reader.Tell(Messages.Start.Value);
			myActorSystem.WhenTerminated.Wait();
		}
	}
}
