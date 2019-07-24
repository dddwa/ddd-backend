using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using Microsoft.AspNetCore.Http;
using System.Linq;
using DDD.Core.Voting;
using DDD.Functions.Extensions;
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
            [BindConferenceConfig]
            ConferenceConfig conference,
            [BindKeyDatesConfig]
            KeyDatesConfig keyDates,
            [BindSubmissionsConfig]
            SubmissionsConfig submissions,
            [BindVotingConfig]
            VotingConfig voting,
            [BindTitoSyncConfig]
            TitoSyncConfig tickets
            )
        {
            var vote = await req.Content.ReadAsAsync<VoteRequest>();
            var ip = req.GetIpAddress();

            // Within voting window, allowing for 5 minutes of clock drift
            if (keyDates.Before(x => x.VotingAvailableFromDate) || keyDates.After(x => x.VotingAvailableToDate, TimeSpan.FromMinutes(5)))
            {
                log.LogWarning("Attempt to access SubmitVote endpoint outside of allowed window of {start} -> {end}.", keyDates.VotingAvailableFromDate, keyDates.VotingAvailableToDate);
                return new StatusCodeResult((int) HttpStatusCode.NotFound);
            }

            // Correct number of votes
            var numVotesSubmitted = vote.SessionIds?.Length ?? 0;
            if (numVotesSubmitted < conference.MinVotes || numVotesSubmitted > conference.MaxVotes)
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint with incorrect number of votes ({numVotes} rather than {minVotes} - {maxVotes}).", numVotesSubmitted, conference.MinVotes, conference.MaxVotes);
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            // Correct number of indices
            if (numVotesSubmitted != vote.Indices?.Length)
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint without matching indices ({numIndices} vs {numVotes}).", vote.Indices?.Length, numVotesSubmitted);
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            // Valid voting start time, allowing for 5 minutes of clock drift
            if (vote.VotingStartTime > keyDates.Now.AddMinutes(5) || vote.VotingStartTime < keyDates.VotingAvailableFromDate.AddMinutes(-5))
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint with invalid start time (got {submittedStartTime} instead of {votingStartTime} - {now}).", vote.VotingStartTime, keyDates.VotingAvailableFromDate, keyDates.Now);
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }

            if (voting.TicketNumberWhileVotingValue == TicketNumberWhileVoting.Required)
            {
                // Get tickets
                var ticketsRepo = await tickets.GetRepositoryAsync();
                var matchedTicket = await ticketsRepo.GetAsync(conference.ConferenceInstance, vote.TicketNumber.ToUpperInvariant());
                // Only if you have a valid ticket
                if (string.IsNullOrEmpty(vote.TicketNumber) || matchedTicket == null)
                {
                    log.LogWarning("Attempt to submit to SubmitVote endpoint without a valid ticket. Ticket id sent was {ticketNumber}", vote.TicketNumber);
                    return new StatusCodeResult((int) HttpStatusCode.BadRequest);
                }
            }

            // Get submitted sessions
            var (submissionsRepo, _) = await submissions.GetRepositoryAsync();
            var allSubmissions = await submissionsRepo.GetAllAsync(conference.ConferenceInstance);
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
            var repo = await voting.GetRepositoryAsync();
            var existing = await repo.GetAsync(conference.ConferenceInstance, vote.Id.ToString());
            if (existing != null)
            {
                log.LogWarning("Attempt to submit to SubmitVotes endpoint with a duplicate vote (got {voteId}).", vote.Id);
                return new StatusCodeResult((int) HttpStatusCode.Conflict);
            }

            // Save vote
            log.LogInformation("Successfully received vote with Id {voteId}; persisting...", vote.Id);
            var voteToPersist = new Vote(conference.ConferenceInstance, vote.Id, vote.SessionIds, vote.Indices, vote.TicketNumber?.ToUpperInvariant(), ip, vote.VoterSessionId, vote.VotingStartTime, keyDates.Now);
            await repo.CreateAsync(voteToPersist);

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
