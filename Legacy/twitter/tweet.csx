public class tweet
{
    public string created_at { get; set; }
    public string text { get; set; }

    public quoted_status quoted_status { get; set; }

    public entities entities { get; set; }
}
