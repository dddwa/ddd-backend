
using Microsoft.WindowsAzure.Storage.Table;

namespace DDD.Core.Tito
{
    public class TitoTicket : TableEntity
    {
        public TitoTicket()
        {
        }

        public TitoTicket(string conferenceInstance, string ticketId)
        {
            PartitionKey = conferenceInstance;
            RowKey = ticketId;
        }

        public string TicketId => RowKey;
    }
}
