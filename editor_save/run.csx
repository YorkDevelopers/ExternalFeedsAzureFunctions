#load "../Shared/github.csx"

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Configuration;

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

    // Get request body
    var data = await req.Content.ReadAsStringAsync();

    // Write the data to git hub as this user
    var gitHubClient = new GitHub(log, accessToken);
    await gitHubClient.WriteFileToGitHubAsync(FILENAME, data);

    return req.CreateResponse(HttpStatusCode.OK);
}

