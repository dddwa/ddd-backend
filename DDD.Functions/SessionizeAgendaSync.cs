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
    public static class SessionizeAgendaSync
    {
        [FunctionName("SessionizeAgendaSync")]
        public static async Task Run(
            [TimerTrigger("%SessionizeReadModelSyncSchedule%")]
            TimerInfo timer,
            ILogger log,
            [BindSessionsConfig]
            SessionsConfig sessionsConfig,
            [BindSessionizeReadModelSyncConfig]
            SessionizeReadModelSyncConfig config
        )
        {
            if (config.Now < config.StopSyncingSessionsFromDate || config.Now > config.StopSyncingAgendaFromDate)
            {
                log.LogInformation("SessionizeAgendaSync sync not active");
                return;
            }

            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient, config.SessionizeAgendaApiKey);
                var (sessionsRepo, presentersRepo) = await sessionsConfig.GetSessionRepositoryAsync();

                await SyncService.Sync(apiClient, sessionsRepo, presentersRepo, log, new DateTimeProvider(), sessionsConfig.ConferenceInstance);
            }
        }
    }
}
