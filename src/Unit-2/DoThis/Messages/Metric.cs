namespace ChartApp.Messages
{
	public class Metric
	{
		public Metric(string series, float counterValue)
		{
			CounterValue = counterValue;
			Series = series;
		}

		public string Series { get; }

		public float CounterValue { get; }
	}
}