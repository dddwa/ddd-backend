using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using DDD.Core.DocumentDb;
using DDD.Sessionize;
using DDD.Functions.Config;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System;
using System.Net.Http;

namespace DDD.Functions
{
    public static class GetVotes
    {
        [FunctionName("GetVotes")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req,
            TraceWriter log,
            [BindGetVotesConfig]
            GetVotesConfig config)
        {
            var http = new HttpClient();
            var eventId = "44602457150";
            var bearer = "WKNWNIK3D4RPIBUQUUP3";
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);
            var r = await http.GetAsync($"https://www.eventbriteapi.com/v3/events/{eventId}/orders");
            //var content = await r.Content.ReadAsAsync<>();



            // Get sessions
            var documentDbClient = DocumentDbAccount.Parse(config.SessionsConnectionString);
            var repo = new DocumentDbRepository<SessionOrPresenter>(documentDbClient, config.CosmosDatabaseId, config.CosmosCollectionId);
            await repo.InitializeAsync();
            var all = await repo.GetAllItemsAsync();

            // Get votes
            var account = CloudStorageAccount.Parse(config.VotingConnectionString);
            var table = account.CreateCloudTableClient().GetTableReference(config.VotingTable);
            await table.CreateIfNotExistsAsync();
            var votes = await GetByPartitionKey(table, config.ConferenceInstance);

            var presenters = all.Where(x => x.Presenter != null).Select(x => x.Presenter).ToArray();
            var sessions = all.Where(x => x.Session != null)
                .Select(x => x.Session)
                .Select(s => new SessionWithVotes
                {
                    Id = s.Id.ToString(),
                    Title = s.Title,
                    Abstract = s.Abstract,
                    Format = s.Format,
                    Level = s.Level,
                    Tags = s.Tags,
                    Presenters = s.PresenterIds.Select(pId => presenters.Where(p => p.Id == pId).Select(p => new Presenter
                        {
                            Id = p.Id.ToString(),
                            Name = p.Name,
                            Tagline = p.Tagline,
                            Bio = p.Bio,
                            ProfilePhotoUrl = p.ProfilePhotoUrl,
                            TwitterHandle = p.TwitterHandle,
                            WebsiteUrl = p.WebsiteUrl
                        }).Single()).ToArray(),
                    CreatedDate = s.CreatedDate,
                    ModifiedDate = s.ModifiedDate,
                    IsUnderrepresented = s.DataFields.ContainsKey("Are you a member of any underrepresented groups?")
                        ? !string.IsNullOrEmpty(s.DataFields["Are you a member of any underrepresented groups?"])
                        : false,
                    Pronoun = s.DataFields["Your preferred pronoun"],
                    JobRole = s.DataFields["How would you identify your job role?"],
                    SpeakingExperience = s.DataFields["How much speaking experience do you have?"],
                })
                .OrderBy(s => s.Title)
                .ToArray();

            votes.SelectMany(v => JsonConvert.DeserializeObject<string[]>(v.SessionIds).ToArray())
                .GroupBy(x => x)
                .Select(group => new
                {
                    SessionId = group.Key,
                    VoteCount = group.Count()
                })
                .ToList()
                .ForEach(v => sessions.Single(s => s.Id == v.SessionId).TotalVotes = v.VoteCount);

            var response = new GetVotesResponse
            {
                Sessions = sessions.OrderByDescending(s => s.TotalVotes).ToArray()
            };
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new DefaultContractResolver();

            return new JsonResult(response, settings);
        }

        public static async Task<List<Vote>> GetByPartitionKey(CloudTable table, string conferenceInstance)
        {
            var query = new TableQuery<Vote>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, conferenceInstance));
            TableQuerySegment<Vote> querySegment = null;
            var returnList = new List<Vote>();

            while (querySegment == null || querySegment.ContinuationToken != null)
            {
                querySegment = await table.ExecuteQuerySegmentedAsync(query, querySegment != null ? querySegment.ContinuationToken : null);
                returnList.AddRange(querySegment);
            }

            return returnList;
        }
    }

    public class SessionWithVotes
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public string Format { get; set; }
        public string Level { get; set; }
        public string[] Tags { get; set; }
        public Presenter[] Presenters { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? ModifiedDate { get; set; }
        public bool IsUnderrepresented { get; set; }
        public string Pronoun { get; set; }
        public string JobRole { get; set; }
        public string SpeakingExperience { get; set; }

        public int TotalVotes { get; set; }
    }

    public class Presenter
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Tagline { get; set; }
        public string Bio { get; set; }
        public string ProfilePhotoUrl { get; set; }
        public string TwitterHandle { get; set; }
        public string WebsiteUrl { get; set; }
    }

    public class GetVotesResponse
    {
        public SessionWithVotes[] Sessions { get; set; }
    }
}
