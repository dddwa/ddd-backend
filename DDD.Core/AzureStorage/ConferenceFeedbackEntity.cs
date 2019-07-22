using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.AzureStorage
{
    public class ConferenceFeedbackEntity : TableEntity
    {
        public ConferenceFeedbackEntity() { }
        public ConferenceFeedbackEntity(string conferenceInstance, string name, string rating, string liked, string improvementIdeas, string deviceId)
        {
            PartitionKey = conferenceInstance;
            RowKey = Guid.NewGuid().ToString();
            Name = name;
            Rating = rating;
            Liked = liked;
            ImprovementIdeas = improvementIdeas;
            DeviceId = deviceId;
        }
        public string Name { get; set; }
        public string Rating { get; set; }
        public string Liked { get; set; }
        public string ImprovementIdeas { get; set; }
        public string DeviceId { get; set; }
    }
}