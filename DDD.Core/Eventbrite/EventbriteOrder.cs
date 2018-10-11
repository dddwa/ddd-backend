using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.Eventbrite
{
    public class EventbriteOrder : TableEntity
    {
        public EventbriteOrder()
        {
        }

        public EventbriteOrder(string conferenceInstance, string orderNumber)
        {
            PartitionKey = conferenceInstance;
            RowKey = orderNumber;
        }

        public string OrderId => RowKey;
    }
}
