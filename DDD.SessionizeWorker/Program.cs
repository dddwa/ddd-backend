using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using DDD.Sessionize;
using DDD.Sessionize.Sessionize;
using DDD.Sessionize.Sync;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DDD.SessionizeWorker
{
    class Program
    {
        static void Main()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            var loggerFactory = new LoggerFactory()
                .AddConsole(config.GetSection("Logging"))
                .AddAzureWebAppDiagnostics()
                .AddDebug();
            
            MainAsync(config, loggerFactory.CreateLogger("DDD.SessionizeWorker")).GetAwaiter().GetResult();
        }

        static async Task MainAsync(IConfiguration config, ILogger logger)
        {
            var cosmosSettings = config.GetSection("Cosmos").Get<CosmosSettings>();
            var cosmosEndpoint = config.GetConnectionString("CosmosEndpoint");
            var cosmosKey = config.GetConnectionString("CosmosKey");
            var sessionizeApiKey = config.GetValue<string>("SessionizeApiKey");

            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient, sessionizeApiKey);

                var repo = new DocumentDbRepository<SessionOrPresenter>(new DocumentClient(new Uri(cosmosEndpoint), cosmosKey), cosmosSettings.DatabaseId, cosmosSettings.CollectionId);
                await repo.InitializeAsync();

                await SyncService.Sync(apiClient, repo, logger);
            }
        }
    }
}
