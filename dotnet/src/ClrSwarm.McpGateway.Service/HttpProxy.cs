
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;

namespace ClrSwarm.McpGateway.Service;

public static class HttpProxy
{
    public static HttpRequestMessage CreateProxiedHttpRequest(HttpContext context, Func<Uri, Uri>? targetOverride = null)
    {
        // If the incoming request uses Transfer-Encoding: chunked it must be represented
        // by an HttpContent on the HttpRequestMessage. Create a StreamContent when the
        // incoming request has a body length or explicitly includes the Transfer-Encoding header.
        var hasTransferEncodingHeader = context.Request.Headers.ContainsKey(HeaderNames.TransferEncoding);
        var hasBody = context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > 0;
        HttpContent? content = null;
        if (hasBody || hasTransferEncodingHeader)
        {
            content = new StreamContent(context.Request.Body);
        }

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = targetOverride == null ? new Uri(context.Request.GetEncodedUrl()) : targetOverride(new Uri(context.Request.GetEncodedUrl())),
            Content = content
        };

        foreach (var header in context.Request.Headers)
        {
            // Skip the inbound Authorization header
            if (string.Equals(header.Key, HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]))
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
        }

        requestMessage.Headers.TryAddWithoutValidation("Forwarded", $"for={context.Connection.RemoteIpAddress};proto={context.Request.Scheme};host={context.Request.Host.Value}");
        return requestMessage;
    }

    public static Task CopyProxiedHttpResponseAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();
        foreach (var header in response.Content.Headers)
            context.Response.Headers[header.Key] = header.Value.ToArray();

        context.Response.Headers.Remove(HeaderNames.TransferEncoding);

        return response.Content.CopyToAsync(context.Response.Body, cancellationToken);
    }
}
