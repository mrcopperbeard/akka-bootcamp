using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;
using ChartApp.Messages;

namespace ChartApp.Actors
{
	public class PerformanceCounterCoordinatorActor : ReceiveActor
	{
		public PerformanceCounterCoordinatorActor(IActorRef chartingActor)
		{
			IDictionary<CounterType, IActorRef> counterActors = new Dictionary<CounterType, IActorRef>();

			Receive<Watch>(message =>
			{
				var counterType = message.CounterType;

				if (!counterActors.TryGetValue(message.CounterType, out var counterActor))
				{
					counterActor = Context.ActorOf(CreateProps(counterType));

					counterActors[counterType] = counterActor;
				}

				var series = CreateSeries(counterType);

				chartingActor.Tell(new ChartingActor.AddSeries(series));
				counterActor.Tell(new SubscribeCounter(counterType, chartingActor));
			});

			Receive<Unwatch>(message =>
			{
				var counterType = message.CounterType;

				if (!counterActors.TryGetValue(counterType, out var counterActor))
				{
					return;
				}

				chartingActor.Tell(new ChartingActor.RemoveSeries(counterType.ToString()));
				counterActor.Tell(new UnsubscribeCounter(counterType, chartingActor));

				counterActors.Remove(counterType);
			});
		}

		private static Props CreateProps(CounterType counterType)
			=> Props.Create(() => new PerformanceCounterActor(
				counterType.ToString(),
				() => CreateCounter(counterType)));

		private static PerformanceCounter CreateCounter(CounterType counterType)
		{
			return counterType switch
			{
				CounterType.Cpu => new PerformanceCounter("Processor", "% Processor Time", "_Total", true),
				CounterType.Memory => new PerformanceCounter("Memory", "% Committed Bytes In Use", true),
				CounterType.Disk => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true),
				_ => throw new NotSupportedException($"{nameof(CounterType)} with value {counterType} is not supported")
			};
		}

		private static Series CreateSeries(CounterType counterType)
		{
			return counterType switch
			{
				CounterType.Cpu => new Series(counterType.ToString())
				{
					Color = Color.DarkRed,
					ChartType = SeriesChartType.SplineArea,
				},
				CounterType.Memory => new Series(counterType.ToString())
				{
					Color = Color.MediumBlue,
					ChartType = SeriesChartType.FastLine,
				},
				CounterType.Disk => new Series(counterType.ToString())
				{
					Color = Color.DarkGreen,
					ChartType = SeriesChartType.SplineArea,
				},
				_ => throw new NotSupportedException($"{nameof(CounterType)} with value {counterType} is not supported")
			};
		}

		public class Watch
		{
			public Watch(CounterType counterType)
			{
				CounterType = counterType;
			}

			public CounterType CounterType { get; }
		}

		public class Unwatch
		{
			public Unwatch(CounterType counterType)
			{
				CounterType = counterType;
			}

			public CounterType CounterType { get; }
		}
	}
}