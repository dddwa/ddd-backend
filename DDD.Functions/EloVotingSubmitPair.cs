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
            [BindEloVotingConfig]
            EloVotingConfig eloVoting
        )
        {
            if (!eloVoting.EloEnabled)
            {
                log.LogWarning("Attempt to access EloVotingSubmitPair endpoint while EloEnabled feature flag is disabled.");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }
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

            var (winnerVoteId, winnerSessionId, winnerInUnixTimeSeconds) = Encryptor.DecryptSubmissionId(vote.WinnerSessionId, eloVoting.EloPasswordPhrase);
            var (loserVoteId, loserSessionId, loserInUnixTimeSeconds) = Encryptor.DecryptSubmissionId(vote.LoserSessionId, eloVoting.EloPasswordPhrase);

            // we encode the vote into the encrypted payload, so make sure that the pair match, otherwise we're
            // dealing with a weird scenario where someone has two values from different requests
            if (!winnerVoteId.Equals(loserVoteId))
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair with mismatched vote ids.");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            if (loserInUnixTimeSeconds != winnerInUnixTimeSeconds)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair with mismatched expiry timestamps.");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            var voteTimeoffset = DateTimeOffset.FromUnixTimeSeconds(winnerInUnixTimeSeconds);
            var secondsSinceVoteTime = (keyDates.Now - voteTimeoffset).TotalSeconds;
            // make sure the submission is not more 5 minutes form the retrieveing these pair
            if (eloVoting.EloAllowedTimeInSecondsToSubmit < secondsSinceVoteTime)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint after {secondsSinceVoteTime} seconds and the maximum allowed is {eloVoting.EloAllowedTimeInSecondsToSubmit} (Elo pair was retrived at {voteTimeoffset} ).", secondsSinceVoteTime, eloVoting.EloAllowedTimeInSecondsToSubmit, voteTimeoffset);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            // Valid session ids
            if ((!allSubmissionIds.Contains(winnerSessionId) || !allSubmissionIds.Contains(loserSessionId)) || winnerSessionId == loserSessionId)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint with at least one invalid or duplicate submission id (got {winnerId} and {loserId}).", winnerSessionId, loserSessionId);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            // pick one or the other from (winnerVoteId and loserVoteId), we've already made sure that they match
            // No existing vote
            var repo = await eloVoting.GetRepositoryAsync();
            var existing = await repo.GetAsync(conference.ConferenceInstance, winnerVoteId);
            if (existing != null)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint with a duplicate vote (got {winnerVoteId}).", winnerVoteId);
                return new StatusCodeResult((int)HttpStatusCode.Conflict);
            }

            // Save vote
            log.LogInformation("Successfully received elo vote with Id {winnerVoteId}; persisting...", winnerVoteId);
            var eloVoteToPersist = new EloVote(conference.ConferenceInstance, Guid.Parse(winnerVoteId), winnerSessionId, loserSessionId, vote.IsDraw, ip, vote.VoterSessionId, keyDates.Now);
            await repo.CreateAsync(eloVoteToPersist);

            return new StatusCodeResult((int)HttpStatusCode.NoContent);
        }
    }
    public class EloVoteRequest
    {
        public string WinnerSessionId { get; set; }
        public string LoserSessionId { get; set; }
        public bool IsDraw { get; set; }
        public string VoterSessionId { get; set; }
    }
}
