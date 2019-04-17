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
        private static HttpClient HttpClient;
        private static ILogger Logger;

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
            TitoSyncConfig = titoSyncConfig;
            Logger = log;

            if (keyDates.After(x => x.StopSyncingTitoFromDate, TimeSpan.FromMinutes(10)))
            {
                Logger.LogInformation("TitoSync sync date passed");
                return;
            }
            
            var ids = new List<string>();
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", $"token={titoSyncConfig.ApiKey}");

            var (registrations, hasMoreItems, nextPage) = await GetRegistrationsAsync();
            
            if (registrations != null && registrations.Any())
            {
                ids.AddRange(registrations.Select(o => o.Id));
                Logger.LogInformation("Retrieved {registrationsCount} orders from Tito.", registrations.Count());
            }
            
            while (hasMoreItems)
            {
                (registrations, hasMoreItems, nextPage) = await GetRegistrationsAsync(nextPage.Value);
                if (registrations != null && registrations.Any())
                {
                    Logger.LogInformation("Found more {registrationsCount} orders from Tito.", registrations.Count());
                    ids.AddRange(registrations.Select(o => o.Id));
                }
            }
            
            var repo = await titoSyncConfig.GetRepositoryAsync();
            var existingOrders = await repo.GetAllAsync(conference.ConferenceInstance);

            // Taking up to 100 records to meet Azure Storage Bulk Operation limit
            var newOrders = ids.Except(existingOrders.Select(x => x.OrderId).ToArray()).Distinct().Take(100).ToArray();
            Logger.LogInformation(
                "Found {existingCount} existing orders and {currentCount} current orders. Inserting {newCount} new orders.",
                existingOrders.Count, ids.Count, newOrders.Count());
            await repo.CreateBatchAsync(newOrders.Select(o => new TitoOrder(conference.ConferenceInstance, o))
                .ToArray());
        }

        private static async Task<(Registration[], bool, int?)> GetRegistrationsAsync(int pageNumber = 1)
        {
            var titoUrl = $"https://api.tito.io/v3/{TitoSyncConfig.AccountId}/{TitoSyncConfig.EventId}/registrations?page={pageNumber}";
            var response = await HttpClient.GetAsync(titoUrl);
            if(response.IsSuccessStatusCode) 
            {
                try
                {
                    var formatters = new MediaTypeFormatterCollection();
                    formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.api+json"));
                    var content = await response.Content.ReadAsAsync<PaginatedTitoOrderResponse>(formatters);
                    
                    return (content.Registrations, content.Meta.HasMoreItems, content.Meta.NextPage);
                }
                catch(Exception ex)
                {
                    Logger.LogCritical("Error fomratting/reading Tito response. ", ex);
                }
            }
            else 
            {
                Logger.LogCritical("Error connecting to Tito with http response: {reason}. The dump of the response: ", response.StatusCode, response.Content.ReadAsStringAsync());
            }
            return (null, false, null);
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