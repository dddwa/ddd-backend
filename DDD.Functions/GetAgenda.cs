using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using DDD.Functions.Extensions;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace DDD.Functions
{
    public static class GetAgenda
    {
        [FunctionName("GetAgenda")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest req,
            ILogger log,
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindKeyDatesConfig]
            KeyDatesConfig keyDates,
            [BindSessionsConfig]
            SessionsConfig sessionsConfig)
        {
            if (keyDates.Before(x => x.SubmissionsAvailableToDate))
            {
                log.LogWarning("Attempt to access GetAgenda endpoint before they are available at {availableDate}.", keyDates.SubmissionsAvailableToDate);
                return new StatusCodeResult(404);
            }

            var (sessionsRepo, presentersRepo) = await sessionsConfig.GetRepositoryAsync();
            var sessions = await sessionsRepo.GetAllAsync(conference.ConferenceInstance);
            var presenters = await presentersRepo.GetAllAsync(conference.ConferenceInstance);

            var agenda = sessions.Select(x => x.GetSession())
                .Select(s => new Session
                {
                    Id = s.Id.ToString(),
                    Title = s.Title, 
                    Abstract = s.Abstract,
                    Format = s.Format,
                    Level = s.Level,
                    Tags = s.Tags,
                    Presenters = s.PresenterIds.Select(pId => presenters.Where(p => p.Id == pId).Select(p => p.GetPresenter()).Select(p => new Presenter
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
                .OrderBy(x => x.Title)
                .ToArray();

            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new DefaultContractResolver();

            return new JsonResult(agenda, settings);
        }

        public class Session
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Abstract { get; set; }
            public string Format { get; set; }
            public string Level { get; set; }
            public string[] Tags { get; set; }
            public Presenter[] Presenters { get; set; }
        }

        public class Presenter
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
