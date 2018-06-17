using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.AzureStorage
{
    public static class AzureStorageExtensions
    {
        public static async Task<List<T>> GetAllByPartitionKeyAsync<T>(this CloudTable table, string partitionKey) where T : ITableEntity, new()
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrEmpty(partitionKey))
                throw new ArgumentNullException(nameof(partitionKey));

            var query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            TableQuerySegment<T> querySegment = null;
            var returnList = new List<T>();

            do
            {
                querySegment = await table.ExecuteQuerySegmentedAsync(query, querySegment?.ContinuationToken);
                returnList.AddRange(querySegment);
            } while (querySegment.ContinuationToken != null);

            return returnList;
        }
    }
}
