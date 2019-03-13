using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.Tito
{
    public class TitoOrder : TableEntity
    {
        public TitoOrder()
        {
        }

        public TitoOrder(string conferenceInstance, string orderNumber)
        {
            PartitionKey = conferenceInstance;
            RowKey = orderNumber;
        }

        public string OrderId => RowKey;
    }
}
