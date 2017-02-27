#load "common.csx"
#load "gitHub.csx"
#load "document.csx"
#load "childtag.csx"

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using YamlDotNet.Serialization;

public static void Run(TimerInfo myTimer, TraceWriter log)
{
    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");  

    const string URL = "http://www.ypsyork.org/events/?pno=";
    var client = new WebClient();

    List<Common> allEvents = new List<Common>();
    var pageNumber = 1;
    while (true)
    {
        if (!ReadPage(client, URL, allEvents, pageNumber)) break;
        pageNumber++;
    }

    var serializer = new Serializer();
    var yaml = serializer.Serialize(allEvents);

    // Push the file to git
    var gitHubClient = new GitHub(log);
    gitHubClient.WriteFileToGitHub("_data/YPS.yml", yaml);
}

private static bool ReadPage(WebClient client, string URL, List<Common> allEvents, int pageNumber)
{
    var added = false;
    Console.WriteLine("Looking for page "  + pageNumber);
    //var html = client.DownloadString(URL + pageNumber);
    var html = readpage(URL + pageNumber);

    var document = new Document(html);

    var article = document.GetNextTag(@"<div class=""article"">");
    while (article != null)
    {
        var common = new Common();
        common.IsFree = false;  //free to members

        var articleTitle = document.GetNextTag(@"<h3 class=""article-title"">", article);
        if (articleTitle != null)
        {
            var urlTag = document.GetNextTagOfType("a", articleTitle);
            common.Name = urlTag.Contents;
            common.URL = document.GetAttribute(urlTag, "href");
        }

        var articleDetails = document.GetNextTag(@"dl class=""article-details""", article);

        var dateLabelTag = document.GetNextTagOfType("dt", articleDetails);
        var dateTag = document.GetNextTagOfType("dd", dateLabelTag);

        var startTimeLabelTag = document.GetNextTagOfType("dt", dateTag);
        var startTimeTag = document.GetNextTagOfType("dd", startTimeLabelTag);

        if (startTimeTag.Contents == "All day event")
        {
            common.Starts = DateTime.ParseExact(dateTag.Contents, "d MMM yyyy", null);
            common.Ends = common.Starts.AddHours(23);
        }
        else
        {
            common.Starts = DateTime.ParseExact(dateTag.Contents + " " + startTimeTag.Contents, "d MMM yyyy h:mm tt", null);
            common.Ends = common.Starts.AddHours(2);
        }
        var venueLabelTag = document.GetNextTagOfType("dt", startTimeTag);
        var venueTag = startTimeTag;
        if (venueLabelTag != null)
        {
            venueTag = document.GetNextTagOfType("dd", venueLabelTag);
            common.Venue = venueTag.Contents;
        }

        var logo = document.GetNextTagOfType("img", venueTag);
        common.Logo = document.GetAttribute(logo, "src");

        common.Description = GetDescription(common.URL, client);
        allEvents.Add(common);


        article = document.GetNextTag(@"<div class=""article"">", article);
        added = true;
    }

    return added;
}

private static string GetDescription(string URL, WebClient client)
{
    // var html = client.DownloadString(URL);
    var html = readpage(URL);

    var document = new Document(html);
    var heading = document.GetNextTag(@"<h3 class=""article-title"">");
    var description = document.GetNextTagOfType("p", heading);
    return description.Contents;
}

private static string readpage(string Url)
{
    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(Url);
    myRequest.Method = "GET";
    WebResponse myResponse = myRequest.GetResponse();
    var sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
    string result = sr.ReadToEnd();
    sr.Close();
    myResponse.Close();

    return result;
}
