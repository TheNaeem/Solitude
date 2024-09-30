using RestSharp;

namespace Solitude.Objects.Endpoints;

public class DefaultEndpoint : EndpointBase
{

    public DefaultEndpoint(string url, Method requestMethod = Method.Get, RestClientOptions? options = null, Parameter? body = null)
    {
        Client = new(options ?? new());
        Request = new(url, requestMethod);

        if (body is not null)
        {
            Request.AddParameter(body);
        }
    }
}
