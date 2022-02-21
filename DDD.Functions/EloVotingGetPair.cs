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
            SubmissionsConfig submissions
        )
        {
            if (keyDates.Before(x => x.SubmissionsAvailableFromDate) || keyDates.After(x => x.SubmissionsAvailableToDate))
            {
                log.LogWarning("Attempt to access GetSubmissions endpoint outside of allowed window of {start} -> {end}.", keyDates.SubmissionsAvailableFromDate, keyDates.SubmissionsAvailableToDate);
                return new StatusCodeResult(404);
            }

            // Within voting window, allowing for 5 minutes of clock drift
            if (keyDates.Before(x => x.VotingAvailableFromDate) || keyDates.After(x => x.VotingAvailableToDate, TimeSpan.FromMinutes(5)))
            {
                log.LogWarning("Attempt to access SubmitVote endpoint outside of allowed window of {start} -> {end}.", keyDates.VotingAvailableFromDate, keyDates.VotingAvailableToDate);
                return new StatusCodeResult(404);
            }
            var (submissionsRepo, _) = await submissions.GetRepositoryAsync();
            var receivedSubmissions = await submissionsRepo.GetAllAsync(conference.ConferenceInstance);

            // first random
            var random = new Random();
            var submissionA = receivedSubmissions.Where(x => x.Session != null)
                .Select(x => x.GetSession())
                .Select(s => new Submission
                {
                    Id = s.Id.ToString(),
                    Title = s.Title,
                    Abstract = s.Abstract,
                })
                .ElementAt(random.Next(receivedSubmissions.Count));


            var submissionB = receivedSubmissions.Where(x => x.Session != null && x.Id.ToString() != submissionA.Id)
                            .Select(x => x.GetSession())
                            .Select(s => new Submission
                            {
                                Id = s.Id.ToString(),
                                Title = s.Title,
                                Abstract = s.Abstract,
                            })
                            .ElementAt(random.Next(receivedSubmissions.Count));

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
