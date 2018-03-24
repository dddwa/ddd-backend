using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DDD.Core.Domain;
using DDD.SessionizeWorker.DocumentDb;
using DDD.SessionizeWorker.Sessionize;
using Microsoft.Azure.Documents;
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
            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient);
                var sessionizeData = await apiClient.GetAllData("mdhxdmti");
                var adapter = new SessionizeAdapter.SessionizeAdapter();
                var source = adapter.Convert(sessionizeData);
                var sourcePresenters = source.Item2;
                var sourceSessions = source.Item1;

                var cosmosSettings = config.GetSection("Cosmos").Get<CosmosSettings>();

                var repo = new DocumentDbRepository<SessionOrPresenter>(config.GetConnectionString("CosmosEndpoint"),
                    config.GetConnectionString("CosmosKey"), cosmosSettings.DatabaseId, cosmosSettings.CollectionId);
                await repo.Initialize();

                var destination = (await repo.GetAllItemsAsync()).ToArray();
                var destinationPresenters = destination.Select(x => x.GetPresenter()).Where(x => x.HasValue).Select(x => x.Value).ToArray();
                var destinationSessions = destination.Select(x => x.GetSession()).Where(x => x.HasValue).Select(x => x.Value).ToArray();

                var deletedPresenters = destinationPresenters.Except(sourcePresenters, new PresenterSync());
                var presentersToInsert = sourcePresenters.Except(destinationPresenters, new PresenterSync()).ToArray();
            }
        }
    }

    public class CosmosSettings
    {
        public string DatabaseId { get; set; }
        public string CollectionId { get; set; }
    }

    public class PresenterSync : IEqualityComparer<Presenter>
    {
        public bool Equals(Presenter x, Presenter y)
        {
            return x.ExternalId == y.ExternalId;
        }

        public int GetHashCode(Presenter obj)
        {
            return obj.ExternalId.GetHashCode();
        }
    }

    public class SessionSync : IEqualityComparer<Session>
    {
        public bool Equals(Session x, Session y)
        {
            return x.ExternalId == y.ExternalId;
        }

        public int GetHashCode(Session obj)
        {
            return obj.ExternalId.GetHashCode();
        }
    }

    public class SessionOrPresenter
    {
        public Guid Id { get; private set; }
        public Presenter Presenter { get; private set; }
        public Session Session { get; private set; }

        public SessionOrPresenter(Session session)
        {
            Session = session;
            Id = session.Id;
        }

        public SessionOrPresenter(Presenter presenter)
        {
            Presenter = presenter;
            Id = presenter.Id;
        }

        public Option<Session> GetSession()
        {
            return Option.For(Session);
        }

        public Option<Presenter> GetPresenter()
        {
            return Option.For(Presenter);
        }
    }

    public static class Option
    {
        public static Option<T> For<T>(T value) => new Option<T>(value);
    }

    public class Option<T>
    {

        public Option(T value)
        {
            Value = value;
        }

        public bool HasValue => Value != null;
        public T Value { get; }
    }
}
