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
using DDD.Core.Domain;
using DDD.Core.EloVoting;

namespace DDD.Functions
{
    public static class EloVotingGetPair
    {
        private const string CookieName = "DDDPerth.VotingSessionId";

        private static readonly string[] KeynoteExternalIds = new[]
        {
            // TODO : make this a configuration setting eventually.
            "337380"
        };
        
        private static async Task<List<Session>> LoadSessions(SubmissionsConfig submissions, ConferenceConfig conference)
        {
            // do these outside of the lock because...
            var (submissionsRepo, _) = await submissions.GetRepositoryAsync();
            var receivedSubmissions = await submissionsRepo.GetAllAsync(conference.ConferenceInstance);
            
            var validSessions = receivedSubmissions
                .Where(x => x.Session != null)
                .Select(x => x.GetSession())
                .Where(x => x.Format != "Keynote" && !KeynoteExternalIds.Contains(x.ExternalId));

            return validSessions.ToList();
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

            UserVotingSessionRepository repo = new UserVotingSessionRepository();
            Lazy<Task<List<Session>>> sessions = new Lazy<Task<List<Session>>>(async () => await LoadSessions(submissions, conference));

            var userSessionId = string.IsNullOrEmpty(
                req.Cookies[CookieName])
                ? Guid.NewGuid().ToString()
                : req.Cookies[CookieName];
            
            var sessionIds = await repo.GetSessionIds(sessions, userSessionId);

            var allSessions = await sessions.Value;
            var validSessions = allSessions
                .Where(x => x.Id.ToString() == sessionIds.Item1 || x.Id.ToString() == sessionIds.Item2)
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

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver()
            };

            req.HttpContext.Response.Cookies.Append(CookieName, userSessionId);

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
