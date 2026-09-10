using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace TyFi.Auth.Functions.Worker.Tests;

// Minimal hand-written test doubles for the Functions Worker's abstract HTTP types. These avoid
// FunctionContext.GetHttpRequestDataAsync() (which depends on an internal SDK feature and cannot be
// faked outside the real host), so they're only usable for code paths that receive an
// already-resolved HttpRequestData, such as IProblemResponseWriter.
internal sealed class FakeFunctionContext : FunctionContext
{
    public override string InvocationId => "test-invocation";
    public override string FunctionId => "test-function";
    public override TraceContext TraceContext => null!;
    public override BindingContext BindingContext => null!;
    public override RetryContext RetryContext => null!;
    public override IServiceProvider InstanceServices { get; set; } = null!;
    public override FunctionDefinition FunctionDefinition => null!;
    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
    public override IInvocationFeatures Features => null!;
}

internal sealed class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext functionContext)
        : base(functionContext)
    {
    }

    public override Stream Body { get; } = new MemoryStream();
    public override HttpHeadersCollection Headers { get; } = new();
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = [];
    public override Uri Url { get; } = new("https://example.test/");
    public override IEnumerable<ClaimsIdentity> Identities { get; } = [];
    public override string Method { get; } = "POST";

    public override HttpResponseData CreateResponse() => new FakeHttpResponseData(FunctionContext);
}

internal sealed class FakeHttpResponseData : HttpResponseData
{
    public FakeHttpResponseData(FunctionContext functionContext)
        : base(functionContext)
    {
    }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; } = new();
    public override Stream Body { get; set; } = new MemoryStream();
    public override HttpCookies Cookies { get; } = null!;
}
