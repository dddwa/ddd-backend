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
            [BindSessionizeReadModelSyncConfig]
            SessionizeReadModelSyncConfig config
        )
        {
            var documentDbClient = DocumentDbAccount.Parse(config.ConnectionString);
            var repo = new DocumentDbRepository<SessionOrPresenter>(documentDbClient, config.CosmosDatabaseId, config.CosmosCollectionId);
            await repo.InitializeAsync();

            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient, config.SessionizeApiKey);

                await SyncService.Sync(apiClient, repo, log, new DateTimeProvider());
            }
        }
    }
}
