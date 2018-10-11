using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DDD.Core.AppInsights;
using DDD.Functions.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace DDD.Functions
{
    public static class AppInsightsSync
    {
        [FunctionName("AppInsightsSync")]
        public static async Task Run(
            [TimerTrigger("%AppInsightsSyncSchedule%")]
            TimerInfo timer,
            ILogger log,
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindAppInsightsSyncConfig]
            AppInsightsSyncConfig appInsights,
            [BindKeyDatesConfig]
            KeyDatesConfig keyDates
        )
        {
            if (keyDates.Before(x => x.StartSyncingAppInsightsFromDate) || keyDates.After(x => x.StopSyncingAppInsightsFromDate, TimeSpan.FromMinutes(10)))
            {
                log.LogInformation("AppInsightsSync sync not active");
                return;
            }

            var http = new HttpClient();
            http.DefaultRequestHeaders.Add("x-api-key", appInsights.ApplicationKey);

            var response = await http.GetAsync($"https://api.applicationinsights.io/v1/apps/{appInsights.ApplicationId}/query?timespan={WebUtility.UrlEncode(keyDates.StartSyncingAppInsightsFrom)}%2F{WebUtility.UrlEncode(keyDates.StopSyncingAppInsightsFrom)}&query={WebUtility.UrlEncode(VotingUserQuery.Query)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsAsync<AppInsightsQueryResponse<VotingUserQuery>>();
            var currentRecords = content.Data.Select(x => new AppInsightsVotingUser(conference.ConferenceInstance, x.UserId, x.VoteId, x.StartTime)).ToArray();

            var repo = await appInsights.GetRepositoryAsync();
            var existingRecords = await repo.GetAllAsync(conference.ConferenceInstance);

            // Taking up to 100 records to meet Azure Storage Bulk Operation limit
            var newRecords = currentRecords.Except(existingRecords, new AppInsightsVotingUserComparer()).Take(100).ToArray();
            log.LogInformation("Found {existingCount} existing app insights voting users and {currentCount} current app insights voting users. Inserting {newCount} new orders.", existingRecords.Count, currentRecords.Length, newRecords.Length);
            await repo.CreateBatchAsync(newRecords);
        }
    }

    public class AppInsightsQueryResponse<T> where T: ICanHydrateFromAppInsights<T>, new()
    {
        public AppInsightsTableResponse[] Tables { get; set; }

        public T[] Data => Tables[0].Rows.Select(r => new T().Hydrate(Tables[0].Columns, r)).ToArray();
    }

    public class AppInsightsTableResponse
    {
        public string Name { get; set; }
        public AppInsightsTableResponseColumn[] Columns { get; set; }
        public string[][] Rows { get; set; }
    }

    public class AppInsightsTableResponseColumn
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public interface ICanHydrateFromAppInsights<T>
    {
        T Hydrate(AppInsightsTableResponseColumn[] columns, string[] row);
    }

    public class VotingUserQuery : ICanHydrateFromAppInsights<VotingUserQuery>
    {
        public const string Query = "customEvents | where name == \"voting:voteIdGenerated\" | project user_Id, customDimensions.startTime, customDimensions.id";

        public VotingUserQuery Hydrate(AppInsightsTableResponseColumn[] columns, string[] row)
        {
            var columnNames = columns.Select(x => x.Name).ToArray();
            UserId = row[Array.IndexOf(columnNames, "user_Id")];
            StartTime = row[Array.IndexOf(columnNames, "customDimensions_startTime")];
            VoteId = row[Array.IndexOf(columnNames, "customDimensions_id")];
            return this;
        }

        public string UserId { get; set; }
        public string StartTime { get; set; }
        public string VoteId { get; set; }
    }
}
