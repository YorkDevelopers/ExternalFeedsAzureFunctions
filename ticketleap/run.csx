#load "responses.csx"
#load "../Shared/common.csx"
#load "../Shared/gitHub.csx"

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

    const string URL = "https://public-api.ticketleap.com";
    const string FILENAME = "_data/TicketLeap.yml";
    const string LECTURE = "c15215f2-da03-4d08-8668-6ec90cdefe0e";

    // Use the TicketLeap API to find all the Lecture events in York
    var token = ConfigurationManager.AppSettings["TICKETLEAPTOKEN"];
    var client = PrepareHttpClient(new Uri(URL));
    var response = GET<Response>(client, $"/events/by/location/GBR/%20/YORK?key={token}&page_num=1&page_size=100&event_filters={LECTURE}");

    // Take all the events and convert them into our 'Commmon' event format
    var allEvents = new List<Common>();
    foreach (var evt in response.events)
    {
        // Populate our common structure
        var common = new Common();
        common.Name = evt.name;
        common.Description = evt.description;
        common.URL = evt.url;
        common.Logo = evt.hero_small_image_url;
        common.Starts = evt.earliest_end_local;
        common.Ends = evt.earliest_end_local;
        common.Venue = evt.venue_name;
        allEvents.Add(common);
    }

    // Convert all the events into yaml
    var serializer = new Serializer();
    var yaml = serializer.Serialize(allEvents);

    // Push the file to git
    var gitHubClient = new GitHub(log);
    gitHubClient.WriteFileToGitHub(FILENAME, yaml);

}

        
/// <summary>
/// Configure the ticketleap endpoint
/// </summary>
/// <param name="endPoint"></param>
/// <param name="oAuthtoken"></param>
/// <returns></returns>
private static HttpClient PrepareHttpClient(Uri endPoint)
{
    var client = new HttpClient();
    client.BaseAddress = endPoint;
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