using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // parse query parameter
    string oAuthcode = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "oAuthcode", true) == 0)
        .Value;

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    oAuthcode = oAuthcode ?? data?.code;

    return oAuthcode == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass the oAuthcode on the query string or in the request body")
        : req.CreateResponse(HttpStatusCode.OK, "Hello " + oAuthcode);
}