using Newtonsoft.Json;

namespace DDD.SessionizeWorker.Sessionize
{
    public class SessionizeSession
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        [JsonProperty("speakers")]
        public string[] SpeakerIds { get; set; }
        [JsonProperty("categoryItems")]
        public int[] CategoryItemIds { get; set; }

        public SessionizeQuestionAnswer[] QuestionAnswers { get; set; }
    }

    public class SessionizeQuestionAnswer
    {
        public int QuestionId { get; set; }
        public string AnswerValue { get; set; }
    }

}