using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DDD.Core.AzureStorage;
using DDD.Functions.Config;
using Flurl.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace DDD.Functions
{
    public static class EventbriteSync
    {
        [FunctionName("EventbriteSync")]
        public static async Task Run(
            [TimerTrigger("%EventbriteSyncSchedule%")]
            TimerInfo timer,
            ILogger log,
            [BindEventbriteSyncConfig]
            EventbriteSyncConfig config
        )
        {
            if (config.Now > config.StopSyncingEventbriteFromDate.AddMinutes(10))
            {
                log.LogInformation("EventbriteSync sync date passed");
                return;
            }

            var client = new FlurlClient($"https://www.eventbriteapi.com/v3/events/{config.EventId}/orders").WithOAuthBearerToken(config.EventbriteApiKey);

            var ids = new List<string>();
            var (orders, hasMoreItems, continuation) = await GetOrdersAsync(client);
            ids.AddRange(orders.Select(o => o.Id));
            while (hasMoreItems)
            {
                (orders, hasMoreItems, continuation) = await GetOrdersAsync(client, continuation);
                ids.AddRange(orders.Select(o => o.Id));
            }

            var account = CloudStorageAccount.Parse(config.ConnectionString);
            var table = account.CreateCloudTableClient().GetTableReference(config.EventbriteTable);
            await table.CreateIfNotExistsAsync();
            var existingOrders = await table.GetAllByPartitionKeyAsync<EventbriteOrder>(config.ConferenceInstance);

            // Taking up to 100 records to meet Azure Storage Bulk Operation limit
            var newOrders = ids.Except(existingOrders.Select(x => x.OrderId).ToArray()).Distinct().Take(100).ToArray();
            log.LogInformation("Found {existingCount} existing orders and {currentCount} current orders. Inserting {newCount} new orders.", existingOrders.Count, ids.Count, newOrders.Count());

            if (newOrders.Length > 0)
            {
                var batch = new TableBatchOperation();
                newOrders.ToList().ForEach(o => batch.Add(TableOperation.Insert(new EventbriteOrder(config.ConferenceInstance, o))));
                await table.ExecuteBatchAsync(batch);
            }
        }

        private static async Task<(Order[], bool, string)> GetOrdersAsync(IFlurlClient client, string continuation = null)
        {
            var urlSegments = continuation != null ? new object[]{"?continuation=" + continuation} : new object[]{};
            var response = await client.Request(urlSegments).GetJsonAsync<PaginatedEventbriteOrderResponse>();
            return (response.Orders, response.Pagination.HasMoreItems, response.Pagination.Continuation);
        }
    }

    public class PaginatedEventbriteOrderResponse
    {
        public Pagination Pagination { get; set; }
        public Order[] Orders { get; set; }
    }

    public class Order
    {
        public string Id { get; set; }
    }

    public class Pagination
    {
        public string Continuation { get; set; }
        [JsonProperty("has_more_items")]
        public bool HasMoreItems { get; set; }
    }

    public class EventbriteOrder : TableEntity
    {
        public EventbriteOrder() {}

        public EventbriteOrder(string conferenceInstance, string orderNumber)
        {
            PartitionKey = conferenceInstance;
            RowKey = orderNumber;
        }

        public string OrderId => RowKey;
    }
}
