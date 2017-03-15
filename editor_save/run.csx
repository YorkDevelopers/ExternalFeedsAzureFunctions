#load "../Shared/github.csx"
#load "../Shared/common.csx"
#r "Newtonsoft.Json"

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Configuration;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Newtonsoft.Json;

// Function called by the manual event editor in order to save the yml file back to git hub
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

    // Get request body,  this should be a JSON array of common events
    var data = await req.Content.ReadAsStringAsync();
    var allEvents = JsonConvert.DeserializeObject<List<Common>>(data as string);

    // Convert these into YAML
    var serializer = new Serializer();
    var yaml = serializer.Serialize(allEvents);    

    // Write the data to git hub as this user
    var gitHubClient = new GitHub(log, accessToken);
    await gitHubClient.WriteFileToGitHubAsync(FILENAME, yaml);

    return req.CreateResponse(HttpStatusCode.OK);
}

