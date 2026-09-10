using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace TyFi.Auth.Functions.Worker;

/// <inheritdoc cref="IProblemResponseWriter" />
public sealed class JsonProblemResponseWriter : IProblemResponseWriter
{
    /// <inheritdoc />
    public async Task<HttpResponseData> WriteAsync(HttpRequestData request, HttpStatusCode statusCode, string title, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var response = request.CreateResponse(statusCode);
        // The convenience WriteAsJsonAsync(value, ct) overload defaults its own statusCode
        // parameter to 200 OK and applies it unconditionally, so it must be passed explicitly here.
        await response.WriteAsJsonAsync(new { title, status = (int)statusCode }, statusCode, cancellationToken).ConfigureAwait(false);
        return response;
    }
}
