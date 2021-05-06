namespace WinTail
{
	public class Messages
	{
		public class Start
		{
			public static readonly Start Value = new Start();
		}

		public class Continue
		{
			public static readonly Continue Value = new Continue();
		}

		public class Exit
		{
			public static readonly Exit Value = new Exit();
		}

		public class SuccessInput
		{
			public SuccessInput(string reason)
			{
				Reason = reason;
			}

			public string Reason { get; }
		}

		public class ValidateRequest
		{
			public ValidateRequest(string input)
			{
				Input = input;
			}

			public string Input { get; }
		}

		public class ErrorInput
		{
			public ErrorInput(string reason)
			{
				Reason = reason;
			}

			public string Reason { get; }
		}
	}
}