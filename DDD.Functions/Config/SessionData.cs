using System.Threading.Tasks;
using DDD.Core.AzureStorage;
using Microsoft.WindowsAzure.Storage;

namespace DDD.Functions.Config
{
    public static class SessionData
    {
        public static async Task<(ITableStorageRepository<SessionEntity>, ITableStorageRepository<PresenterEntity>)> GetSessionRepositoryAsync(string connectionString, string sessionsTable, string presentersTable)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var sessionsRepo = new TableStorageRepository<SessionEntity>(storageAccount, sessionsTable);
            var presentersRepo = new TableStorageRepository<PresenterEntity>(storageAccount, presentersTable);
            await sessionsRepo.InitializeAsync();
            await presentersRepo.InitializeAsync();

            return (sessionsRepo, presentersRepo);
        }

        public static async Task<ITableStorageRepository<NotifiedSessionEntity>> GetNotifiedSessionRepositoryAsync(string connectionString, string table)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var notifiedSessionRepo = new TableStorageRepository<NotifiedSessionEntity>(storageAccount, table);
            await notifiedSessionRepo.InitializeAsync();
            return notifiedSessionRepo;
        }

        public static Task<ITableStorageRepository<NotifiedSessionEntity>> GetNotifiedSessionRepositoryAsync(this NewSessionNotificationConfig config)
        {
            return GetNotifiedSessionRepositoryAsync(config.ConnectionString, config.NotifiedSessionsTable);
        }

        public static Task<(ITableStorageRepository<SessionEntity>, ITableStorageRepository<PresenterEntity>)> GetSubmissionRepositoryAsync(this SubmissionsConfig config)
        {
            return GetSessionRepositoryAsync(config.ConnectionString, config.SubmissionsTable, config.SubmittersTable);
        }

        public static Task<(ITableStorageRepository<SessionEntity>, ITableStorageRepository<PresenterEntity>)> GetSessionRepositoryAsync(this SessionsConfig config)
        {
            return GetSessionRepositoryAsync(config.ConnectionString, config.SessionsTable, config.PresentersTable);
        }
    }
}
