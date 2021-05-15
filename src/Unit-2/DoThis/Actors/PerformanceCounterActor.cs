using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;
using ChartApp.Messages;

namespace ChartApp.Actors
{
	public class PerformanceCounterActor : ReceiveActor
	{
		private readonly Func<PerformanceCounter> _createCounter;

		private readonly ICancelable _cancelPublish;

		private readonly HashSet<IActorRef> _subscribers;

		private PerformanceCounter _counter;

		public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> createCounter)
		{
			_createCounter = createCounter;
			_subscribers = new HashSet<IActorRef>();
			_cancelPublish = new Cancelable(Context.System.Scheduler);

			Receive<GatherMetrics>(message =>
			{
				var metric = new Metric(seriesName, _counter.NextValue());

				foreach (var subscriber in _subscribers)
				{
					subscriber.Tell(metric);
				}
			});

			Receive<SubscribeCounter>(message =>
			{
				_subscribers.Add(message.Subscriber);
			});

			Receive<UnsubscribeCounter>(message =>
			{
				_subscribers.Remove(message.Subscriber);
			});
		}

		protected override void PreStart()
		{
			_counter = _createCounter();

			var delay = TimeSpan.FromMilliseconds(250);

			Context.System.Scheduler.ScheduleTellRepeatedly(
				delay,
				delay,
				Self,
				new GatherMetrics(),
				Self,
				_cancelPublish);
		}

		protected override void PostStop()
		{
			_cancelPublish.Cancel();
			_subscribers.Clear();
			_counter.Dispose();
		}
	}
}