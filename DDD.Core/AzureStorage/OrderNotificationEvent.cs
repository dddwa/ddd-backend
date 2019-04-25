namespace DDD.Core.AzureStorage
{
    public class OrderNotificationEvent
    {
        public string OrdererName { get; set; }
        public string EventName { get; set; }
        public string OrderNumber { get; set; }
        public string AdminUrl { get; set; }
        public decimal Total { get; set; }
        public string TicketsPurchasedDescription { get; set; }
    }
}
