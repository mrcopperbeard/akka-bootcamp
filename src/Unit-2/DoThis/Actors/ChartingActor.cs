using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

using Akka.Actor;

namespace ChartApp.Actors
{
	public class ChartingActor : ReceiveActor
	{
		#region Messages

		public class AddSeries
		{
			public AddSeries(Series series)
			{
				Series = series;
			}

			public Series Series { get; }
		}

		public class InitializeChart
		{
			public InitializeChart(Dictionary<string, Series> initialSeries)
			{
				InitialSeries = initialSeries;
			}

			public Dictionary<string, Series> InitialSeries { get; }
		}

		#endregion

		private readonly Chart _chart;
		private Dictionary<string, Series> _seriesIndex;

		private ChartingActor()
		{
			Receive<InitializeChart>(HandleInitialize);

			Receive<AddSeries>(message =>
			{
				var series = message.Series;
				var name = series.Name;

				if (string.IsNullOrEmpty(name) || _seriesIndex.ContainsKey(name))
				{
					return;
				}

				_seriesIndex[name] = series;
				_chart.Series.Add(series);
			});
		}

		public ChartingActor(Chart chart)
			: this(chart, new Dictionary<string, Series>())
		{
		}

		public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex)
			: this()
		{
			_chart = chart;
			_seriesIndex = seriesIndex;
		}

		#region Individual Message Type Handlers

		private void HandleInitialize(InitializeChart ic)
		{
			if (ic.InitialSeries != null)
				//swap the two series out
				_seriesIndex = ic.InitialSeries;

			//delete any existing series
			_chart.Series.Clear();

			//attempt to render the initial chart
			if (_seriesIndex.Any())
				foreach (var series in _seriesIndex)
				{
					//force both the chart and the internal index to use the same names
					series.Value.Name = series.Key;
					_chart.Series.Add(series.Value);
				}
		}

		#endregion
	}
}
