using System;
using Microsoft.Azure.Cosmos.Table;
namespace DDD.Core.EloVoting
{
    public class EloVote : TableEntity
    {
        public EloVote() { }

        public EloVote(string conferenceInstance, Guid voteId, string winnerSessionId, string loserSessionId, bool isDraw, string ipAddress, string voterSessionId, DateTimeOffset votingSubmittedTime)
        {
            PartitionKey = conferenceInstance;
            RowKey = voteId.ToString();
            WinnerSessionId = winnerSessionId;
            LoserSessionId = loserSessionId;
            IsDraw = isDraw;
            IpAddress = ipAddress;
            VoterSessionId = voterSessionId;
            VotingSubmittedTime = votingSubmittedTime;
        }

        public string WinnerSessionId { get; set; }
        public string LoserSessionId { get; set; }
        public bool IsDraw { get; private set; }
        public string IpAddress { get; set; }
        public string VoterSessionId { get; set; }
        public DateTimeOffset VotingSubmittedTime { get; set; }
        public string VoteId => RowKey;
    }
}
