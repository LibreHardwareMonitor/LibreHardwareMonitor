using System.Net.Http.Headers;
using System.Text;
using LibreHardwareMonitor.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

string endpoint = Environment.GetEnvironmentVariable("LHM_BASE_URL") ?? "http://127.0.0.1:8085/";
if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? baseAddress) ||
    (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps) ||
    !string.IsNullOrEmpty(baseAddress.UserInfo) ||
    !string.IsNullOrEmpty(baseAddress.Query) ||
    !string.IsNullOrEmpty(baseAddress.Fragment))
{
    throw new InvalidOperationException("LHM_BASE_URL must be an absolute HTTP or HTTPS base URL without credentials, query, or fragment.");
}

if (!baseAddress.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
    baseAddress = new Uri(baseAddress.AbsoluteUri + "/", UriKind.Absolute);

string? username = Environment.GetEnvironmentVariable("LHM_USERNAME");
string? password = Environment.GetEnvironmentVariable("LHM_PASSWORD");
if (string.IsNullOrWhiteSpace(username) != string.IsNullOrWhiteSpace(password))
    throw new InvalidOperationException("Set both LHM_USERNAME and LHM_PASSWORD, or leave both unset.");

var httpClient = new HttpClient
{
    BaseAddress = baseAddress,
    Timeout = TimeSpan.FromSeconds(10)
};

if (!string.IsNullOrWhiteSpace(username))
{
    string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
}

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services.AddSingleton(httpClient);
builder.Services.AddSingleton<HardwareMonitorClient>();
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<HardwareTools>();

await builder.Build().RunAsync();
