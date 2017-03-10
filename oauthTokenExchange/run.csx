using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // parse query parameter
    string code = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "code", true) == 0)
        .Value;

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    code = code ?? data?.code;

    return code == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a code on the query string or in the request body")
        : req.CreateResponse(HttpStatusCode.OK, "Hello " + code);
}