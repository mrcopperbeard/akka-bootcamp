using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

using Akka.Actor;
using ChartApp.Messages;

namespace ChartApp.Actors
{
	public class ChartingActor : ReceiveActor, IWithUnboundedStash
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

		public class TogglePause
		{
		}

		#endregion

		/// <summary>
		/// Maximum number of points we will allow in a series
		/// </summary>
		public const int MaxPoints = 250;

		/// <summary>
		/// Incrementing counter we use to plot along the X-axis
		/// </summary>
		private int _xPosCounter;

		private readonly Chart _chart;

		private readonly Control _pauseButton;

		private Dictionary<string, Series> _seriesIndex;

		public ChartingActor(Chart chart, Control pauseButton)
		{
			_chart = chart;
			_pauseButton = pauseButton;
			_seriesIndex = new Dictionary<string, Series>();

			Charting();
		}

		public IStash Stash { get; set; }

		private void SetChartBoundaries()
		{
			double maxAxisX = _xPosCounter;
			double minAxisX = _xPosCounter - MaxPoints;

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

		private void Charting()
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
				SetMetric(metric.Series, metric.CounterValue);
				SetChartBoundaries();
			});

			Receive<TogglePause>(_ =>
			{
				BecomeStacked(Paused);
				SetPauseButtonText(false);
			});
		}

		private void Paused()
		{
			Receive<AddSeries>(_ => Stash.Stash());
			Receive<RemoveSeries>(_ => Stash.Stash());

			Receive<TogglePause>(_ =>
			{
				UnbecomeStacked();
				Stash.UnstashAll();
				SetPauseButtonText(true);
			});

			Receive<Metric>(metric =>
			{
				SetMetric(metric.Series, 0f);
				SetChartBoundaries();
			});
		}

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

		private void SetMetric(string seriesName, float seriesValue)
		{
			if (_seriesIndex.TryGetValue(seriesName, out var series))
			{
				series.Points.AddXY(_xPosCounter++, seriesValue);
				while (series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
			}
		}

		private void SetPauseButtonText(bool resumePressed)
		{
			_pauseButton.Text = resumePressed ? "PAUSE ||" : "RESUME ->";
		}
	}
}
