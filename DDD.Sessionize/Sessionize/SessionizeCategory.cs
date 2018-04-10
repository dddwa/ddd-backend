namespace DDD.Sessionize.Sessionize
{
    public class SessionizeCategory
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public SessionizeCategoryItem[] Items { get; set; }
    }

    public class SessionizeCategoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}