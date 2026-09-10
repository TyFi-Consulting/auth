using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using TyFi.Auth.OtpMailer.Auth;
using TyFi.Auth.OtpMailer.Handling;
using TyFi.Auth.OtpMailer.Mail;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddOtpMailerTokenValidation(builder.Configuration);
builder.Services.AddMailerooEmailSender(builder.Configuration);
builder.Services.AddOnOtpSendHandling();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
