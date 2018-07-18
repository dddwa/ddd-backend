using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using DDD.Functions.Config;
using System;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.AspNetCore.Http;
using DDD.Core.DocumentDb;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace DDD.Functions
{
    public static class SubmitVote
    {
        [FunctionName("SubmitVote")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequestMessage req,
            ILogger log,
            [BindSubmissionsConfig]
            SubmissionsConfig submissionsConfig,
            [BindSubmissionsAndVotingConfig]
            SubmissionsAndVotingConfig config
            )
        {
            var vote = await req.Content.ReadAsAsync<VoteRequest>();
            var ip = req.GetIpAddress();

            // Within voting window, allowing for 5 minutes of clock drift
            if (config.Now < config.VotingAvailableFromDate || config.Now > config.VotingAvailableToDate.AddMinutes(5))
            {
                log.LogWarning("Attempt to access SubmitVote endpoint outside of allowed window of {start} -> {end}.", config.VotingAvailableFromDate, config.VotingAvailableToDate);
                return new StatusCodeResult((int) HttpStatusCode.NotFound);
            }

            // Correct number of votes
            var numVotesSubmitted = vote.SessionIds?.Length ?? 0;
            if (numVotesSubmitted < config.MinVotes || numVotesSubmitted > config.MaxVotes)
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint with incorrect number of votes ({numVotes} rather than {minVotes} - {maxVotes}).", numVotesSubmitted, config.MinVotes, config.MaxVotes);
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            // Correct number of indices
            if (numVotesSubmitted != vote.Indices?.Length)
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint without matching indices ({numIndices} vs {numVotes}).", vote.Indices?.Length, numVotesSubmitted);
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            // Valid voting start time, allowing for 5 minutes of clock drift
            if (vote.VotingStartTime > config.Now.AddMinutes(5) || vote.VotingStartTime < config.VotingAvailableFromDate.AddMinutes(-5))
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint with invalid start time (got {submittedStartTime} instead of {votingStartTime} - {now}).", vote.VotingStartTime, config.VotingAvailableFromDate, config.Now);
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            // Get submitted sessions
            var (sessionsRepo, _) = await submissionsConfig.GetSubmissionRepositoryAsync();
            await sessionsRepo.InitializeAsync();
            var allSubmissions = await sessionsRepo.GetAllAsync(submissionsConfig.ConferenceInstance);
            var allSubmissionIds = allSubmissions.Where(s => s.Session != null).Select(s => s.Id.ToString()).ToArray();

            // Valid session ids
            if (vote.SessionIds.Any(id => !allSubmissionIds.Contains(id)) || vote.SessionIds.Distinct().Count() != vote.SessionIds.Count())
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint with at least one invalid or duplicate submission id (got {sessionIds}).", JsonConvert.SerializeObject(vote.SessionIds));
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            // Valid indices
            if (vote.Indices.Any(index => index <= 0 || index > allSubmissionIds.Count()) || vote.Indices.Distinct().Count() != vote.Indices.Count())
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint with at least one invalid or duplicate index (got {indices} when the number of sessions is {totalNumberOfSessions}).", JsonConvert.SerializeObject(vote.Indices), allSubmissionIds.Count());
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            // No existing vote
            var account = CloudStorageAccount.Parse(config.VotingConnectionString);
            var table = account.CreateCloudTableClient().GetTableReference(config.VotingTable);
            await table.CreateIfNotExistsAsync();
            var existing = await table.ExecuteAsync(TableOperation.Retrieve<Vote>(submissionsConfig.ConferenceInstance, vote.Id.ToString()));
            if (existing.HttpStatusCode != (int) HttpStatusCode.NotFound)
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint with a duplicate vote (got {voteId}).", vote.Id);
                return new StatusCodeResult((int) HttpStatusCode.Conflict);
            }

            // Save vote
            log.LogInformation("Successfully received vote with Id {voteId}; persisting...", vote.Id);
            var voteToPersist = new Vote(submissionsConfig.ConferenceInstance, vote.Id, vote.SessionIds, vote.Indices, vote.TicketNumber, ip, vote.VoterSessionId, vote.VotingStartTime, config.Now);
            await table.ExecuteAsync(TableOperation.Insert(voteToPersist));

            return new StatusCodeResult((int) HttpStatusCode.NoContent);
        }
    }

    public class VoteRequest
    {
        public Guid Id { get; set; }
        public string[] SessionIds { get; set; }
        public string TicketNumber { get; set; }
        public int[] Indices { get; set; }
        public string VoterSessionId { get; set; }
        public DateTimeOffset VotingStartTime { get; set; }
    }

    public class Vote : TableEntity
    {
        public Vote() {}

        public Vote(string conferenceInstance, Guid voteId, string[] sessionIds, int[] indices, string ticketNumber, string ipAddress, string voterSessionId, DateTimeOffset votingStartTime, DateTimeOffset votingSubmittedTime)
        {
            PartitionKey = conferenceInstance;
            RowKey = voteId.ToString();
            SessionIds = JsonConvert.SerializeObject(sessionIds);
            Indices = JsonConvert.SerializeObject(indices);
            TicketNumber = ticketNumber;
            IpAddress = ipAddress;
            VoterSessionId = voterSessionId;
            VotingStartTime = votingStartTime;
            VotingSubmittedTime = votingSubmittedTime;
        }

        public string SessionIds { get; set; }

        public string[] GetSessionIds()
        {
            return JsonConvert.DeserializeObject<string[]>(SessionIds);
        }

        public string Indices { get; set; }

        public string[] GetIndices()
        {
            return JsonConvert.DeserializeObject<string[]>(Indices);
        }

        public string TicketNumber { get; set; }
        public string IpAddress { get; set; }
        public string VoterSessionId { get; set; }
        public DateTimeOffset VotingStartTime { get; set; }
        public DateTimeOffset VotingSubmittedTime { get; set; }
        public string VoteId => RowKey;
    }

    public static class RequestExtensions
    {
        public static string GetIpAddress(this HttpRequestMessage req)
        {
            if (req.Properties.ContainsKey("HttpContext"))
                return ((DefaultHttpContext)req.Properties["HttpContext"])?.Connection?.RemoteIpAddress?.ToString();

            return null;
        }
    }
}
