using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;

namespace TyFi.Auth.Functions.Worker.Tests;

// Fixture "functions" used only to exercise AuthorizationRequirementResolver's reflection over
// real [Authorize]/[AllowAnonymous] attributes.
public sealed class AuthorizationRequirementResolverFixtures
{
    [Function("Anonymous")]
    [AllowAnonymous]
    public void AnonymousFunction()
    {
    }

    [Function("WithPolicy")]
    [Authorize("sessions:read")]
    public void FunctionWithPolicy()
    {
    }

    [Function("NoAttributes")]
    public void FunctionWithNoAttributes()
    {
    }

    [Function("AuthenticatedOnly")]
    [AuthorizeAuthenticated]
    public void FunctionRequiringAuthenticatedOnly()
    {
    }

    [Function("AuthenticatedOnlyWithPolicy")]
    [Authorize("sessions:read")]
    [AuthorizeAuthenticated]
    public void FunctionWithBothPolicyAndAuthenticatedOnly()
    {
    }
}
