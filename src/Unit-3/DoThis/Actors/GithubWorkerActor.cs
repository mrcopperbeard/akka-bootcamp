using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Octokit;

namespace GithubActors.Actors
{
	/// <summary>
	///     Individual actor responsible for querying the Github API
	/// </summary>
	public class GithubWorkerActor : ReceiveActor
	{
		#region Message classes

		public class QueryStarrers
		{
			public QueryStarrers(RepoKey key)
			{
				Key = key;
			}

			public RepoKey Key { get; }
		}

		/// <summary>
		///     Query an individual starrer
		/// </summary>
		public class QueryStarrer
		{
			public QueryStarrer(string login)
			{
				Login = login;
			}

			public string Login { get; }
		}

		public class StarredReposForUser
		{
			public StarredReposForUser(string login, IEnumerable<Repository> repos)
			{
				Repos = repos;
				Login = login;
			}

			public string Login { get; }

			public IEnumerable<Repository> Repos { get; }
		}

		#endregion

		private IGitHubClient _gitHubClient;
		private readonly Func<IGitHubClient> _gitHubClientFactory;

		public GithubWorkerActor(Func<IGitHubClient> gitHubClientFactory)
		{
			_gitHubClientFactory = gitHubClientFactory;
			InitialReceives();
		}

		protected override void PreStart()
		{
			_gitHubClient = _gitHubClientFactory();
		}

		private void InitialReceives()
		{
			//query an individual starrer
			Receive<RetryableQuery>(query => query.Query is QueryStarrer, query =>
			{
				// ReSharper disable once PossibleNullReferenceException (we know from the previous IS statement that this is not null)
				var starGiver = (query.Query as QueryStarrer).Login;
				var sender = Sender;

				_gitHubClient.Activity.Starring.GetAllForUser(starGiver)
					.ContinueWith<object>((Task<IReadOnlyList<Repository>> task) => task.IsCanceled || task.IsFaulted
						? query.NextTry()
						: new StarredReposForUser(starGiver, task.Result))
					.PipeTo(sender);
			});

			//query all star givers for a repository
			Receive<RetryableQuery>(query => query.Query is QueryStarrers, query =>
			{
				// ReSharper disable once PossibleNullReferenceException (we know from the previous IS statement that this is not null)
				var starGivers = (query.Query as QueryStarrers).Key;
				var sender = Sender;
				_gitHubClient.Activity.Starring.GetAllStargazers(starGivers.Owner, starGivers.Repo)
					.ContinueWith<object>((Task<IReadOnlyList<User>> task) => task.IsFaulted || task.IsCanceled
						? query.NextTry()
						: task.Result.ToArray())
					.PipeTo(sender);
			});
		}
	}
}
