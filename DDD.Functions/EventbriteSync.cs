using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DDD.Core.Eventbrite;
using DDD.Functions.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
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
            [BindConferenceConfig] ConferenceConfig conference,
            [BindKeyDatesConfig] KeyDatesConfig keyDates,
            [BindEventbriteSyncConfig] EventbriteSyncConfig eventbrite
        )
        {
            if (keyDates.After(x => x.StopSyncingEventbriteFromDate, TimeSpan.FromMinutes(10)))
            {
                log.LogInformation("EventbriteSync sync date passed");
                return;
            }

            var ids = new List<string>();
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", eventbrite.ApiKey);

            var (orders, hasMoreItems, continuation) = await GetOrdersAsync(http,
                $"https://www.eventbriteapi.com/v3/events/{eventbrite.EventId}/orders/");
            ids.AddRange(orders.Select(o => o.Id));
            while (hasMoreItems)
            {
                (orders, hasMoreItems, continuation) = await GetOrdersAsync(http,
                    $"https://www.eventbriteapi.com/v3/events/{eventbrite.EventId}/orders/?continuation={continuation}");
                ids.AddRange(orders.Select(o => o.Id));
            }

            var repo = await eventbrite.GetRepositoryAsync();
            var existingOrders = await repo.GetAllAsync(conference.ConferenceInstance);

            // Taking up to 100 records to meet Azure Storage Bulk Operation limit
            var newOrders = ids.Except(existingOrders.Select(x => x.OrderId).ToArray()).Distinct().Take(100).ToArray();
            log.LogInformation(
                "Found {existingCount} existing orders and {currentCount} current orders. Inserting {newCount} new orders.",
                existingOrders.Count, ids.Count, newOrders.Count());
            await repo.CreateBatchAsync(newOrders.Select(o => new EventbriteOrder(conference.ConferenceInstance, o))
                .ToArray());
        }

        private static async Task<(Order[], bool, string)> GetOrdersAsync(HttpClient http, string eventbriteUrl)
        {
            var response = await http.GetAsync(eventbriteUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsAsync<PaginatedEventbriteOrderResponse>();
            return (content.Orders, content.Pagination.HasMoreItems, content.Pagination.Continuation);
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
        [JsonProperty("has_more_items")] public bool HasMoreItems { get; set; }
    }
}