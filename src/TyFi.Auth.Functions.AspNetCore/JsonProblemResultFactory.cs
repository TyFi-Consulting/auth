using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace TyFi.Auth.Functions.AspNetCore;

/// <inheritdoc cref="IProblemResultFactory" />
public sealed class JsonProblemResultFactory : IProblemResultFactory
{
    /// <inheritdoc />
    public IActionResult Create(HttpStatusCode statusCode, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new ObjectResult(new { title, status = (int)statusCode }) { StatusCode = (int)statusCode };
    }
}
