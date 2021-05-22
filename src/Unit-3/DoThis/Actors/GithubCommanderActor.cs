using System;
using System.Linq;

using Akka.Actor;
using Akka.Routing;

namespace GithubActors.Actors
{
	/// <summary>
	/// Top-level actor responsible for coordinating and launching repo-processing jobs
	/// </summary>
	public class GithubCommanderActor : ReceiveActor, IWithUnboundedStash
	{
		private int _pendingJobReplies;
		private RepoKey _repoKey;

		private IActorRef _coordinator;
		private IActorRef _canAcceptJobSender;

		public GithubCommanderActor()
		{
			Ready();
		}

		public IStash Stash { get; set; }

		protected override void PreStart()
		{
			var coordinatorProps = Props.Create(() => new GithubCoordinatorActor());

			_coordinator = Context.ActorOf(coordinatorProps.WithRouter(FromConfig.Instance), ActorPaths.GithubCoordinatorActor.Name);

			base.PreStart();
		}

		protected override void PreRestart(Exception reason, object message)
		{
			//kill off the old coordinator so we can recreate it from scratch
			_coordinator.Tell(PoisonPill.Instance);
			base.PreRestart(reason, message);
		}

		private void Ready()
		{
			Receive<CanAcceptJob>(message =>
			{
				_coordinator.Tell(message);
				_repoKey = message.Repo;

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

			Receive<ReceiveTimeout>(message =>
			{
				_canAcceptJobSender.Tell(new UnableToAcceptJob(_repoKey));
				BecomeReady();
			});
		}

		private void BecomeReady()
		{
			Become(Ready);
			Stash.UnstashAll();
			Context.SetReceiveTimeout(null);
		}

		private void BecomeAsking()
		{
			_canAcceptJobSender = Sender;
			_pendingJobReplies = _coordinator.Ask<Routees>(new GetRoutees()).Result.Members.Count();

			Become(Asking);

			Context.SetReceiveTimeout(TimeSpan.FromSeconds(3));
		}

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
	}
}
