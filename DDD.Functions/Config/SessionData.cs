using System.Threading.Tasks;
using DDD.Core.AzureStorage;
using Microsoft.WindowsAzure.Storage;

namespace DDD.Functions.Config
{
    public static class SessionData
    {
        public static async Task<ITableStorageRepository<NotifiedSessionEntity>> GetRepositoryAsync(this NewSessionNotificationConfig config)
        {
            var repo = new TableStorageRepository<NotifiedSessionEntity>(CloudStorageAccount.Parse(config.ConnectionString), config.Table);
            await repo.InitializeAsync();
            return repo;
        }

        public static Task<(ITableStorageRepository<SessionEntity>, ITableStorageRepository<PresenterEntity>)> GetRepositoryAsync(this SubmissionsConfig config)
        {
            return GetSessionRepositoryAsync(config.ConnectionString, config.SubmissionsTable, config.SubmittersTable);
        }

        public static Task<(ITableStorageRepository<SessionEntity>, ITableStorageRepository<PresenterEntity>)> GetRepositoryAsync(this SessionsConfig config)
        {
            return GetSessionRepositoryAsync(config.ConnectionString, config.SessionsTable, config.PresentersTable);
        }

        public static async Task<ITableStorageRepository<EventbriteOrder>> GetRepositoryAsync(this EventbriteSyncConfig config)
        {
            var repo = new TableStorageRepository<EventbriteOrder>(CloudStorageAccount.Parse(config.ConnectionString), config.Table);
            await repo.InitializeAsync();
            return repo;
        }

        public static async Task<ITableStorageRepository<AppInsightsVotingUser>> GetRepositoryAsync(this AppInsightsSyncConfig config)
        {
            var repo = new TableStorageRepository<AppInsightsVotingUser>(CloudStorageAccount.Parse(config.ConnectionString), config.Table);
            await repo.InitializeAsync();
            return repo;
        }

        public static async Task<ITableStorageRepository<Vote>> GetRepositoryAsync(this VotingConfig config)
        {
            var repo = new TableStorageRepository<Vote>(CloudStorageAccount.Parse(config.ConnectionString), config.Table);
            await repo.InitializeAsync();
            return repo;
        }

        public static async Task<(ITableStorageRepository<ConferenceFeedbackEntity>, ITableStorageRepository<SessionFeedbackEntity>)> GetRepositoryAsync(this FeedbackConfig feedback)
        {
            var storageAccount = CloudStorageAccount.Parse(feedback.ConnectionString);
            var conferenceFeedbackRepository = new TableStorageRepository<ConferenceFeedbackEntity>(storageAccount, feedback.ConferenceFeedbackTable);
            var sessionFeedbackRepository = new TableStorageRepository<SessionFeedbackEntity>(storageAccount, feedback.SessionFeedbackTable);
            await conferenceFeedbackRepository.InitializeAsync();
            await sessionFeedbackRepository.InitializeAsync();

            return (conferenceFeedbackRepository, sessionFeedbackRepository);
        }

        private static async Task<(ITableStorageRepository<SessionEntity>, ITableStorageRepository<PresenterEntity>)> GetSessionRepositoryAsync(string connectionString, string sessionsTable, string presentersTable)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var sessionsRepo = new TableStorageRepository<SessionEntity>(storageAccount, sessionsTable);
            var presentersRepo = new TableStorageRepository<PresenterEntity>(storageAccount, presentersTable);
            await sessionsRepo.InitializeAsync();
            await presentersRepo.InitializeAsync();

            return (sessionsRepo, presentersRepo);
        }
    }
}
