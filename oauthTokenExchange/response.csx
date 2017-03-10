public class Response
{
    public string access_token { get; set; }
    public string scope { get; set; }
    public string bearer { get; set; }

    public string error { get; set; }
    public string error_description { get; set; }
    public string error_uri { get; set; }
}