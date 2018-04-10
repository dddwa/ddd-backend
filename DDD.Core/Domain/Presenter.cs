using System;

namespace DDD.Core.Domain
{
    public class Presenter
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Tagline { get; set; }
        public string Bio { get; set; }
        public string ProfilePhotoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public string TwitterHandle { get; set; }

        public bool DataEquals(Presenter p)
        {
            return Name == p.Name
                && Tagline == p.Tagline
                && Bio == p.Bio
                && ProfilePhotoUrl == p.ProfilePhotoUrl
                && WebsiteUrl == p.WebsiteUrl
                && TwitterHandle == p.TwitterHandle;
        }
    }
}