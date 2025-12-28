using FxNyaa;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration().WriteTo
    .Console(
        outputTemplate: "[FALLBACK] [{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        theme: AnsiConsoleTheme.Sixteen)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen));

builder.Services.Configure<FxNyaaConfig>(
    builder.Configuration.GetSection("FxNyaa"));
builder.Services.ConfigureHttpClientDefaults(x =>
{
    x.RemoveAllLoggers().ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FxNyaa/NoSetVersion (https://github.com/SquirrelKiev/FxNyaa)");
        client.Timeout = TimeSpan.FromSeconds(10);
    });
    // nyaa started sending gzip compressed html whether this was specified or not
    // specifying this just means it at least gets parsed by dotnet
    x.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        { AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip });
});

builder.Services.AddControllers();

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseSerilogRequestLogging();

app.UseHostFiltering();

app.MapRazorPages();
app.MapControllers();

app.Run();