namespace DDD.Sessionize.Sessionize
{
    public class SessionizeResponse
    {
        public SessionizeSession[] Sessions { get; set; }
        public SessionizeCategory[] Categories { get; set; }
        public SessionizeSpeaker[] Speakers { get; set; }
        public SessionizeQuestion[] Questions { get; set; }
    }
}