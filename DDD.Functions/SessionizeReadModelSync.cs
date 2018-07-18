using System.Net.Http;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using DDD.Core.Time;
using DDD.Functions.Config;
using DDD.Sessionize;
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
            [BindSubmissionsConfig]
            SubmissionsConfig submissionsConfig,
            [BindSessionizeReadModelSyncConfig]
            SessionizeReadModelSyncConfig config
        )
        {
            if (config.Now > config.StopSyncingSessionsFromDate)
            {
                log.LogInformation("SessionizeReadModelSync sync date passed");
                return;
            }

            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient, config.SessionizeApiKey);
                var (sessionsRepo, presentersRepo) = await submissionsConfig.GetSubmissionRepositoryAsync();

                await SyncService.Sync(apiClient, sessionsRepo, presentersRepo, log, new DateTimeProvider(), submissionsConfig.ConferenceInstance);
            }
        }
    }
}
