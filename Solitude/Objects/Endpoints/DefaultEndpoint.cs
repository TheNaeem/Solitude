using RestSharp;

namespace Solitude.Objects.Endpoints;

public class DefaultEndpoint : EndpointBase
{

    public DefaultEndpoint(string url, Method requestMethod = Method.Get, Parameter? body = null)
    {
        Client = new();
        Request = new(url, requestMethod);

        if (body is not null)
        {
            Request.AddParameter(body);
        }
    }
}
