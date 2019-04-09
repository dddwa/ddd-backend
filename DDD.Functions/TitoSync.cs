using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

            var ids = new List<string>();
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", $"token={titoSyncConfig.ApiKey}");

            var (registrations, hasMoreItems, nextPage) = await GetRegistrationsAsync(http,
                $"https://api.tito.io/v3/{titoSyncConfig.AccountId}/{titoSyncConfig.EventId}/registrations/");
            ids.AddRange(registrations.Select(o => o.Id));

            while (hasMoreItems)
            {
                (registrations, hasMoreItems, nextPage) = await GetRegistrationsAsync(http,
                    $"https://api.tito.io/v3/{titoSyncConfig.AccountId}/{titoSyncConfig.EventId}/registrations?page={nextPage}");
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

        private static async Task<(Registration[], bool, int?)> GetRegistrationsAsync(HttpClient http, string titoUrl)
        {
            var response = await http.GetAsync(titoUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsAsync<PaginatedTitoOrderResponse>();
            return (content.Registrations, content.Meta.HasMoreItems, content.Meta.NextPage);
        }
    }

    public class PaginatedTitoOrderResponse
    {
        public Meta Meta { get; set; }
        public Registration[] Registrations { get; set; }
    }

    public class Registration
    {
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