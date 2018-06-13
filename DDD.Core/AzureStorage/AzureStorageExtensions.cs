using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.AzureStorage
{
    public static class AzureStorageExtensions
    {
        public static async Task<List<T>> GetAllByPartitionKeyAsync<T>(this CloudTable table, string partitionKey) where T : ITableEntity, new()
        {
            var query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            TableQuerySegment<T> querySegment = null;
            var returnList = new List<T>();

            while (querySegment == null || querySegment.ContinuationToken != null)
            {
                querySegment = await table.ExecuteQuerySegmentedAsync(query, querySegment?.ContinuationToken);
                returnList.AddRange(querySegment);
            }

            return returnList;
        }
    }
}
