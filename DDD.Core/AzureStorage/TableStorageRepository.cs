using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.AzureStorage
{
    public interface ITableStorageRepository<T> where T : class, ITableEntity, new()
    {
        Task InitializeAsync();
        Task<T> GetAsync(string partitionKey, string rowKey);
        Task<IEnumerable<T>> GetAllAsync(string partitionKey = null, string rowKey = null);
        Task CreateAsync(T item);
        Task UpdateAsync(T item);
        Task DeleteAsync(string partitionKey, string rowKey);
    }

    public class TableStorageRepository<T> : ITableStorageRepository<T> where T : class, ITableEntity, new()
    {
        private readonly CloudTable _table;

        public TableStorageRepository(CloudStorageAccount storageAccount, string tableName)
        {
            var client = storageAccount.CreateCloudTableClient();
            _table = client.GetTableReference(tableName);
        }

        public async Task InitializeAsync()
        {
            await _table.CreateIfNotExistsAsync();
        }

        public async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            var record = await _table.ExecuteAsync(TableOperation.Retrieve<T>(partitionKey, rowKey));
            return record.Result as T;
            // todo: return null if 404
        }

        public async Task<IEnumerable<T>> GetAllAsync(string partitionKey = null, string rowKey = null)
        {
            var query = new TableQuery<T>();
            TableQuerySegment<T> querySegment = null;
            var returnList = new List<T>();

            if (partitionKey != null)
                query = query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            if (rowKey != null)
                query = query.Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey));

            do
            {
                querySegment = await _table.ExecuteQuerySegmentedAsync(query, querySegment?.ContinuationToken);
                returnList.AddRange(querySegment);
            } while (querySegment.ContinuationToken != null);

            return returnList;
        }

        public async Task CreateAsync(T item)
        {
            await _table.ExecuteAsync(TableOperation.Insert(item));
        }

        public async Task UpdateAsync(T item)
        {
            await _table.ExecuteAsync(TableOperation.Replace(item));
        }

        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            var existing = await GetAsync(partitionKey, rowKey);
            await _table.ExecuteAsync(TableOperation.Delete(existing));
        }
    }
}
