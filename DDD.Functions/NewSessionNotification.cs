using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DDD.Core.AzureStorage;
using DDD.Functions.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DDD.Functions
{
    public static class NewSessionNotification
    {
        [FunctionName("NewSessionNotification")]
        public static async Task Run(
            [TimerTrigger("%NewSessionNotificationSchedule%")]
            TimerInfo timer,
            ILogger log,
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindNewSessionNotificationConfig]
            NewSessionNotificationConfig newSessionNotification,
            [BindSubmissionsConfig]
            SubmissionsConfig submissions)
        {
            var (submissionsRepo, submittersRepo) = await submissions.GetRepositoryAsync();
            var notifiedSessionsRepo = await newSessionNotification.GetRepositoryAsync();

            var allSubmissions = await submissionsRepo.GetAllAsync(conference.ConferenceInstance);
            var allSubmitters = await submittersRepo.GetAllAsync(conference.ConferenceInstance);
            var notifiedSessions = await notifiedSessionsRepo.GetAllAsync();
            var sessionsToNotify = allSubmissions.Where(s => notifiedSessions.All(n => n.Id != s.Id)).ToArray();

            log.LogInformation("Found {numberOfSessions} sessions, {numberOfSessionsAlreadyNotified} already notified and notifying another {numberOfSessionsBeingNotified}", allSubmissions.Count, notifiedSessions.Count, sessionsToNotify.Length);

            using (var client = new HttpClient())
            {
                foreach (var submission in sessionsToNotify)
                {
                    var presenterIds = submission.GetSession().PresenterIds.Select(x => x.ToString()).ToArray();
                    var presenters = allSubmitters.Where(submitter => presenterIds.Contains(submitter.Id.ToString()));
                    var postContent = JsonConvert.SerializeObject(new
                    {
                        Session = submission.GetSession(),
                        Presenters = presenters.Select(x => x.GetPresenter()).ToArray()
                    }, Formatting.None, new StringEnumConverter());

                    // Post the data
                    log.LogInformation("Posting {submissionId} to {logicAppUrl}", submission.Id, newSessionNotification.LogicAppUrl);
                    var response = await client.PostAsync(newSessionNotification.LogicAppUrl, new StringContent(postContent, Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        log.LogError("Unsuccessful request to post {documentId}; received {statusCode} and {responseBody}", submission.Id, response.StatusCode, await response.Content.ReadAsStringAsync());
                        response.EnsureSuccessStatusCode();
                    }

                    // Persist the notification record
                    await notifiedSessionsRepo.CreateAsync(new NotifiedSessionEntity(submission.Id));
                }
            }
        }
    }
}
