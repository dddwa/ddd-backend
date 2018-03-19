using Newtonsoft.Json;

namespace DDD.SessionizeWorker.Sessionize
{
    public class SessionizeSpeaker
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Bio { get; set; }
        public string TagLine { get; set; }
        [JsonProperty("profilePicture")]
        public string ProfilePictureUrl { get; set; }

        public SessionizeSpeakerLink[] Links { get; set; }
    }

    public class SessionizeSpeakerLink
    {
        public const string LinkedInType = "LinkedIn";
        public const string TwitterType = "Twitter";
        public const string BlogType = "Blog";

        public string LinkType { get; set; }
        public string Url { get; set; }
    }
}