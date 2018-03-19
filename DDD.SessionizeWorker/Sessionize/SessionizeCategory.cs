namespace DDD.SessionizeWorker.Sessionize
{
    public class SessionizeCategory
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public SessionizeCategoryItem[] Items { get; set; }
    }

    public class SessionizeCategoryItem
    {
        public const string SessionFormatTitle = "Session format";
        public const string SessionFormatLightningTalkTitle = "20 mins (15 mins talking)";
        public const string SessionFormatFullTalkTitle = "45 mins (40 mins talking)";
        public const string SessionFormatWorkshopTitle = "Workshop";
        public const string LevelTitle = "Level";
        public const string TagsTitle = "Tags";

        public int Id { get; set; }
        public string Name { get; set; }
    }
}