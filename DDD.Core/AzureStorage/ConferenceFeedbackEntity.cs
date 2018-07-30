using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.AzureStorage
{
    public class ConferenceFeedbackEntity : TableEntity
    {
        public string Name { get; set; }
        public string Rating { get; set; }
        public string Liked { get; set; }
        public string ImprovementIdeas { get; set; }
    }
}