using System.Net;
using System.Text;

namespace Diguifi.Tests.Unit.Helpers;

internal sealed class FakeStripeHttpHandler : HttpMessageHandler
{
    private readonly List<(string PathContains, string Method, string Json)> _stubs = [];

    internal void AddStub(string pathContains, string method, string json)
        => _stubs.Add((pathContains, method, json));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;
        var method = request.Method.Method;

        foreach (var (contains, stubMethod, json) in _stubs)
        {
            if (path.Contains(contains, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(stubMethod, method, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"error":{"type":"invalid_request_error","message":"not found"}}""", Encoding.UTF8, "application/json")
        });
    }
}
