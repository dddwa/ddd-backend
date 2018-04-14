using System;
using System.Net;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Polly;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public static class EmptyDocumentDb
    {
        private const string DocumentDbEmulator = "AccountEndpoint=https://localhost:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        public static async Task<DocumentDbRepository<T>> InitializeAsync<T>(string testDatabaseId, string testCollectionId)
            where T : class
        {
            var documentClient = DocumentDbAccount.Parse(DocumentDbEmulator);

            try
            {
                await Policy
                    .Handle<DocumentClientException>(e => e.StatusCode != HttpStatusCode.NotFound)
                    .WaitAndRetryAsync(3, retryAttempt =>
                        TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt))
                    ).ExecuteAsync(async () => await documentClient.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(testDatabaseId)));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode != HttpStatusCode.NotFound)
                    throw;
            }

            var repo = new DocumentDbRepository<T>(documentClient, testDatabaseId, testCollectionId);
            await repo.InitializeAsync();
            return repo;
        }
    }
}
