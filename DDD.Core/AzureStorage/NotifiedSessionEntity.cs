using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.AzureStorage
{
    public class NotifiedSessionEntity : TableEntity
    {
        public NotifiedSessionEntity() {}

        public NotifiedSessionEntity(Guid id)
        {
            PartitionKey = id.ToString();
            RowKey = Guid.NewGuid().ToString();
        }

        public Guid Id => Guid.Parse(PartitionKey);
    }
}
