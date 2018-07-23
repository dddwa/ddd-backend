using System.Net.Http;
using System.Threading.Tasks;
using DDD.Core.Time;
using DDD.Functions.Config;
using DDD.Sessionize.Sessionize;
using DDD.Sessionize.Sync;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace DDD.Functions
{
    public static class SessionizeReadModelSync
    {
        [FunctionName("SessionizeReadModelSync")]
        public static async Task Run(
            [TimerTrigger("%SessionizeReadModelSyncSchedule%")]
            TimerInfo timer,
            ILogger log,
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindKeyDatesConfig]
            KeyDatesConfig keyDates,
            [BindSubmissionsConfig]
            SubmissionsConfig submissions,
            [BindSessionizeSyncConfig]
            SessionizeSyncConfig sessionize
        )
        {
            if (keyDates.After(x => x.StopSyncingSessionsFromDate))
            {
                log.LogInformation("SessionizeReadModelSync sync date passed");
                return;
            }

            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient, sessionize.SubmissionsApiKey);
                var (sessionsRepo, presentersRepo) = await submissions.GetRepositoryAsync();

                await SyncService.Sync(apiClient, sessionsRepo, presentersRepo, log, new DateTimeProvider(), conference.ConferenceInstance);
            }
        }
    }
}
