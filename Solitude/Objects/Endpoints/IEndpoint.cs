using System.Threading.Tasks;
using RestSharp;

namespace Solitude.Objects.Endpoints;

public interface IEndpoint
{
    RestResponse GetResponse();
    RestResponse<T> GetResponse<T>();
    Task<RestResponse> GetResponseAsync();
    Task<RestResponse<T>> GetResponseAsync<T>();
}
