using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Linq;
using DDD.Functions.Extensions;
using DDD.Core.AzureStorage;

namespace DDD.Functions
{
    public static class SubmitFeedback
    {
        [FunctionName("SubmitFeedback")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequestMessage req,
            ILogger log,
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindKeyDatesConfig]
            KeyDatesConfig keyDates,
            [BindFeedbackConfig]
            FeedbackConfig feedbackConfig,
            [BindSessionsConfig]
            SessionsConfig sessionsConfig
            )
        {
            var feedback = await req.Content.ReadAsAsync<FeedbackRequest>();

            // Within feedback window
            if (keyDates.Before(x => x.FeedbackAvailableFromDate) || keyDates.After(x => x.FeedbackAvailableToDate))
            {
                log.LogWarning("Attempt to access SubmitFeedback endpoint outside of allowed window of {start} -> {end}.", keyDates.FeedbackAvailableFromDate, keyDates.FeedbackAvailableToDate);
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            // Valid feedback
            if (string.IsNullOrWhiteSpace(feedback.Likes) && string.IsNullOrWhiteSpace(feedback.ImprovementIdeas))
            {
                log.LogWarning("Attempt to access SubmitFeedback endpoint with invalid feedback details from {DeviceId}", feedback.DeviceId);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            var (conferenceFeedbackRepo, sessionFeedbackRepo) = await feedbackConfig.GetRepositoryAsync();

            if (feedback.IsConferenceFeedback)
            {
                // Save conference feedback
                log.LogInformation("Successfully received conference feedback for from DeviceId {DeviceId}; persisting...", feedback.DeviceId);
                var feedbackToPersist = new ConferenceFeedbackEntity(
                    conference.ConferenceInstance,
                    feedback.Name,
                    feedback.Rating.ToString(),
                    feedback.Likes,
                    feedback.ImprovementIdeas,
                    feedback.DeviceId.ToString());

                await conferenceFeedbackRepo.CreateAsync(feedbackToPersist);
            }
            else
            {
                // valid Session existed 
                var (sessionsRepo, presentersRepo) = await sessionsConfig.GetRepositoryAsync();
                var allSessions = await sessionsRepo.GetAllAsync(conference.ConferenceInstance);
                var allPresenters = await presentersRepo.GetAllAsync(conference.ConferenceInstance);

                var sessionRow = allSessions.First(s => s != null && feedback.SessionId == s.Id.ToString());
                if (sessionRow == null && !feedback.IsConferenceFeedback)
                {
                    log.LogWarning("Attempt to submit to SubmitFeedback endpoint with invalid SessionId {SessionId} from {DeviceId}", feedback.SessionId, feedback.DeviceId);
                    return new StatusCodeResult((int)HttpStatusCode.BadRequest);
                }

                var session = sessionRow.GetSession();
                var presenterNames = string.Join(", ", session.PresenterIds.Select(pId => allPresenters.Single(p => p.Id == pId)).Select(x => x.Name));

                // Save feedback
                log.LogInformation("Successfully received feedback for {SessionId} from DeviceId {DeviceId}; persisting...", feedback.SessionId, feedback.DeviceId);
                var feedbackToPersist = new SessionFeedbackEntity(
                    session.Id.ToString(),
                    conference.ConferenceInstance,
                    feedback.Name,
                    feedback.Rating.ToString(),
                    feedback.Likes,
                    feedback.ImprovementIdeas,
                    $"{session.Title} - {presenterNames}",
                    feedback.DeviceId.ToString());

                await sessionFeedbackRepo.CreateAsync(feedbackToPersist);
            }
            return new StatusCodeResult((int)HttpStatusCode.NoContent);
        }
    }

    public class FeedbackRequest
    {
        public Guid DeviceId { get; set; }
        public string SessionId { get; set; }
        public string Name { get; set; }
        public int Rating { get; set; }
        public string Likes { get; set; }
        public string ImprovementIdeas { get; set; }
        public bool IsConferenceFeedback { get; set; }
    }
}
