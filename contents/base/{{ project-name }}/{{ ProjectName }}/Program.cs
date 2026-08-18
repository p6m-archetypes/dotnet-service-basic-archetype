using System.Net;
using {{ ProjectName }};
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Formatting.Json;

// Configure Serilog early so bootstrap errors are captured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new JsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // The platform env contract (S3): PAO injects UPPER_SNAKE variables (SERVER_PORT,
    // MANAGEMENT_PORT, HOST). .NET's default binder matches PROPERTY names, not those, so map each
    // contract variable that is present onto its Settings key before binding — without this the
    // service silently ignores the ports the platform gave it and binds its compiled-in defaults.
    // Property-name env (Port=...) still works; the contract layers on top. A basic service weaves
    // in no resources, so the three keys here are the whole contract it has to honor.
    var platformEnv = new Dictionary<string, string>
    {
        ["HOST"] = "Host",
        ["SERVER_PORT"] = "Port",
        ["MANAGEMENT_PORT"] = "ManagementPort",
    };
    var contractOverrides = new Dictionary<string, string?>();
    foreach (var (env, key) in platformEnv)
    {
        if (builder.Configuration[env] is { Length: > 0 } value) contractOverrides[key] = value;
    }
    builder.Configuration.AddInMemoryCollection(contractOverrides);

    var settings = builder.Configuration.Get<Settings>() ?? new Settings();

    // Structured logging via Serilog (JSON when LOGGING_STRUCTURED=true)
    builder.Host.UseSerilog((ctx, services, cfg) =>
    {
        var structured = ctx.Configuration["LOGGING_STRUCTURED"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        if (structured)
            cfg.WriteTo.Console(new JsonFormatter());
        else
            cfg.WriteTo.Console();
        cfg.ReadFrom.Configuration(ctx.Configuration);
    });

    // OpenTelemetry tracing (fail-open: no-op when OTEL_EXPORTER_OTLP_ENDPOINT is absent)
    var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService(
            serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "{{ project-name }}"))
        .WithTracing(t =>
        {
            t.AddAspNetCoreInstrumentation();
            if (!string.IsNullOrEmpty(otlpEndpoint))
                t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        });

    // Two Kestrel endpoints: service traffic on service_port, management on management_port.
    // Skipped in Testing so the test HTTP client uses a single in-process server.
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, settings.Port);
            options.Listen(IPAddress.Any, settings.ManagementPort);
        });
    }

    var app = builder.Build();

    // Management endpoints: health + Prometheus metrics.
    // In production these are only reachable on management_port; in Testing the
    // test client accesses them on the single shared in-process server.
    app.MapGet("/health/readiness", () => Results.Ok(new { status = "ok" }));
    app.MapGet("/health/liveness", () => Results.Ok(new { status = "ok" }));
    app.UseMetricServer(settings.ManagementPort); // prometheus-net: GET /metrics on management_port

    // Application route: a single stub endpoint reporting service identity.
    app.MapGet("/", () => Results.Ok(new { service = "{{ project-name }}", status = "ok" }));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

public partial class Program { }
