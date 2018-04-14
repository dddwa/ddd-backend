using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using Microsoft.Azure.Documents;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public class DocumentDbRepositoryMock<T> : IDocumentDbRepository<T> where T : class
    {
        private List<T> _storage = new List<T>();

        public Task InitializeAsync()
        {
            _storage = new List<T>();
            return Task.FromResult(0);
        }

        public Task<T> GetItemAsync(string id)
        {
            return Task.FromResult(_storage.SingleOrDefault(x => x.GetType().GetProperty("Id")?.GetValue(x) as string == id));
        }

        public Task<IEnumerable<T>> GetAllItemsAsync()
        {
            return Task.FromResult(_storage.AsEnumerable());
        }

        public Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            return Task.FromResult(_storage.Where(predicate.Compile()).AsEnumerable());
        }

        public Task<Document> CreateItemAsync(T item)
        {
            _storage.Add(item);
            return Task.FromResult(new Document());
        }

        public async Task<Document> UpdateItemAsync(string id, T item)
        {
            var existing = await GetItemAsync(id);
            var existingIndex = _storage.IndexOf(existing);
            _storage[existingIndex] = item;
            return new Document();
        }

        public async Task DeleteItemAsync(string id)
        {
            var existing = await GetItemAsync(id);
            _storage.Remove(existing);
        }
    }
}
