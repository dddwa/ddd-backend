using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using DDD.Functions.Extensions;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Net;
using DDD.Core.AzureStorage;

namespace DDD.Functions
{
    //  container for extension methods that adds AsShuffleable and AsSingletonShuffleable to the
    // LINQ queries
    public static class IShuffleableExtensions
    {
        private static readonly object _lock = new object();

        private static readonly IDictionary<string, InfiniteShuffler<SessionEntity>> _instances =
            new Dictionary<string, InfiniteShuffler<SessionEntity>>();
        public static InfiniteShuffler<SessionEntity> AsShuffleable(this IEnumerable<SessionEntity> entity, ShufflerConfig config)
        {
            return new InfiniteShuffler<SessionEntity>(config, entity);
        }

        public static InfiniteShuffler<SessionEntity> AsSingletonShuffleable(this IEnumerable<SessionEntity> entity,
            ShufflerConfig config)
        {
            // it's safe to do this outside of the lock as we know it's a write-once scenario
            if (_instances.ContainsKey(config.Name))
            {
                return _instances[config.Name];
            }
            
            lock (_lock)
            {
                if (!_instances.ContainsKey(config.Name))
                {
                    _instances[config.Name] = new InfiniteShuffler<SessionEntity>(config, entity);
                }
            }
            
            // it's okay to read, we know it's written to now
            return _instances[config.Name];
        }
    }
    public static class EloVotingGetPair
    {
        private static readonly ShufflerConfig EloVotingShufflerConfig = new ShufflerConfig()
        {
            Name = typeof(EloVotingGetPair).AssemblyQualifiedName,
            LowWatermark = 10
        };
        
        [FunctionName("EloVotingGetPair")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest req,
            ILogger log,
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindKeyDatesConfig]
            KeyDatesConfig keyDates,
            [BindSubmissionsConfig]
            SubmissionsConfig submissions,
            [BindEloVotingConfig]
            EloVotingConfig eloVoting
        )
        {
            if (!eloVoting.EloEnabled)
            {
                log.LogWarning("Attempt to access EloVotingSubmitPair endpoint while EloEnabled feature flag is disabled.");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            if (keyDates.Before(x => x.VotingAvailableFromDate) || keyDates.After(x => x.VotingAvailableToDate))
            {
                log.LogWarning("Attempt to access EloVotingGetPair endpoint outside of allowed Voting window of {start} -> {end}.", keyDates.VotingAvailableFromDate, keyDates.VotingAvailableToDate);
                return new StatusCodeResult(404);
            }

            // the original GetSubmission relies on conference.AnonymousSubmissions flag to return submitters or not
            // This new Elo voting will not return the submisster to make it faster 
            //   and DDD is always uses anonymous voting so ignoring the submitter by using _            
            var (submissionsRepo, _) = await submissions.GetRepositoryAsync();
            var receivedSubmissions = await submissionsRepo.GetAllAsync(conference.ConferenceInstance);

            if (!receivedSubmissions.Any())
            {
                log.LogWarning("There is no submission for {year} conference.", conference.ConferenceInstance);
                return new StatusCodeResult(400);
            }

            // I believe this should operate as one query using the underlying provider which /should/ be more efficient
            var validSessions = receivedSubmissions
                .Where(x => x.Session != null)
                // make it a singleton shufflable, so the order is preserved inside of this host.
                .AsSingletonShuffleable(EloVotingShufflerConfig)
                .Take(2)
                .Select(x => x.GetSession())
                .Select(s => new Submission
                {
                    Id = s.Id.ToString(),
                    Title = s.Title,
                    Abstract = s.Abstract,
                    Format = s.Format,
                    Level = s.Level,
                    Tags = s.Tags,
                })                
                .ToList();

            // first random submission
            var submissionA = validSessions[0];
            var submissionB = validSessions[1];

            // encrypt the two ids at once
            var password = eloVoting.EloPasswordPhrase;
            var now = keyDates.Now.ToUnixTimeSeconds();
            var voteId = Guid.NewGuid().ToString();

            submissionA.Id = Encryptor.EncryptSubmissionId(voteId, submissionA.Id, password, now);
            submissionB.Id = Encryptor.EncryptSubmissionId(voteId, submissionB.Id, password, now);
            
            var results = new PairOfSessions()
            {
                SubmissionA = submissionA,
                SubmissionB = submissionB
            };

            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new DefaultContractResolver();

            return new JsonResult(results, settings);
        }

        public class PairOfSessions
        {
            public Submission SubmissionA { get; set; }
            public Submission SubmissionB { get; set; }
        }
        public class Submission
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Abstract { get; set; }
            public string Format { get; set; }
            public string Level { get; set; }
            public string[] Tags { get; set; }
        }
    }
}
