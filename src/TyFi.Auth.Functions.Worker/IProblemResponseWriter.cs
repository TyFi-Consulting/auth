using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace TyFi.Auth.Functions.Worker;

/// <summary>Writes an error response for a rejected invocation. Replace via DI for a custom problem-details schema.</summary>
public interface IProblemResponseWriter
{
    /// <summary>Creates an error response with the given status code and title.</summary>
    Task<HttpResponseData> WriteAsync(HttpRequestData request, HttpStatusCode statusCode, string title, CancellationToken cancellationToken);
}
