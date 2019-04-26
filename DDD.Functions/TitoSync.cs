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
        [FunctionName("TitoSync")]
        public static async Task Run(
            [TimerTrigger("%TitoSyncSchedule%")]
            TimerInfo timer,
            ILogger log,
            [BindConferenceConfig] ConferenceConfig conference,
            [BindKeyDatesConfig] KeyDatesConfig keyDates,
            [BindTitoSyncConfig] TitoSyncConfig config
        )
        {
            if (keyDates.After(x => x.StopSyncingTitoFromDate, TimeSpan.FromMinutes(10)))
            {
                log.LogInformation("TitoSync sync date passed");
                return;
            }
            
            var ids = new List<string>();
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", $"token={config.ApiKey}");

            var (tickets, hasMoreItems, nextPage) = await GetTicketsAsync(httpClient, config, log);
            
            if (tickets != null && tickets.Any())
            {
                ids.AddRange(tickets.Select(o => o.Id));
                log.LogInformation("Retrieved {ticketsCount} tickets from Tito.", tickets.Count());
            }
            
            while (hasMoreItems)
            {
                (tickets, hasMoreItems, nextPage) = await GetTicketsAsync(httpClient, config, log, nextPage.Value);
                if (tickets != null && tickets.Any())
                {
                    log.LogInformation("Found more {ticketsCount} tickets from Tito.", tickets.Count());
                    ids.AddRange(tickets.Select(o => o.Id));
                }
            }
            
            var repo = await config.GetRepositoryAsync();
            var existingTickets = await repo.GetAllAsync(conference.ConferenceInstance);

            // Taking up to 100 records to meet Azure Storage Bulk Operation limit
            var newTickets = ids.Except(existingTickets.Select(x => x.TicketId).ToArray()).Distinct().Take(100).ToArray();
            log.LogInformation(
                "Found {existingCount} existing tickets and {currentCount} current tickets. Inserting {newCount} new tickets.",
                existingTickets.Count, ids.Count, newTickets.Count());
            await repo.CreateBatchAsync(newTickets.Select(o => new TitoTicket(conference.ConferenceInstance, o))
                .ToArray());
        }

        private static async Task<(Ticket[], bool, int?)> GetTicketsAsync(HttpClient httpClient, TitoSyncConfig config, ILogger log, int pageNumber = 1)
        {
            var titoUrl = $"https://api.tito.io/v3/{config.AccountId}/{config.EventId}/tickets?page={pageNumber}";
            var response = await httpClient.GetAsync(titoUrl);
            if (response.IsSuccessStatusCode) 
            {
                try
                {
                    var formatters = new MediaTypeFormatterCollection();
                    formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.api+json"));
                    var content = await response.Content.ReadAsAsync<PaginatedTitoTicketsResponse>(formatters);
                    
                    return (content.Tickets, content.Meta.HasMoreItems, content.Meta.NextPage);
                }
                catch(Exception ex)
                {
                    log.LogCritical("Error reading Tito response.", ex);
                }
            }
            else 
            {
                log.LogCritical("Error connecting to Tito with http response: {reason}. The dump of the response: ", response.StatusCode, response.Content.ReadAsStringAsync());
            }
            return (null, false, null);
        }
    }

    public class PaginatedTitoTicketsResponse
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }
        
        [JsonProperty("tickets")]
        public Ticket[] Tickets { get; set; }
    }

    public class Ticket
    {
        [JsonProperty("reference")]
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