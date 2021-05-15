using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;

using Akka.Actor;
using ChartApp.Messages;

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

		public class RemoveSeries
		{
			public RemoveSeries(string seriesName)
			{
				SeriesName = seriesName;
			}

			public string SeriesName { get; }
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

		/// <summary>
		/// Maximum number of points we will allow in a series
		/// </summary>
		public const int MaxPoints = 250;

		/// <summary>
		/// Incrementing counter we use to plot along the X-axis
		/// </summary>
		private int XPosCounter;

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
				SetChartBoundaries();
			});

			Receive<RemoveSeries>(message =>
			{
				var name = message.SeriesName;

				if (!_seriesIndex.TryGetValue(name, out var series))
				{
					return;
				}

				_chart.Series.Remove(series);
				_seriesIndex.Remove(name);
				SetChartBoundaries();
			});

			Receive<Metric>(metric =>
			{
				var series = _seriesIndex[metric.Series];
				series.Points.AddXY(XPosCounter++, metric.CounterValue);
				while (series.Points.Count > MaxPoints) series.Points.RemoveAt(0);

				SetChartBoundaries();
			});
		}

		public ChartingActor(Chart chart)
			: this(chart, new Dictionary<string, Series>())
		{
		}

		private void SetChartBoundaries()
		{
			double maxAxisX = XPosCounter;
			double minAxisX = XPosCounter - MaxPoints;

			var allPoints = _seriesIndex.Values.SelectMany(series => series.Points).ToList();
			var yValues = allPoints.SelectMany(point => point.YValues).ToList();
			var maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;
			var minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;

			if (allPoints.Count > 2)
			{
				var area = _chart.ChartAreas[0];
				area.AxisX.Minimum = minAxisX;
				area.AxisX.Maximum = maxAxisX;
				area.AxisY.Minimum = minAxisY;
				area.AxisY.Maximum = maxAxisY;
			}
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
			{
				_seriesIndex = ic.InitialSeries;
			}

			//delete any existing series
			_chart.Series.Clear();

			// set the axes up
			var area = _chart.ChartAreas[0];
			area.AxisX.IntervalType = DateTimeIntervalType.Number;
			area.AxisY.IntervalType = DateTimeIntervalType.Number;

			SetChartBoundaries();

			//attempt to render the initial chart
			if (_seriesIndex.Any())
			{
				foreach (var series in _seriesIndex)
				{
					//force both the chart and the internal index to use the same names
					series.Value.Name = series.Key;
					_chart.Series.Add(series.Value);
				}
			}
		}

		#endregion
	}
}
