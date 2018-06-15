using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using DDD.Core.DocumentDb;
using DDD.Sessionize;
using DDD.Functions.Config;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using DDD.Core.AzureStorage;

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
            // Get sessions
            var documentDbClient = DocumentDbAccount.Parse(config.SessionsConnectionString);
            var repo = new DocumentDbRepository<SessionOrPresenter>(documentDbClient, config.CosmosDatabaseId, config.CosmosCollectionId);
            await repo.InitializeAsync();
            var all = (await repo.GetAllItemsAsync()).ToArray();

            // Get votes
            var account = CloudStorageAccount.Parse(config.VotingConnectionString);
            var table = account.CreateCloudTableClient().GetTableReference(config.VotingTable);
            await table.CreateIfNotExistsAsync();
            var votes = await table.GetAllByPartitionKeyAsync<Vote>(config.ConferenceInstance);

            // Get Eventbrite ids
            var ebTable = account.CreateCloudTableClient().GetTableReference(config.EventbriteTable);
            var eventbriteOrders = await ebTable.GetAllByPartitionKeyAsync<EventbriteOrder>(config.ConferenceInstance);
            var eventbriteIds = eventbriteOrders.Select(o => o.OrderId).ToArray();

            // Analyse votes
            var analysedVotes = votes.Select(v => new AnalysedVote(v, votes, eventbriteIds)).ToArray();

            // Get summary
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
                    IsUnderrepresented = s.DataFields.ContainsKey("Are you a member of any underrepresented groups?") && !string.IsNullOrEmpty(s.DataFields["Are you a member of any underrepresented groups?"]),
                    Pronoun = s.DataFields["Your preferred pronoun"],
                    JobRole = s.DataFields["How would you identify your job role?"],
                    SpeakingExperience = s.DataFields["How much speaking experience do you have?"],
                    VoteSummary = new VoteSummary(analysedVotes.Where(v => v.Vote.GetSessionIds().Contains(s.Id.ToString())).ToArray())
                })
                .OrderBy(s => s.Title)
                .ToArray();

            var tagSummaries = sessions.SelectMany(s => s.Tags).Distinct().OrderBy(t => t)
                .Select(tag => new TagSummary
                {
                    Tag = tag,
                    VoteSummary = new VoteSummary(sessions.Where(s => s.Tags.Contains(tag)).SelectMany(s => analysedVotes.Where(v => v.Vote.SessionIds.Contains(s.Id))).ToArray())
                }).ToArray();

            var response = new GetVotesResponse
            {
                VoteSummary = new VoteSummary(analysedVotes),
                Sessions = sessions.OrderByDescending(s => s.VoteSummary.Total).ToArray(),
                TagSummaries = tagSummaries,
                Votes = analysedVotes
            };
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new DefaultContractResolver();

            return new JsonResult(response, settings);
        }
    }

    public class VoteSummary
    {
        public VoteSummary(AnalysedVote[] votes)
        {
            RawTotal = votes.Length;
            TotalWithValidTicketNumber = votes.Count(v => v.HasValidTicketNumber);
            TotalWithInvalidTicketNumber = votes.Count(v => v.HasTicketNumber && !v.HasValidTicketNumber);
            TotalWithDuplicateTicketNumber = votes.Count(v => v.HasDuplicateTicketNumber);
            TotalWithAppInsightsId = votes.Count(v => v.HasAppInsightsId);
            TotalWithDuplicateAppInsightsId = votes.Count(v => v.HasDuplicateAppInsightsId);
            TotalSuspiciousVotes = votes.Count(v => v.LooksSuspicious);
            TotalUniqueIpAddresses = votes.Select(v => v.Vote.IpAddress).Distinct().Count();
            TotalSessionsVoted = votes.Sum(v => v.Vote.GetSessionIds().Length);

            Total = Convert.ToInt32(Math.Round(TotalWithValidTicketNumber * 2 + RawTotal - TotalWithValidTicketNumber - TotalSuspiciousVotes * 0.5 - TotalWithDuplicateTicketNumber * 0.5 - TotalWithDuplicateAppInsightsId * 0.5));
        }

        public int RawTotal { get; set; }
        public int Total { get; set; }
        public int TotalWithValidTicketNumber { get; set; }
        public int TotalWithInvalidTicketNumber { get; set; }
        public int TotalWithDuplicateTicketNumber { get; set; }
        public int TotalWithAppInsightsId { get; set; }
        public int TotalWithDuplicateAppInsightsId { get; set; }
        public int TotalSuspiciousVotes { get; set; }
        public int TotalUniqueIpAddresses { get; set; }
        public int TotalSessionsVoted { get; set; }
    }

    public class TagSummary
    {
        public string Tag { get; set; }
        public VoteSummary VoteSummary { get; set; }
    }

    public class AnalysedVote : IEquatable<AnalysedVote>
    {
        public AnalysedVote(Vote vote, IList<Vote> allVotes, IList<string> validTicketNumbers)
        {
            var orderedIndices = vote.GetIndices().Select(int.Parse).OrderBy(x => x).ToArray();
            var indexGaps = orderedIndices.Select((index, i) => i == 0 ? 0 : index - orderedIndices[i - 1]).Skip(1);

            Vote = vote;
            HasTicketNumber = !string.IsNullOrEmpty(vote.TicketNumber);
            HasValidTicketNumber = HasTicketNumber && validTicketNumbers.Contains(vote.TicketNumber);
            HasDuplicateTicketNumber = HasValidTicketNumber && allVotes.Any(v => v.VoteId != vote.VoteId && v.TicketNumber == vote.TicketNumber);

            HasAppInsightsId = !string.IsNullOrEmpty(vote.VoterSessionId);
            //HasValidAppInsightsId
            HasDuplicateAppInsightsId = HasAppInsightsId && allVotes.Any(v => v.VoteId != vote.VoteId && v.VoterSessionId == vote.VoterSessionId);
            LooksSuspicious = (vote.VotingSubmittedTime - vote.VotingStartTime) < TimeSpan.FromMinutes(2)
                || indexGaps.Count(gap => gap == 1) >= 4;
        }

        public Vote Vote { get; set; }
        public bool HasTicketNumber { get; set; }
        public bool HasValidTicketNumber { get; set; }
        public bool HasDuplicateTicketNumber { get; set; }
        public bool HasAppInsightsId { get; set; }
        public bool HasValidAppInsightsId { get; set; }
        public bool HasDuplicateAppInsightsId { get; set; }
        public bool LooksSuspicious { get; set; }

        public bool Equals(AnalysedVote other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Vote.VoteId, other.Vote.VoteId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Vote.VoteId.Equals(((AnalysedVote) obj).Vote.VoteId);
        }

        public override int GetHashCode()
        {
            return (Vote != null ? Vote.GetHashCode() : 0);
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

        public VoteSummary VoteSummary { get; set; }
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
        public VoteSummary VoteSummary { get; set; }
        public TagSummary[] TagSummaries { get; set; }
        public SessionWithVotes[] Sessions { get; set; }
        public AnalysedVote[] Votes { get; set; }
    }
}
