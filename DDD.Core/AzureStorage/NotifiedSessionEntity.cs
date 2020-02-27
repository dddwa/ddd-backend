using System;
using Microsoft.Azure.Cosmos.Table;

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
