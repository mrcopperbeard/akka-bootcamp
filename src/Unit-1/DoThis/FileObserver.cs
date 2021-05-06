using System;
using System.IO;

using Akka.Actor;

namespace WinTail
{
	public sealed class FileObserver : IDisposable
	{
		private readonly IActorRef _tailActor;

		private readonly string _absoluteFilePath;

		private FileSystemWatcher _watcher;

		public FileObserver(IActorRef tailActor, string absoluteFilePath)
		{
			_tailActor = tailActor;
			_absoluteFilePath = absoluteFilePath;
		}

		public void Start()
		{
			var fileDir = Path.GetDirectoryName(_absoluteFilePath)
				?? throw new ArgumentException($"{_absoluteFilePath} has no directory", nameof(_absoluteFilePath));

			var fileName = Path.GetFileName(_absoluteFilePath)
				?? throw new ArgumentException($"{_absoluteFilePath} has no directory", nameof(_absoluteFilePath));

			_watcher = new FileSystemWatcher(fileDir, fileName)
			{
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
			};

			_watcher.Changed += (_, args) =>
			{
				if (args.ChangeType == WatcherChangeTypes.Changed)
				{
					_tailActor.Tell(new TailActor.FileWrite(args.Name), ActorRefs.NoSender);
				}
			};

			_watcher.Error += (_, args) =>
			{
				_tailActor.Tell(new TailActor.FileError(fileName, args.GetException().Message), ActorRefs.NoSender);
			};

			_watcher.EnableRaisingEvents = true;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_watcher?.Dispose();
		}
	}
}