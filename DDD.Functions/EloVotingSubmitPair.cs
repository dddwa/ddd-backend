using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.Linq;
using DDD.Functions.Extensions;
using DDD.Core.EloVoting;

namespace DDD.Functions
{
    public static class EloVotingSubmitPair
    {
        [FunctionName("EloVotingSubmitPair")]
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
            EloVotingConfig eloVoting
        )
        {
            var vote = await req.Content.ReadAsAsync<EloVoteRequest>();
            var ip = req.GetIpAddress();

            // Within voting window, allowing for 5 minutes of clock drift
            if (keyDates.Before(x => x.VotingAvailableFromDate) || keyDates.After(x => x.VotingAvailableToDate, TimeSpan.FromMinutes(5)))
            {
                log.LogWarning("Attempt to access EloVotingSubmitPair endpoint outside of allowed window of {start} -> {end}.", keyDates.VotingAvailableFromDate, keyDates.VotingAvailableToDate);
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            var (submissionsRepo, _) = await submissions.GetRepositoryAsync();
            var allSubmissions = await submissionsRepo.GetAllAsync(conference.ConferenceInstance);
            var allSubmissionIds = allSubmissions.Where(s => s.Session != null).Select(s => s.Id.ToString()).ToArray();

            var winner = vote.WinnerSessionId;
            var loser = vote.LoserSessionId;
            // Valid session ids
            if ((!allSubmissionIds.Contains(winner) || !allSubmissionIds.Contains(loser)) || winner == loser)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint with at least one invalid or duplicate submission id (got {winnerId} and {loserId}).", winner, loser);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            // No existing vote
            var repo = await eloVoting.GetRepositoryAsync();
            var existing = await repo.GetAsync(conference.ConferenceInstance, vote.Id.ToString());
            if (existing != null)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint with a duplicate vote (got {voteId}).", vote.Id);
                return new StatusCodeResult((int)HttpStatusCode.Conflict);
            }

            // Save vote
            log.LogInformation("Successfully received elo vote with Id {voteId}; persisting...", vote.Id);
            var eloVoteToPersist = new EloVote(conference.ConferenceInstance, vote.Id, winner, loser, vote.isDraw, ip, vote.VoterSessionId, keyDates.Now);
            await repo.CreateAsync(eloVoteToPersist);

            return new StatusCodeResult((int)HttpStatusCode.NoContent);
        }

        public class EloVoteRequest
        {
            public Guid Id { get; set; }
            public string WinnerSessionId { get; set; }
            public string LoserSessionId { get; set; }
            public bool isDraw { get; set; }
            public string VoterSessionId { get; set; }
        }
    }
}
