using System.Linq;
using System.Windows.Forms;

using Akka.Actor;
using ChartApp.Messages;

namespace ChartApp.Actors
{
	public class ButtonToggleActor : ReceiveActor
	{
		public class Toggle
		{
		}

		private bool _toggled;

		public ButtonToggleActor(CounterType counterType, IActorRef coordinatorActor, Control button)
		{
			Receive<Toggle>(_ =>
			{
				_toggled = !_toggled;

				button.Text = string.Join(
					" ",
					button.Text.Split(' ').First(),
					_toggled ? "ON" : "OFF");

				coordinatorActor.Tell(_toggled 
					? new PerformanceCounterCoordinatorActor.Watch(counterType)
					: new PerformanceCounterCoordinatorActor.Unwatch(counterType));
			});
		}
	}
}