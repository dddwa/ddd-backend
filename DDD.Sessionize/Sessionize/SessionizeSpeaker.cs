using Newtonsoft.Json;

namespace DDD.Sessionize.Sessionize
{
    public class SessionizeSpeaker
    {
        public SessionizeSpeaker()
        {
            Links = new SessionizeSpeakerLink[0];
        }

        public string Id { get; set; }
        public string FullName { get; set; }
        public string Bio { get; set; }
        public string TagLine { get; set; }
        [JsonProperty("profilePicture")]
        public string ProfilePictureUrl { get; set; }

        public SessionizeSpeakerLink[] Links { get; set; }
        [JsonProperty("categoryItems")]
        public int[] CategoryItemIds { get; set; }

        public SessionizeQuestionAnswer[] QuestionAnswers { get; set; }
    }

    public class SessionizeSpeakerLink
    {
        public string LinkType { get; set; }
        public string Url { get; set; }
    }
}