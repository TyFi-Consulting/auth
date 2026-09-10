using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace TyFi.Auth.Functions.AspNetCore;

/// <summary>Builds the <see cref="IActionResult"/> for a rejected invocation. Replace via DI for a custom problem-details schema.</summary>
public interface IProblemResultFactory
{
    /// <summary>Creates an error result with the given status code and title.</summary>
    IActionResult Create(HttpStatusCode statusCode, string title);
}
