using System.IO;
using System.Text;
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
			var fullPath = Path.GetFullPath(filePath);

			_filePath = filePath;
			_reporter = reporter;
			_observer = new FileObserver(Self, fullPath);
			_observer.Start();

			reporter.Tell("Observer started");

			_fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			_fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);

			var text = _fileStreamReader.ReadToEnd();
			Self.Tell(new InitialRead(_filePath, text));
		}

		/// <inheritdoc />
		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case InitialRead initialRead:
					_reporter.Tell(initialRead.Text);
					break;

				case FileWrite _:
					var text = _fileStreamReader.ReadToEnd();
					if (!string.IsNullOrEmpty(text))
					{
						_reporter.Tell(text);
					}

					break;

				case FileError fileError:
					_reporter.Tell($"Tail error: {fileError.Reason}");
					break;
			}
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