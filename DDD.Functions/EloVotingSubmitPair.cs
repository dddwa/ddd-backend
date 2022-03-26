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
            var conferenceYear = conference.ConferenceInstance;

            // Within voting window, allowing for 5 minutes of clock drift
            if (keyDates.Before(x => x.VotingAvailableFromDate) || keyDates.After(x => x.VotingAvailableToDate, TimeSpan.FromMinutes(5)))
            {
                log.LogWarning("Attempt to access EloVotingSubmitPair endpoint outside of allowed window of {start} -> {end}.", keyDates.VotingAvailableFromDate, keyDates.VotingAvailableToDate);
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            var (submissionsRepo, _) = await submissions.GetRepositoryAsync();
            var allSubmissions = await submissionsRepo.GetAllAsync(conferenceYear);
            var allSubmissionIds = allSubmissions.Where(s => s.Session != null).Select(s => s.Id.ToString()).ToArray();

            var (winnerVoteId, winnerSessionId, winnerInUnixTimeSeconds) = Encryptor.DecryptSubmissionId(vote.WinnerSessionId, eloVoting.EloPasswordPhrase);
            var (loserVoteId, loserSessionId, loserInUnixTimeSeconds) = Encryptor.DecryptSubmissionId(vote.LoserSessionId, eloVoting.EloPasswordPhrase);

            // we encode the vote into the encrypted payload, so make sure that the pair match, otherwise we're
            // dealing with a weird scenario where someone has two values from different requests
            if (!winnerVoteId.Equals(loserVoteId))
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair with mismatched vote ids. (Got {winnerVoteId} and {loserVoteId}", winnerVoteId, loserVoteId);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            if (loserInUnixTimeSeconds != winnerInUnixTimeSeconds)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair with mismatched expiry timestamps. (Got {winnerInUnixTimeSeconds} and {loserInUnixTimeSeconds}", winnerInUnixTimeSeconds, loserInUnixTimeSeconds);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            // pick one or the other from (winnerInUnixTimeSeconds and loserInUnixTimeSeconds), we've already made sure that they match
            var voteTime = DateTimeOffset.FromUnixTimeSeconds(winnerInUnixTimeSeconds);
            var secondsSinceVote = (keyDates.Now - voteTime).TotalSeconds;
            // make sure the submission is not more 5 minutes form the retrieveing these pair
            if (eloVoting.EloAllowedTimeInSecondsToSubmit < secondsSinceVote)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint after {secondsSinceVote} seconds and the maximum allowed is {eloVoting.EloAllowedTimeInSecondsToSubmit} seconds (EloPair was retrived at {voteTime} ).", secondsSinceVote, eloVoting.EloAllowedTimeInSecondsToSubmit, voteTime);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            // Valid session ids
            if ((!allSubmissionIds.Contains(winnerSessionId) || !allSubmissionIds.Contains(loserSessionId)) || winnerSessionId == loserSessionId)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint with at least one invalid or duplicate submission ids (got {winnerSessionId} and {loserSessionId}).", winnerSessionId, loserSessionId);
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            // pick one or the other from (winnerVoteId and loserVoteId), we've already made sure that they match
            // No existing vote
            var repo = await eloVoting.GetRepositoryAsync();
            var existing = await repo.GetAsync(conferenceYear, winnerVoteId);
            if (existing != null)
            {
                log.LogWarning("Attempt to submit to EloVotingSubmitPair endpoint with a duplicate voteId(got {winnerVoteId}).", winnerVoteId);
                return new StatusCodeResult((int)HttpStatusCode.Conflict);
            }

            // Save vote
            log.LogInformation("Successfully received elo vote with Id {winnerVoteId}; persisting...", winnerVoteId);
            var eloVoteToPersist = new EloVote(conferenceYear, winnerVoteId, winnerSessionId, loserSessionId, vote.IsDraw, ip, vote.VoterSessionId);
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
