using Microsoft.Azure.Cosmos.Table;

namespace DDD.Core.AzureStorage
{
    public class DedupeWebhookEntity : TableEntity
    {
        public DedupeWebhookEntity() {}

        public DedupeWebhookEntity(string webhookType, string eventType, string id)
        {
            PartitionKey = webhookType;
            RowKey = $"{eventType}|{id}";
        }
    }
}
