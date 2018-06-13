using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DDD.Core.AzureStorage;
using DDD.Functions.Config;
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
            if (config.Now > config.StopSyncingEventbriteFromDate)
            {
                log.LogInformation("EventbriteSync sync date passed");
                return;
            }

            var ids = new List<string>();
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.EventbriteApiKey);
            var response = await http.GetAsync($"https://www.eventbriteapi.com/v3/events/{config.EventId}/orders");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsAsync<PaginatedEventbriteOrderResponse>();
            content.Orders.ToList().ForEach(o => ids.Add(o.Id));
            while (content.Pagination.HasMoreItems)
            {
                response = await http.GetAsync($"https://www.eventbriteapi.com/v3/events/{config.EventId}/orders?continuation={content.Pagination.Continuation}");
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsAsync<PaginatedEventbriteOrderResponse>();
                content.Orders.ToList().ForEach(o => ids.Add(o.Id));
            }

            var account = CloudStorageAccount.Parse(config.ConnectionString);
            var table = account.CreateCloudTableClient().GetTableReference(config.EventbriteTable);
            await table.CreateIfNotExistsAsync();
            var existingOrders = await table.GetAllByPartitionKeyAsync<EventbriteOrder>(config.ConferenceInstance);

            var newOrders = ids.Except(existingOrders.Select(x => x.OrderId).ToArray()).Distinct().Take(100).ToArray();
            log.LogInformation("Found {existingCount} existing orders and {currentCount} current orders. Inserting {newCount} new orders.", existingOrders.Count, ids.Count, newOrders.Count());

            if (newOrders.Length > 0)
            {
                var batch = new TableBatchOperation();
                newOrders.ToList().ForEach(o => batch.Add(TableOperation.Insert(new EventbriteOrder(config.ConferenceInstance, o))));
                await table.ExecuteBatchAsync(batch);
            }
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
