namespace Feeds.Twitter
{
    public class quoted_status
    {
        public string created_at { get; set; }

        public string text { get; set; }

        public entities entities { get; set; }

        public user user { get; set; }
    }
}
