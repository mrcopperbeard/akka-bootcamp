using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Routing;
using Akka.Util.Internal;

namespace GithubActors.Actors
{
	/// <summary>
	/// Top-level actor responsible for coordinating and launching repo-processing jobs
	/// </summary>
	public class GithubCommanderActor : ReceiveActor, IWithUnboundedStash
	{
		public class CanAcceptJob
		{
			public CanAcceptJob(RepoKey repo)
			{
				Repo = repo;
			}

			public RepoKey Repo { get; }
		}

		public class AbleToAcceptJob
		{
			public AbleToAcceptJob(RepoKey repo)
			{
				Repo = repo;
			}

			public RepoKey Repo { get; }
		}

		public class UnableToAcceptJob
		{
			public UnableToAcceptJob(RepoKey repo)
			{
				Repo = repo;
			}

			public RepoKey Repo { get; }
		}

		private int _pendingJobReplies;

		private IActorRef _coordinator;
		private IActorRef _canAcceptJobSender;

		public GithubCommanderActor()
		{
			Ready();
		}

		public IStash Stash { get; set; }

		protected override void PreStart()
		{
			const int groupCount = 3;
			var coordinatorProps = Props.Create(() => new GithubCoordinatorActor());

			RepeatName(ActorPaths.GithubCoordinatorActor.Name, groupCount)
				.ForEach(name => Context.ActorOf(coordinatorProps, name));

			var group = new BroadcastGroup(RepeatName(ActorPaths.GithubCoordinatorActor.Path, groupCount));

			_coordinator = Context.ActorOf(Props.Empty.WithRouter(group));

			base.PreStart();
		}

		protected override void PreRestart(Exception reason, object message)
		{
			//kill off the old coordinator so we can recreate it from scratch
			_coordinator.Tell(PoisonPill.Instance);
			base.PreRestart(reason, message);
		}

		private static IEnumerable<string> RepeatName(string name, int count) =>
			Enumerable
				.Range(1, count)
				.Select(i => string.Join('-', name, i.ToString()));

		private void Ready()
		{
			Receive<CanAcceptJob>(message =>
			{
				_coordinator.Tell(message);

				BecomeAsking();
			});
		}

		private void Asking()
		{
			Receive<CanAcceptJob>(_ => Stash.Stash());

			Receive<AbleToAcceptJob>(message =>
			{
				_canAcceptJobSender.Tell(message);

				Sender.Tell(new GithubCoordinatorActor.BeginJob(message.Repo));

				var launchResultWindow = new MainFormActor.LaunchRepoResultsWindow(message.Repo, Sender);
				Context.ActorSelection(ActorPaths.MainFormActor.Path).Tell(launchResultWindow);

				BecomeReady();
			});

			Receive<UnableToAcceptJob>(message =>
			{
				_pendingJobReplies--;

				if (_pendingJobReplies == 0)
				{
					_canAcceptJobSender.Tell(message);

					BecomeReady();
				}
			});
		}

		private void BecomeReady()
		{
			Become(Ready);
			Stash.UnstashAll();
		}

		private void BecomeAsking()
		{
			_canAcceptJobSender = Sender;
			_pendingJobReplies = 3;

			Become(Asking);
		}
	}
}
