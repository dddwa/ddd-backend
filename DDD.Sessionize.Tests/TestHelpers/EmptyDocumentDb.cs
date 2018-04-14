using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using Microsoft.Azure.Documents.Client;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public static class EmptyDocumentDb
    {
        private const string DocumentDbEmulator = "AccountEndpoint=https://localhost:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        public static async Task<DocumentDbRepository<T>> InitializeAsync<T>(string testDatabaseId, string testCollectionId)
            where T : class
        {
            var documentClient = DocumentDbAccount.Parse(DocumentDbEmulator);
            await documentClient.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(testDatabaseId));
            var repo = new DocumentDbRepository<T>(documentClient, testDatabaseId, testCollectionId);
            await repo.InitializeAsync();
            return repo;
        }
    }
}
