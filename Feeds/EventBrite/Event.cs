namespace Feeds.EventBrite
{
    public class Event
    {
        public string resource_uri { get; set; }
        public Text description { get; set; }
        public Text name { get; set; }
        public When start { get; set; }
        public When end { get; set; }
        public string venue_id { get; set; }

        public string url { get; set; }

        public bool is_free { get; set; }
        public Logo logo { get; set; }
    }
}
