using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System;
using DDD.Functions.Extensions;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Net;

namespace DDD.Functions
{
    public static class EloVotingGetPair
    {
        private static readonly Random Random = new Random();

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

            var validSessions = receivedSubmissions.Where(x => x.Session != null);

            // first random submission
            var random = new Random();
            var submissionA = validSessions
                .Select(x => x.GetSession())
                .Select(s => new Submission
                {
                    Id = s.Id.ToString(),
                    Title = s.Title,
                    Abstract = s.Abstract,
                })
                .ElementAt(random.Next(validSessions.Count()));

            var submissionB = validSessions.Where(x => x.Id.ToString() != submissionA.Id)
                .Select(x => x.GetSession())
                .Select(s => new Submission
                {
                    Id = s.Id.ToString(),
                    Title = s.Title,
                    Abstract = s.Abstract,
                })
                // need to -1 here because we have removed one from contention with the first choice                
                .ElementAt(random.Next(validSessions.Count() - 1));  

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
        }
    }
}
