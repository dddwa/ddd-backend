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

            var (winnerVoteId, winner, winnerUnixTimeSeconds) = Encryptor.DecryptSubmissionId(vote.WinnerSessionId, eloVoting.EloPasswordPhrase);
            var (loserVoteId, loser, loserUnixTimeSeconds) = Encryptor.DecryptSubmissionId(vote.LoserSessionId, eloVoting.EloPasswordPhrase);

            // we encode the vote into the encrypted payload, so make sure that the pair match, otherwise we're
            // dealing with a weird scenario where someone has two values from different requests
            if (!winnerVoteId.Equals(loserVoteId))
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair with mismatched vote ids.");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            if (loserUnixTimeSeconds != winnerUnixTimeSeconds)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair with mismatched expiry timestamps.");
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            // pick one or the other, we've already made sure that they match
            var voteId = winnerVoteId;
            var allowedTimeToAcceptTheVote = keyDates.Now.AddSeconds(-eloVoting.EloAllowedTimeInSecondsToSubmit).ToUnixTimeSeconds();

            // make sure the submission is not more 5 minutes form the retrieveing these pair
            if (winnerUnixTimeSeconds < allowedTimeToAcceptTheVote || loserUnixTimeSeconds < allowedTimeToAcceptTheVote)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint after {allowedTimeToAcceptTheVote} seconds of GetPair (got {winnerTime} and {loserTime}).", allowedTimeToAcceptTheVote, winnerUnixTimeSeconds, loserUnixTimeSeconds);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            // Valid session ids
            if ((!allSubmissionIds.Contains(winner) || !allSubmissionIds.Contains(loser)) || winner == loser)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint with at least one invalid or duplicate submission id (got {winnerId} and {loserId}).", winner, loser);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            // No existing vote
            var repo = await eloVoting.GetRepositoryAsync();
            var existing = await repo.GetAsync(conference.ConferenceInstance, voteId);
            if (existing != null)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint with a duplicate vote (got {voteId}).", voteId);
                return new StatusCodeResult((int)HttpStatusCode.Conflict);
            }

            // Save vote
            log.LogInformation("Successfully received elo vote with Id {voteId}; persisting...", voteId);
            var eloVoteToPersist = new EloVote(conference.ConferenceInstance, Guid.Parse(voteId), winner, loser, vote.IsDraw, ip, vote.VoterSessionId, keyDates.Now);
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
