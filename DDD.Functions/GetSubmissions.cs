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
    public static class GetSubmissions
    {
        private static readonly Random Random = new Random();

        [FunctionName("GetSubmissions")]
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

            var (submissionsRepo, submittersRepo) = await submissions.GetRepositoryAsync();
            var receivedSubmissions = await submissionsRepo.GetAllAsync(conference.ConferenceInstance);
            var submitters = await submittersRepo.GetAllAsync(conference.ConferenceInstance);

            var submissionData = receivedSubmissions.Where(x => x.Session != null)
                .Select(x => x.GetSession())
                .Select(s => new Submission
                {
                    Id = s.Id.ToString(),
                    Title = s.Title, 
                    Abstract = s.Abstract,
                    Format = s.Format,
                    Level = s.Level,
                    Tags = s.Tags,
                    Presenters = conference.AnonymousSubmissions
                        ? new Submitter[0]
                        : s.PresenterIds.Select(pId => submitters.Where(p => p.Id == pId).Select(p => p.GetPresenter()).Select(p => new Submitter
                        {
                            Id = p.Id.ToString(),
                            Name = p.Name,
                            Tagline = p.Tagline,
                            Bio = p.Bio,
                            ProfilePhotoUrl = p.ProfilePhotoUrl,
                            TwitterHandle = p.TwitterHandle,
                            WebsiteUrl = p.WebsiteUrl
                        }).Single()).ToArray()
                })
                .OrderBy(x => Random.Next())
                .ToArray();

            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new DefaultContractResolver();

            return new JsonResult(submissionData, settings);
        }

        public class Submission
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Abstract { get; set; }
            public string Format { get; set; }
            public string Level { get; set; }
            public string[] Tags { get; set; }
            public Submitter[] Presenters { get; set; }
        }

        public class Submitter
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Tagline { get; set; }
            public string Bio { get; set; }
            public string ProfilePhotoUrl { get; set; }
            public string TwitterHandle { get; set; }
            public string WebsiteUrl { get; set; }
        }
    }
}
