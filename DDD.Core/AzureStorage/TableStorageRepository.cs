using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace DDD.Core.AzureStorage
{
    public interface ITableStorageRepository<T> where T : class, ITableEntity, new()
    {
        Task InitializeAsync();
        Task<T> GetAsync(string partitionKey, string rowKey);
        Task<IList<T>> GetAllAsync(string partitionKey = null, string rowKey = null);
        Task CreateAsync(T item);
        Task UpdateAsync(T item);
        Task DeleteAsync(string partitionKey, string rowKey);
        Task CreateBatchAsync(IList<T> batch);
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
        }

        public async Task<IList<T>> GetAllAsync(string partitionKey = null, string rowKey = null)
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

        public async Task CreateBatchAsync(IList<T> batch)
        {
            if (batch.Count == 0)
                return;

            if (batch.Count > 100)
                throw new InvalidOperationException($"Attempt to insert batch operation with too many records ({batch.Count}), max of 100.");
            if (batch.Any(x => x.PartitionKey != batch[0].PartitionKey))
                throw new InvalidOperationException("Attempt to insert batch operation with records that have a mix of partition keys.");

            var batchOperation = new TableBatchOperation();
            batch.ToList().ForEach(x => batchOperation.Add(TableOperation.Insert(x)));
            await _table.ExecuteBatchAsync(batchOperation);
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
