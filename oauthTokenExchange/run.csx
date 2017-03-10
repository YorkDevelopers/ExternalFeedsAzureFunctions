#load "response.csx"

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Configuration;
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // parse query parameter
    var code = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "oAuthCode", true) == 0)
        .Value;

    var state = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "oAuthState", true) == 0)
        .Value;

    if (code == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass the oAuthcode on the query string or in the request body");

    if (state == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, "Please pass the oAuthcode on the query string or in the request body");

    // Get request body
    //dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    //oAuthcode = oAuthcode ?? data?.code;

    var client = new HttpClient();
    client.BaseAddress = new Uri("https://github.com");
    client.DefaultRequestHeaders
          .Accept
          .Add(new MediaTypeWithQualityHeaderValue("application/json"));

    var client_id = ConfigurationManager.AppSettings["GITHUB_APP_CLIENT_ID"];
    var client_token = ConfigurationManager.AppSettings["GITHUB_APP_CLIENT_TOKEN"];
    var redirect_uri = "http://localhost/oauthcomplete.html";
    var url = "/login/oauth/access_token?client_id=" + client_id + "&client_secret=" + client_token + "&code=" + code + "&redirect_uri=" + redirect_uri + "&state=" + state;

    var response = await POST<Response>(client, url, null, log);
    if (!string.IsNullOrWhiteSpace(response.error))
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, $"{response.error} {response.error_description} {response.error_uri}");
    }
    else
    {
        return req.CreateResponse(HttpStatusCode.OK, response.access_token);
    }
}

public static async Task<T> POST<T>(HttpClient client, string apiCall, HttpContent value, TraceWriter log)
{
    // Proxy the call onto our service.
    var httpResponseMessage = await client.PostAsync(apiCall, value);
    if (!httpResponseMessage.IsSuccessStatusCode)
    {
        throw new Exception(string.Format("Failed to POST to {0}.   Status {1}.  Reason {2}.", apiCall, (int)httpResponseMessage.StatusCode, httpResponseMessage.ReasonPhrase));
    }
    else
    {
        return await httpResponseMessage.Content.ReadAsAsync<T>();
    }
}