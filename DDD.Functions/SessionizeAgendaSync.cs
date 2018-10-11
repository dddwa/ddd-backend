using System.Net.Http;
using System.Threading.Tasks;
using DDD.Core.Time;
using DDD.Functions.Extensions;
using DDD.Sessionize.Sessionize;
using DDD.Sessionize.Sync;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace DDD.Functions
{
    public static class SessionizeAgendaSync
    {
        [FunctionName("SessionizeAgendaSync")]
        public static async Task Run(
            [TimerTrigger("%SessionizeReadModelSyncSchedule%")]
            TimerInfo timer,
            ILogger log,
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindKeyDatesConfig]
            KeyDatesConfig keyDates,
            [BindSessionsConfig]
            SessionsConfig sessions,
            [BindSessionizeSyncConfig]
            SessionizeSyncConfig sessionize
        )
        {
            if (keyDates.Before(x => x.StopSyncingSessionsFromDate) || keyDates.After(x => x.StopSyncingAgendaFromDate))
            {
                log.LogInformation("SessionizeAgendaSync sync not active");
                return;
            }

            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient, sessionize.AgendaApiKey);
                var (sessionsRepo, presentersRepo) = await sessions.GetRepositoryAsync();

                await SyncService.Sync(apiClient, sessionsRepo, presentersRepo, log, new DateTimeProvider(), conference.ConferenceInstance);
            }
        }
    }
}
