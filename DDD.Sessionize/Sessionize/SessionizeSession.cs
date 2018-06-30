using Newtonsoft.Json;

namespace DDD.Sessionize.Sessionize
{
    public class SessionizeSession
    {
        public SessionizeSession()
        {
            SpeakerIds = new string[0];
            CategoryItemIds = new int[0];
            QuestionAnswers = new SessionizeQuestionAnswer[0];
        }

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