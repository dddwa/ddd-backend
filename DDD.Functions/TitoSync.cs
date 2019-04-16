using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DDD.Core.Tito;
using DDD.Functions.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DDD.Functions
{
    public static class TitoSync
    {
        private static TitoSyncConfig TitoSyncConfig;

        [FunctionName("TitoSync")]
        public static async Task Run(
            [TimerTrigger("%TitoSyncSchedule%")]
            TimerInfo timer,
            ILogger log,
            [BindConferenceConfig] ConferenceConfig conference,
            [BindKeyDatesConfig] KeyDatesConfig keyDates,
            [BindTitoSyncConfig] TitoSyncConfig titoSyncConfig
        )
        {
            if (keyDates.After(x => x.StopSyncingTitoFromDate, TimeSpan.FromMinutes(10)))
            {
                log.LogInformation("TitoSync sync date passed");
                return;
            }

            TitoSyncConfig = titoSyncConfig;
            var ids = new List<string>();
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", $"token={titoSyncConfig.ApiKey}");

            var (registrations, hasMoreItems, nextPage) = await GetRegistrationsAsync(http);
            
            if(registrations!= null && registrations.Count() > 0)
            {
                ids.AddRange(registrations.Select(o => o.Id));
                log.LogInformation("Retrieved and inserted {registrationsCount} orders from Tito.", registrations.Count());
            }

            while (hasMoreItems && nextPage.HasValue)
            {
                (registrations, hasMoreItems, nextPage) = await GetRegistrationsAsync(http, nextPage.Value);
                log.LogInformation("Found more ti.to orders. Retrieving and inserting {registrationsCount} orders from Tito.", registrations.Count());
                ids.AddRange(registrations.Select(o => o.Id));
            }

            var repo = await titoSyncConfig.GetRepositoryAsync();
            var existingOrders = await repo.GetAllAsync(conference.ConferenceInstance);

            // Taking up to 100 records to meet Azure Storage Bulk Operation limit
            var newOrders = ids.Except(existingOrders.Select(x => x.OrderId).ToArray()).Distinct().Take(100).ToArray();
            log.LogInformation(
                "Found {existingCount} existing orders and {currentCount} current orders. Inserting {newCount} new orders.",
                existingOrders.Count, ids.Count, newOrders.Count());
            await repo.CreateBatchAsync(newOrders.Select(o => new TitoOrder(conference.ConferenceInstance, o))
                .ToArray());
        }

        private static async Task<(Registration[], bool, int?)> GetRegistrationsAsync(HttpClient http, int pageNumber = 1)
        {
            var titoUrl = $"https://api.tito.io/v3/{TitoSyncConfig.AccountId}/{TitoSyncConfig.EventId}/registrations?page={pageNumber}";
            var response = await http.GetAsync(titoUrl);
            response.EnsureSuccessStatusCode();

            var formatters = new MediaTypeFormatterCollection();
            formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.api+json"));
            var content = await response.Content.ReadAsAsync<PaginatedTitoOrderResponse>(formatters);
            
            return (content.Registrations, content.Meta.HasMoreItems, content.Meta.NextPage);
        }
    }

    public class PaginatedTitoOrderResponse
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }
        
        [JsonProperty("data")]
        public Registration[] Registrations { get; set; }
    }

    public class Registration
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Meta
    {
        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }

        [JsonProperty("next_page")]
        public int? NextPage { get; set; }

        [JsonProperty("total_pages")]
        public int TotalPages { get; set; }

        [JsonIgnore] public bool HasMoreItems => NextPage.HasValue || CurrentPage < TotalPages;
    }
}