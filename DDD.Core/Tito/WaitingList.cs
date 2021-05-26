
using Microsoft.Azure.Cosmos.Table;

namespace DDD.Core.Tito
{
    public class WaitingList : TableEntity
    {
        public WaitingList()
        {
        }

        public WaitingList(string conferenceInstance, string email)
        {
            PartitionKey = conferenceInstance;
            RowKey = email;
        }

        public string Email => RowKey;
    }
}
