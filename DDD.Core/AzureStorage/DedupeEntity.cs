using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.AzureStorage
{
    public class DedupeEntity : TableEntity
    {
        public DedupeEntity()
        {
        }

        public DedupeEntity(string id, string notificationMessage)
        {
            PartitionKey = id;
            RowKey = notificationMessage;
        }

        public string OrderId => RowKey;
    }
}
