#load "when.csx"
#load "text.csx"
#load "logo.csx"

public class Event
{
    //date
    //description
    //location
    //url
    //image
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