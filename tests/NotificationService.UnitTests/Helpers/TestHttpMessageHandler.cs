using System.Net;

namespace NotificationService.UnitTests.Helpers;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handlerFunc;
    private readonly List<HttpRequestMessage> _sentRequests = new();
    private readonly object _lock = new();

    public IReadOnlyList<HttpRequestMessage> SentRequests
    {
        get
        {
            lock (_lock)
            {
                return _sentRequests.ToList();
            }
        }
    }

    public TestHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    public TestHttpMessageHandler(HttpResponseMessage staticResponse)
        : this(_ => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = staticResponse.StatusCode,
            Content = staticResponse.Content
        }))
    {
    }

    public TestHttpMessageHandler(HttpStatusCode statusCode, string content)
        : this(req => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        }))
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _sentRequests.Add(request);
        }

        return await _handlerFunc(request);
    }
}
