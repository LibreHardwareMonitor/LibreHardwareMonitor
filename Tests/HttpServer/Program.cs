// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Windows.Forms.UI;
using LibreHardwareMonitor.Windows.Forms.Utilities;

Computer computer = new();
HttpServer server = new(new Node("HTTP regression"), computer, "127.0.0.1", 18086);
using HttpClient client = new() { BaseAddress = new Uri("http://127.0.0.1:18086"), Timeout = TimeSpan.FromSeconds(2) };
int passed = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS " + message);
    passed++;
}
try
{
    Check(server.StartHttpListener(), "start listener");
    foreach (string path in new[] { "/data.json", "/data.json?_=123", "/?cache=1", "/images_icon/cpu.png?v=1" })
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        Check(response.StatusCode == HttpStatusCode.OK, "route " + path);
    }
    foreach (string path in new[] { "/Sensor", "/Sensor?action=Get&id=/missing", "/Sensor?bare" })
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Check(response.StatusCode == HttpStatusCode.BadRequest && body.RootElement.GetProperty("result").GetString() == "fail", "bounded JSON error " + path);
    }
    using (HttpResponseMessage response = await client.GetAsync("/notSensor"))
        Check(response.StatusCode == HttpStatusCode.NotFound, "exact sensor route");
    using (HttpResponseMessage response = await client.GetAsync("/notResetAllMinMax"))
        Check(response.StatusCode == HttpStatusCode.NotFound, "exact reset route");
    using (HttpRequestMessage request = new(HttpMethod.Get, "/Sensor?action=Set&id=/missing&value=1"))
    {
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
        using HttpResponseMessage response = await client.SendAsync(request);
        Check(response.StatusCode == HttpStatusCode.Forbidden, "cross-site writes remain blocked");
    }
    using (HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, "/data.json")))
        Check(response.StatusCode == HttpStatusCode.MethodNotAllowed && response.Content.Headers.Allow.Contains("GET"), "unsupported method");

    foreach ((string header, bool expected) in new[] { ("gzip", true), ("gzip;q=0", false), ("*;q=1, gzip;q=0", false), ("br, GZIP;q=0.5", true), ("xgzip", false), ("*;q=0.5", true), ("gzip;q=invalid", false) })
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "/data.json");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", header);
        using HttpResponseMessage response = await client.SendAsync(request);
        if (header == "gzip;q=invalid" && response.StatusCode == HttpStatusCode.BadRequest)
        {
            Check(true, "HTTP stack rejects malformed encoding quality");
            continue;
        }
        bool compressed = response.Content.Headers.ContentEncoding.Contains("gzip");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        using MemoryStream input = new(bytes);
        using Stream body = compressed ? new GZipStream(input, CompressionMode.Decompress) : input;
        using JsonDocument json = await JsonDocument.ParseAsync(body);
        Check(compressed == expected && response.Headers.Vary.Contains("Accept-Encoding"), "encoding " + header);
    }
    await Task.WhenAll(Enumerable.Range(0, 32).Select(async i =>
    {
        using HttpResponseMessage response = await client.GetAsync("/data.json?n=" + i);
        response.EnsureSuccessStatusCode();
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }));
    Check(true, "32 concurrent JSON requests after errors");

    for (int i = 0; i < 5; i++)
    {
        await Task.Delay(30);
        Stopwatch watch = Stopwatch.StartNew();
        Check(server.StopHttpListener() && watch.ElapsedMilliseconds < 1000, "idle shutdown under one second " + i);
        Check(server.StartHttpListener(), "restart " + i);
        using HttpResponseMessage response = await client.GetAsync("/data.json");
        response.EnsureSuccessStatusCode();
    }
    server.StopHttpListener();
    server.AuthEnabled = true;
    server.UserName = "test";
    server.SetPassword("local-regression");
    Check(server.StartHttpListener(), "start authenticated listener");
    using (HttpResponseMessage response = await client.GetAsync("/data.json"))
        Check(response.StatusCode == HttpStatusCode.Unauthorized, "authentication required");
    using (HttpRequestMessage request = new(HttpMethod.Get, "/data.json"))
    {
        request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("test:incorrect")));
        using HttpResponseMessage response = await client.SendAsync(request);
        Check(response.StatusCode == HttpStatusCode.Unauthorized, "incorrect password rejected");
    }
    using (HttpRequestMessage request = new(HttpMethod.Get, "/data.json"))
    {
        request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("test:local-regression")));
        using HttpResponseMessage response = await client.SendAsync(request);
        Check(response.StatusCode == HttpStatusCode.OK, "valid authentication");
    }
    Console.WriteLine($"{passed} checks passed.");
}
finally { server.Quit(); }
