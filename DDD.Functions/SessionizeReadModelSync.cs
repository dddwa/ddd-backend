using System;
using System.Net.Http;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using DDD.Functions.Config;
using DDD.Sessionize;
using DDD.Sessionize.Sessionize;
using DDD.Sessionize.Sync;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace DDD.Functions
{
    public static class SessionizeReadModelSync
    {
        [FunctionName("SessionizeReadModelSync")]
        public static async Task Run(
            [TimerTrigger("%SessionizeReadModelSyncSchedule%")]
            TimerInfo timer,
            TraceWriter log,
            [BindSessionizeReadModelSyncConfig]
            SessionizeReadModelSyncConfig config
        )
        {
            log.Info("Starting sync of sessionize read model");

            var documentDbClient = DocumentDbAccount.Parse(config.ConnectionString);

            var repo = new DocumentDbRepository<SessionOrPresenter>(documentDbClient, config.CosmosDatabaseId, config.CosmosCollectionId);
            await repo.Initialize();

            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient);

                await SyncService.Sync(apiClient, config.SessionizeApiKey, repo);
            }

            log.Info($"Sessionize read model synced, next running on {timer.FormatNextOccurrences(5)}");
        }
    }
}
