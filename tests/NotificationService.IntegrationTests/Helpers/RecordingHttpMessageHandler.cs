using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace NotificationService.IntegrationTests.Helpers;

public class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpRequestMessage> _recordedRequests;
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public IReadOnlyCollection<HttpRequestMessage> RecordedRequests => _recordedRequests.ToArray();

    public RecordingHttpMessageHandler(
        ConcurrentQueue<HttpRequestMessage>? sharedQueue = null,
        Func<HttpRequestMessage, HttpResponseMessage>? responseFactory = null)
    {
        _recordedRequests = sharedQueue ?? new ConcurrentQueue<HttpRequestMessage>();
        _responseFactory = responseFactory ?? DefaultResponseFactory;
    }

    public static HttpResponseMessage DefaultResponseFactory(HttpRequestMessage req)
    {
        var uri = req.RequestUri?.ToString() ?? "";
        if (uri.Contains("chat/completions"))
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                        "choices": [
                            {
                                "message": {
                                    "role": "assistant",
                                    "content": "⚠️ Forwarded alert from test source."
                                }
                            }
                        ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"
                )
            };
        }

        // Discord webhook standard response is 204 No Content or 200 OK
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _recordedRequests.Enqueue(request);
        return Task.FromResult(_responseFactory(request));
    }
}
