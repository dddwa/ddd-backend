using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.AzureStorage
{
    public class SessionFeedbackEntity : TableEntity
    {
        public SessionFeedbackEntity() {}

        public SessionFeedbackEntity(string sessionId, string conferenceInstance, string name, string rating, string liked, string improvementIdeas, string sessionName) {
            PartitionKey = conferenceInstance;
            RowKey = Guid.NewGuid().ToString();
            SessionId = sessionId;
            Name = name;
            Rating = rating;
            Liked = liked;
            ImprovementIdeas = improvementIdeas;
            SessionName = sessionName;
            Time = null;
        }

        public string Name { get; set; }
        public string Rating { get; set; }
        public string Liked { get; set; }
        public string ImprovementIdeas { get; set; }
        public string SessionId { get; set; }
        public string SessionName { get; set; }
        public string Time { get; set; }
    }
}