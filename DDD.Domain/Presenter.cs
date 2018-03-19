using System;

namespace DDD.Domain
{
    public class Presenter
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
}