#load "venue.csx"
#load "common.csx"
#load "gitHub.csx"
#load "responses.csx"

using System;
using System.Configuration;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using YamlDotNet.Serialization;


public static void Run(TimerInfo myTimer, TraceWriter log)
{
    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");    

    const string CATEGORY = "102"; //Science and Tech
    const string URL = "https://www.eventbriteapi.com";
    const string FILENAME = "_data/EventBrite.yml";

    // We keep a cache of venues so that we only have to fetch each one once.
    var venues = new Dictionary<string, Venue>();

    // Use the EventBrite API to find all the Science and Tech events in York
    var token = ConfigurationManager.AppSettings["EVENTBRITETOKEN"];
    var client = PrepareHttpClient(new Uri(URL), token);
    var events = GET<Responses>(client, $"/v3/events/search/?location.address=York&categories={CATEGORY}");

    // Take all the events and convert them into our 'Commmon' event format
    var allEvents = new List<Common>();
    foreach (var evt in events.events)
    {
        // Either take the venue from our cache or go and fetch it
        var venue = default(Venue);
        if (!venues.TryGetValue(evt.venue_id, out venue))
        {
            venue = GET<Venue>(client, $"/v3/venues/{evt.venue_id}/");
            venues.Add(evt.venue_id, venue);
        }

        // Populate our common structure
        var common = new Common();
        common.Name = evt.name.text;
        common.Description = evt.description.text;
        common.URL = evt.url;
        common.IsFree = evt.is_free;
        common.Logo = CleanUpURL(evt.logo?.url);
        common.Starts = evt.start.local;
        common.Ends = evt.end.local;
        common.Venue = venue.name;
        allEvents.Add(common);
    }

    // Convert all the events into yaml
    var serializer = new Serializer();
    var yaml = serializer.Serialize(allEvents.Where(IncludeEvent));

    // Push the file to git
    var gitHubClient = new GitHub(log);
    gitHubClient.WriteFileToGitHub(FILENAME, yaml);

}

        
/// <summary>
/// Are we happy to include this event?
/// </summary>
/// <param name="common"></param>
/// <returns></returns>
private static bool IncludeEvent(Common common)
{
    if (common.Description.Contains("These courses are for staff and students of York St. John University only"))
        return false;
    else
        return true;
}

/// <summary>
/// The image URL we are provided is a bit strange as it starts with a page and
/// then takes the actual image page as a querystring parameter
/// We remove the initial page,  and then reverse the URL encoding on the rest
/// of the URL.
/// </summary>
/// <param name="originalURL"></param>
/// <returns></returns>
private static string CleanUpURL(string originalURL)
{
    const string PREFIX = "https://img.evbuc.com/";

    if (!string.IsNullOrWhiteSpace(originalURL) && originalURL.StartsWith(PREFIX))
    {
        originalURL = originalURL.Substring(PREFIX.Length);
        originalURL = WebUtility.UrlDecode(originalURL);
    }

    return originalURL;
}

/// <summary>
/// When calling the EventBrite API we have to pass our App's token
/// in the header.
/// </summary>
/// <param name="endPoint"></param>
/// <param name="oAuthtoken"></param>
/// <returns></returns>
private static HttpClient PrepareHttpClient(Uri endPoint, string oAuthtoken)
{
    var client = new HttpClient();
    client.BaseAddress = endPoint;
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + oAuthtoken);
    return client;
}

/// <summary>
/// Wrapper around an HTTP GET call
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="client"></param>
/// <param name="apiCall"></param>
/// <returns></returns>
public static T GET<T>(HttpClient client, string apiCall)
{
    // Proxy the call onto our service.
    var httpResponseMessage = client.GetAsync(apiCall).Result;
    if (!httpResponseMessage.IsSuccessStatusCode)
    {
        throw new Exception(string.Format("Failed to GET from {0}.   Status {1}.  Reason {2}. {3}", apiCall, (int)httpResponseMessage.StatusCode, httpResponseMessage.ReasonPhrase, httpResponseMessage.RequestMessage));
    }
    else
    {
        return httpResponseMessage.Content.ReadAsAsync<T>().Result;
            }
        }
       