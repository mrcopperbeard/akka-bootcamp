using Akka.Actor;

namespace ChartApp.Messages
{
	public class SubscribeCounter
	{
		public SubscribeCounter(CounterType counter, IActorRef subscriber)
		{
			Counter = counter;
			Subscriber = subscriber;
		}

		public CounterType Counter { get; }

		public IActorRef Subscriber { get; }
	}
}