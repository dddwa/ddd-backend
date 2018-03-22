using System;

namespace DDD.Domain
{
    public class Presenter
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        //public string Email { get; set; }
        public string Tagline { get; set; }
        public string Bio { get; set; }
        public string ProfilePhotoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string TwitterHandle { get; set; }
    }
}