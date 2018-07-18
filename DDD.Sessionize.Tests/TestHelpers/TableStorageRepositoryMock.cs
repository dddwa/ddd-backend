using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DDD.Core.AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public class TableStorageRepositoryMock<T> : ITableStorageRepository<T> where T : class, ITableEntity, new()
    {
        private List<T> _storage = new List<T>();

        public Task InitializeAsync()
        {
            _storage = new List<T>();
            return Task.FromResult(0);
        }

        public Task<T> GetAsync(string partitionKey, string rowKey)
        {
            return Task.FromResult(_storage.SingleOrDefault(x => x.PartitionKey == partitionKey && x.RowKey == rowKey));
        }

        public Task<IEnumerable<T>> GetAllAsync(string partitionKey = null, string rowKey = null)
        {
            return Task.FromResult(_storage.Where(x => (x.PartitionKey == partitionKey || partitionKey == null) && x.RowKey == rowKey || rowKey == null));
        }

        public Task CreateAsync(T item)
        {
            _storage.Add(item);
            return Task.FromResult(0);
        }

        public async Task UpdateAsync(T item)
        {
            var existing = await GetAsync(item.PartitionKey, item.RowKey);
            var existingIndex = _storage.IndexOf(existing);
            _storage[existingIndex] = item;
        }

        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            var existing = await GetAsync(partitionKey, rowKey);
            _storage.Remove(existing);
        }
    }
}
