using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Polly;

namespace DDD.Core.DocumentDb
{
    public class DocumentDbRepository<T> where T : class
    {
        private readonly DocumentClient _client;
        private readonly string _databaseId;
        private readonly string _collectionId;

        public DocumentDbRepository(DocumentClient client, string databaseId, string collectionId)
        {
            _databaseId = databaseId;
            _collectionId = collectionId;
            _client = client;
        }

        public async Task InitializeAsync()
        {
            await Policy
                .Handle<DocumentClientException>()
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt))
                ).ExecuteAsync(async () =>
                {
                    await CreateDatabaseIfNotExistsAsync();
                    await CreateCollectionIfNotExistsAsync();
                });
        }

        public async Task<T> GetItemAsync(string id)
        {
            try
            {
                Document document = await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<IEnumerable<T>> GetAllItemsAsync()
        {
            IDocumentQuery<T> query = _client.CreateDocumentQuery<T>(
                    UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
                    new FeedOptions { MaxItemCount = -1 })
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IDocumentQuery<T> query = _client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId),
                new FeedOptions { MaxItemCount = -1 })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public async Task<Document> CreateItemAsync(T item)
        {
            return await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId), item, disableAutomaticIdGeneration: true);
        }

        public async Task<Document> UpdateItemAsync(string id, T item)
        {
            return await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id), item);
        }

        public async Task DeleteItemAsync(string id)
        {
            await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
        }

        private async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_databaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _client.CreateDatabaseAsync(new Database { Id = _databaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await _client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(_databaseId),
                        new DocumentCollection { Id = _collectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
