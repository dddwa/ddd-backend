using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DDD.Core.AzureStorage;
using DDD.Functions.Config;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Functions
{
    public static class AppInsightsSync
    {
        [FunctionName("AppInsightsSync")]
        public static async Task Run(
            [TimerTrigger("%AppInsightsSyncSchedule%")]
            TimerInfo timer,
            ILogger log,
            [BindAppInsightsSyncConfig]
            AppInsightsSyncConfig config
        )
        {
            if (config.Now > config.StopSyncingAppInsightsFromDate.AddMinutes(10))
            {
                log.LogInformation("AppInsightsSync sync date passed");
                return;
            }

            var http = new HttpClient();
            http.DefaultRequestHeaders.Add("x-api-key", config.AppInsightsApplicationKey);

            var response = await http.GetAsync($"https://api.applicationinsights.io/v1/apps/{config.AppInsightsApplicationId}/query?timespan={WebUtility.UrlEncode(config.StartSyncingAppInsightsFrom)}%2F{WebUtility.UrlEncode(config.StopSyncingAppInsightsFrom)}&query={WebUtility.UrlEncode(VotingUserQuery.Query)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsAsync<AppInsightsQueryResponse<VotingUserQuery>>();
            var currentRecords = content.Data.Select(x => new AppInsightsVotingUser(config.ConferenceInstance, x.UserId, x.VoteId, x.StartTime)).ToArray();

            var account = CloudStorageAccount.Parse(config.ConnectionString);
            var table = account.CreateCloudTableClient().GetTableReference(config.AppInsightsTable);
            await table.CreateIfNotExistsAsync();
            var existingRecords = await table.GetAllByPartitionKeyAsync<AppInsightsVotingUser>(config.ConferenceInstance);

            // Taking up to 100 records to meet Azure Storage Bulk Operation limit
            var newRecords = currentRecords.Except(existingRecords, new AppInsightsVotingUserComparer()).Take(100).ToArray();
            log.LogInformation("Found {existingCount} existing app insights voting users and {currentCount} current aapp insights voting users. Inserting {newCount} new orders.", existingRecords.Count(), currentRecords.Count(), newRecords.Count());

            if (newRecords.Length > 0)
            {
                var batch = new TableBatchOperation();
                newRecords.ToList().ForEach(u => batch.Add(TableOperation.Insert(u)));
                await table.ExecuteBatchAsync(batch);
            }
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

    public class AppInsightsVotingUser : TableEntity
    {
        public AppInsightsVotingUser() {}

        public AppInsightsVotingUser(string conferenceInstance, string userId, string voteId, string startTime)
        {
            PartitionKey = conferenceInstance;
            RowKey = Guid.NewGuid().ToString();
            UserId = userId;
            VoteId = voteId;
            StartTime = startTime;
        }

        public string UserId { get; set; }
        public string VoteId { get; set; }
        public string StartTime { get; set; }
    }

    public class AppInsightsVotingUserComparer : IEqualityComparer<AppInsightsVotingUser>
    {
        public bool Equals(AppInsightsVotingUser x, AppInsightsVotingUser y)
        {
            return x.UserId == y.UserId && x.VoteId == y.VoteId;
        }

        public int GetHashCode(AppInsightsVotingUser obj)
        {
            return (obj.UserId + "|" + obj.VoteId).GetHashCode();
        }
    }
}
