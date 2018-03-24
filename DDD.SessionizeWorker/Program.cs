using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DDD.SessionizeWorker.DocumentDb;
using DDD.SessionizeWorker.Sessionize;
using DDD.SessionizeWorker.Sync;
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
            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient);
                var cosmosSettings = config.GetSection("Cosmos").Get<CosmosSettings>();
                var repo = new DocumentDbRepository<SessionOrPresenter>(config.GetConnectionString("CosmosEndpoint"),
                    config.GetConnectionString("CosmosKey"), cosmosSettings.DatabaseId, cosmosSettings.CollectionId);
                await repo.Initialize();
                var sessionizeApiKey = config.GetValue<string>("SessionizeApiKey");

                await SyncService.Sync(apiClient, sessionizeApiKey, repo);
            }
        }
    }
}
