using System.IO;
using Akka.Actor;

namespace WinTail
{
	public class TailActor : UntypedActor
	{
		private readonly string _filePath;

		private readonly IActorRef _reporter;

		private readonly FileObserver _observer;

		private readonly Stream _fileStream;

		private readonly StreamReader _fileStreamReader;

		public TailActor(string filePath, IActorRef reporter)
		{
			_filePath = filePath;
			_reporter = reporter;
			_observer = new FileObserver(Self, Path.GetFullPath(filePath));
		}

		/// <inheritdoc />
		protected override void OnReceive(object message)
		{
			throw new System.NotImplementedException();
		}

		public class FileWrite
		{
			public FileWrite(string filePath)
			{
				FilePath = filePath;
			}

			public string FilePath { get; }
		}

		public class FileError
		{
			public FileError(string fileName, string reason)
			{
				FileName = fileName;
				Reason = reason;
			}

			public string FileName { get; }

			public string Reason { get; }
		}

		public class InitialRead
		{
			public InitialRead(string fileName, string text)
			{
				FileName = fileName;
				Text = text;
			}

			public string FileName { get; }

			public string Text { get; }
		}
	}
}