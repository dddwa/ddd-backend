namespace DDD.Core.AzureStorage
{
    public class TicketNotificationEvent
    {
        public string AttendeeName { get; set; }
        public string EventName { get; set; }
        public string TicketClass { get; set; }
        public string TicketNumber { get; set; }
        public string AdminUrl { get; set; }
    }
}
