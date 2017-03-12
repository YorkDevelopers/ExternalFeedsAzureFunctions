#load "../Shared/github.csx"
#load "../Shared/common.csx"
#r "Newtonsoft.Json"

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Configuration;
using YamlDotNet.Serialization;
using System.Collections.Generic;
using Newtonsoft.Json;

// Function called by the manual event editor in order to load the yml file from git hub
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    const string FILENAME = "_data/Manual.yml";

    // parse query parameter
    var accessToken = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "accessToken", true) == 0)
        .Value;

    if (accessToken == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass the accessToken is the query string");

    // Read the data out of git hub
    var gitHubClient = new GitHub(log, accessToken);
    var yaml = await gitHubClient.ReadFileFromGitHubAsync(FILENAME);

    // The yml contains an array of events in our 'common' format.  Convert this into  a list of objects.
    var deserializer = new Deserializer();
    var allEvents = deserializer.Deserialize<List<Common>>(yaml);

    // And then convert this into JSON as that is what we deal with in the front end.
    var json = JsonConvert.SerializeObject(allEvents);

    return req.CreateResponse(HttpStatusCode.OK, json);
}

