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
using System.Security.Cryptography.X509Certificates;
using DDD.Core.AzureStorage;
using DDD.Core.Domain;
using DDD.Core.EloVoting;

namespace DDD.Functions
{
    public static class EloVotingGetPair
    {
        private const string DefaultSessionIdHeaderName = "DDDPerth.VotingSessionId";

        private static readonly string[] KeynoteExternalIds = new[]
        {
            // TODO : make this a configuration setting eventually.
            "337380"
        };
        
        private static async Task<List<string>> LoadSessionIds(SubmissionsConfig submissions, ConferenceConfig conference)
        {
            var (submissionsRepo, _) = await submissions.GetRepositoryAsync();
            var receivedSubmissions = await submissionsRepo.GetAllAsync(conference.ConferenceInstance);

            var validSessionIds = receivedSubmissions
                .Where(x => x.Session != null)
                .Select(x => x.GetSession())
                .Where(x => x.Format != "Keynote" && !KeynoteExternalIds.Contains(x.ExternalId))
                .Select(x => x.Id.ToString());

            return validSessionIds.ToList();
        }

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

            // using a Lazy here intentionally to avoid making the call to the underlying store more than once
            // rather than loading just for the sake of it, we can instead use the factory to initialise the list
            // once when we actually need it and then re-use the result over and over, it also stops the logic that
            // loads the sessions from being propagated through the stack - effectively a lambda with caching for free
            var sessionIdsLoader = new Lazy<Task<List<string>>>(async () => await LoadSessionIds(submissions, conference));

            // get the voting session id from the cookie from the user's browser, creating a new one if it doesn't exist
            var headerKey = string.IsNullOrEmpty(submissions.UserVotingSessionHeaderName)
                ? DefaultSessionIdHeaderName
                : submissions.UserVotingSessionHeaderName;
            var userSessionId = req.Headers[headerKey];
            if (string.IsNullOrEmpty(userSessionId))
            {
                log.LogWarning("Could not get session identifier from header {headerName}.", headerKey);
                return new StatusCodeResult(400);
            }

            // retrieve a pair of vote session ids from the data stored against the id in the user's cookie
            var userVoteSessionRepository = await submissions.GetUserVoteSessionRepositoryAsync();
            var sessionIds = await userVoteSessionRepository.NextSessionPair(sessionIdsLoader, userSessionId);

            if (sessionIds == null)
            {
                log.LogWarning("Could not get session pair for user voting session with id {userVoteSessionId}.", userSessionId);
                return new StatusCodeResult(400);
            }

            // we have the id's of two sessions, so load them directly from the underlying table store rather than loading everything
            // and iterating which is a pain
            var (sessionRepository, _) = await submissions.GetRepositoryAsync();
            var loadedSessions = new List<SessionEntity>()
            {
                await sessionRepository.GetAsync(conference.ConferenceInstance, sessionIds.Item1),
                await sessionRepository.GetAsync(conference.ConferenceInstance, sessionIds.Item2)
            };
            
            // convert the sessions into submission entries
            var validSessions = loadedSessions
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

            if (!validSessions.Any() || validSessions.Count != 2)
            {
                log.LogWarning("Could not get sessions for User session id {userSessionId}, got {sessionCount} Sessions.", userSessionId, validSessions.Count);
                return new StatusCodeResult(400);
            }
            
            // first random submission
            var submissionA = validSessions[0];
            var submissionB = validSessions[1];

            var password = eloVoting.EloPasswordPhrase;
            var now = keyDates.Now.ToUnixTimeSeconds();
            var voteId = Guid.NewGuid().ToString();

            // encrypt the id's that we're going to send in the payload, this will prevent naughty people
            // from spamming the endpoint as each vote can only be submitted once as the vote id is encoded
            // into the payload so we can detect a replay attack
            submissionA.Id = Encryptor.EncryptSubmissionId(voteId, submissionA.Id, password, now);
            submissionB.Id = Encryptor.EncryptSubmissionId(voteId, submissionB.Id, password, now);
            
            var results = new PairOfSessions()
            {
                SubmissionA = submissionA,
                SubmissionB = submissionB
            };

            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver()
            };

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
