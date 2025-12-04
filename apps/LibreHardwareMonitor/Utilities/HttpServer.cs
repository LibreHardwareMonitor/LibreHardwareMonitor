// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI;

namespace LibreHardwareMonitor.Utilities;

public class HttpServer
{
    private readonly HttpListener _listener;
    private readonly Node _root;
    private readonly IElement _rootElement;
    private Task _listenerTask;
    private CancellationTokenSource _cts;

    public HttpServer(Node node, IElement rootElement, string ip, int port, bool authEnabled = false, string userName = "", string password = "")
    {
        _root = node;
        _rootElement = rootElement;
        ListenerIp = ip;
        ListenerPort = port;
        AuthEnabled = authEnabled;
        UserName = userName;
        Password = password;

        try
        {
            _listener = new HttpListener { IgnoreWriteExceptions = true };
        }
        catch (PlatformNotSupportedException)
        {
            _listener = null;
        }
    }

    ~HttpServer()
    {
        if (PlatformNotSupported)
            return;

        StopHttpListener();
        try
        {
            _listener?.Abort();
        }
        catch { }
    }

    public bool AuthEnabled { get; set; }

    public string ListenerIp { get; set; }

    public int ListenerPort { get; set; }

    public string Password
    {
        get { return PasswordSHA256; }
        set { PasswordSHA256 = ComputeSHA256(value); }
    }

    public bool PlatformNotSupported
    {
        get { return _listener == null; }
    }

    public string UserName { get; set; }

    private string PasswordSHA256 { get; set; }

    public bool StartHttpListener()
    {
        if (PlatformNotSupported)
            return false;

        try
        {
            if (_listener.IsListening)
                return true;

            // validate that the selected IP exists (it could have been previously selected before switching networks)
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            bool ipFound = false;
            foreach (IPAddress ip in host.AddressList)
            {
                if (ListenerIp == ip.ToString())
                {
                    ipFound = true;
                    break;
                }
            }

            if (!ipFound)
            {
                // default to behavior of previous version if we don't know what interface to use.
                ListenerIp = "+";
            }

            string prefix = "http://" + ListenerIp + ":" + ListenerPort + "/";

            _listener.Prefixes.Clear();
            _listener.Prefixes.Add(prefix);
            _listener.Realm = "Libre Hardware Monitor";
            _listener.AuthenticationSchemes = AuthEnabled ? AuthenticationSchemes.Basic : AuthenticationSchemes.Anonymous;
            _listener.Start();

            _cts = new CancellationTokenSource();
            _listenerTask = Task.Run(() => ProcessRequestsAsync(_cts.Token));
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public bool StopHttpListener()
    {
        if (PlatformNotSupported)
            return false;

        try
        {
            _cts?.Cancel();
            _listenerTask?.Wait(TimeSpan.FromSeconds(5)); // Graceful wait
            _listener?.Stop();
            _cts?.Dispose();
        }
        catch (HttpListenerException)
        { }
        catch (OperationCanceledException)
        { }
        catch (NullReferenceException)
        { }
        catch (Exception)
        { }

        return true;
    }

    private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
    {
        while (_listener.IsListening && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleContextAsync(context), cancellationToken);
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 50)
            {
                // Handle Windows update bug (e.g., 2025-10 Cumulative Update): retry after delay
                System.Diagnostics.Debug.WriteLine($"HttpListener error (code {ex.ErrorCode}): {ex.Message}. Retrying in 5 seconds.");
                await Task.Delay(5000, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                break; // Listener stopped
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected HttpListener error: {ex.Message}");
            }
        }
    }

    public static IDictionary<string, string> ToDictionary(NameValueCollection col)
    {
        IDictionary<string, string> dict = new Dictionary<string, string>();
        foreach (string k in col.AllKeys)
        {
            dict.Add(k, col[k]);
        }

        return dict;
    }

    public SensorNode FindSensor(Node node, string id)
    {
        if (node is SensorNode sNode)
        {
            if (sNode.Sensor.Identifier.ToString() == id)
                return sNode;
        }

        foreach (Node child in node.Nodes)
        {
            SensorNode s = FindSensor(child, id);
            if (s != null)
            {
                return s;
            }
        }

        return null;
    }
    public void SetSensorControlValue(SensorNode sNode, string value)
    {
        if (sNode.Sensor.Control == null)
        {
            throw new ArgumentException("Specified sensor '" + sNode.Sensor.Identifier + "' can not be set");
        }

        if (value == "null")
        {
            sNode.Sensor.Control.SetDefault();
        }
        else
        {
            sNode.Sensor.Control.SetSoftware(float.Parse(value, CultureInfo.InvariantCulture));
        }
    }

    //Handles "/Sensor" requests.
    //Parameters are taken from the query part of the URL.
    //Get:
    //http://localhost:8085/Sensor?action=Get&id=/some/node/path/0
    //The output is either:
    //{"result":"fail","message":"Some error message"}
    //or:
    //{"result":"ok","value":42.0, "format":"{0:F2} RPM"}
    //
    //Set:
    //http://localhost:8085/Sensor?action=Set&id=/some/node/path/0&value=42.0
    //http://localhost:8085/Sensor?action=Set&id=/some/node/path/0&value=null
    //The output is either:
    //{"result":"fail","message":"Some error message"}
    //or:
    //{"result":"ok"}
    private void HandleSensorRequest(HttpListenerRequest request, Dictionary<string, object> result)
    {
        IDictionary<string, string> dict = ToDictionary(HttpUtility.ParseQueryString(request.Url.Query));

        if (dict.ContainsKey("action"))
        {
            if (dict.ContainsKey("id"))
            {
                SensorNode sNode = FindSensor(_root, dict["id"]);

                if (sNode == null)
                {
                    throw new ArgumentException("Unknown id " + dict["id"] + " specified");
                }

                if (dict["action"] == "ResetMinMax")
                {
                    // Reset Min/Max, then return Sensor values...
                    sNode.Sensor.ResetMin();
                    sNode.Sensor.ResetMax();
                    dict["action"] = "Get";
                }

                switch (dict["action"])
                {
                    case "Set" when dict.ContainsKey("value"):
                        SetSensorControlValue(sNode, dict["value"]);
                        break;
                    case "Set":
                        throw new ArgumentNullException("No value provided");
                    case "Get":
                        result["value"] = sNode.Sensor.Value;
                        result["min"] = sNode.Sensor.Min;
                        result["max"] = sNode.Sensor.Max;
                        result["format"] = sNode.Format;
                        break;
                    default:
                        throw new ArgumentException("Unknown action type " + dict["action"]);
                }
            }
            else
            {
                throw new ArgumentNullException("No id provided");
            }
        }
        else
        {
            throw new ArgumentNullException("No action provided");
        }
    }

    //Handles http POST requests in a REST like manner.
    //Currently the only supported base URL is http://localhost:8085/Sensor.
    private string HandlePostRequest(HttpListenerRequest request)
    {
        var result = new Dictionary<string, object> { ["result"] = "ok" };

        try
        {
            if (request.Url.Segments.Length == 2)
            {
                if (request.Url.Segments[1] == "Sensor")
                {
                    HandleSensorRequest(request, result);
                }
                else
                {
                    throw new ArgumentException("Invalid URL ('" + request.Url.Segments[1] + "'), possible values: ['Sensor']");
                }
            }
            else
                throw new ArgumentException("Empty URL, possible values: ['Sensor']");
        }
        catch (Exception e)
        {
            result["result"] = "fail";
            result["message"] = e.ToString();
        }
        return System.Text.Json.JsonSerializer.Serialize(result);
    }

    private async Task HandleContextAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        bool authenticated = true;

        if (AuthEnabled)
        {
            try
            {
                HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;
                authenticated = (identity.Name == UserName) && (ComputeSHA256(identity.Password) == PasswordSHA256);
            }
            catch
            {
                authenticated = false;
            }
        }

        if (authenticated)
        {
            switch (request.HttpMethod)
            {
                case "POST":
                    {
                        string postResult = HandlePostRequest(request);
                        await SendResponseAsync(context.Response, postResult, "application/json");
                        break;
                    }
                case "GET":
                    {
                        string requestedFile = request.RawUrl.Substring(1);

                        if (requestedFile == "data.json")
                        {
                            await SendJsonAsync(context.Response, request);
                            return;
                        }

                        if (requestedFile == "metrics")
                        {
                            await SendPrometheusAsync(context.Response, request);
                            return;
                        }

                        if (requestedFile.Contains("images_icon"))
                        {
                            await ServeResourceImageAsync(context.Response, requestedFile.Replace("images_icon/", string.Empty));
                            return;
                        }

                        if (requestedFile.Contains("Sensor"))
                        {
                            var sensorResult = new Dictionary<string, object>();
                            HandleSensorRequest(request, sensorResult);
                            await SendJsonSensorAsync(context.Response, sensorResult);
                            return;
                        }

                        if (requestedFile.Contains("ResetAllMinMax"))
                        {
                            _rootElement.Accept(new SensorVisitor(delegate (ISensor sensor)
                            {
                                sensor.ResetMin();
                                sensor.ResetMax();
                            }));
                            await SendJsonAsync(context.Response, request);
                            return;
                        }

                        // default file to be served
                        if (string.IsNullOrEmpty(requestedFile))
                            requestedFile = "index.html";

                        string[] splits = requestedFile.Split('.');
                        string ext = splits[splits.Length - 1];
                        await ServeResourceFileAsync(context.Response, "Web." + requestedFile.Replace('/', '.'), ext);
                        break;
                    }
                default:
                    {
                        context.Response.StatusCode = 404;
                        break;
                    }
            }
        }
        else
        {
            context.Response.StatusCode = 401;
        }

        if (context.Response.StatusCode == 401)
        {
            const string responseString = @"<HTML><HEAD><TITLE>401 Unauthorized</TITLE></HEAD>
  <BODY><H4>401 Unauthorized</H4>
  Authorization required.</BODY></HTML> ";

            await SendResponseAsync(context.Response, responseString, "text/html");
        }

        try
        {
            context.Response.Close();
        }
        catch
        {
            // client closed connection before the content was sent
        }
    }
    private async Task ServeResourceFileAsync(HttpListenerResponse response, string name, string ext)
    {
        // resource names do not support the hyphen
        name = "LibreHardwareMonitor.Resources." +
               name.Replace("custom-theme", "custom_theme");

        string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].Replace('\\', '.') == name)
            {
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(names[i]);

                response.ContentType = GetContentType("." + ext);
                response.ContentLength64 = stream.Length;
                byte[] buffer = new byte[512 * 1024];
                try
                {
                    int len;
                    while ((len = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await response.OutputStream.WriteAsync(buffer, 0, len);
                    }

                    await response.OutputStream.FlushAsync();
                    response.OutputStream.Close();
                    response.Close();
                }
                catch (HttpListenerException)
                { }
                catch (InvalidOperationException)
                { }

                return;
            }
        }

        response.StatusCode = 404;
        response.Close();
    }

    private async Task ServeResourceImageAsync(HttpListenerResponse response, string name)
    {
        name = "LibreHardwareMonitor.Resources." + name;

        string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].Replace('\\', '.') == name)
            {
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(names[i]);

                using Image image = Image.FromStream(stream);
                response.ContentType = "image/png";
                try
                {
                    using var ms = new MemoryStream();
                    image.Save(ms, ImageFormat.Png);
                    byte[] buffer = ms.ToArray();
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                catch (HttpListenerException)
                { }

                response.Close();
                return;
            }
        }

        response.StatusCode = 404;
        response.Close();
    }

    private async Task SendJsonAsync(HttpListenerResponse response, HttpListenerRequest request = null)
    {
        Dictionary<string, object> json = new();

        int nodeIndex = 0;

        json["id"] = nodeIndex++;
        json["Text"] = "Sensor";
        json["Min"] = "Min";
        json["Value"] = "Value";
        json["Max"] = "Max";
        json["ImageURL"] = string.Empty;

        json["Children"] = new List<object> { GenerateJsonForNode(_root, ref nodeIndex) };

        byte[] buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(json));

        bool acceptGzip;
        try
        {
            acceptGzip = (request != null) && (request.Headers["Accept-Encoding"].IndexOf("gzip", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        catch
        {
            acceptGzip = false;
        }

        response.AddHeader("Cache-Control", "no-cache");
        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.ContentType = "application/json";

        try
        {
            if (acceptGzip)
            {
                response.AddHeader("Content-Encoding", "gzip");
                using var ms = new MemoryStream();
                using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                    await zip.WriteAsync(buffer, 0, buffer.Length);

                buffer = ms.ToArray();
            }

            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        catch (HttpListenerException)
        { }

        response.Close();
    }

    private string GeneratePrometheusResponse(Node node)
    {
        string responseStr = "";
        string lastTagName = "";

        /// Dictionary to convert all data to base units for OpenMetrics
        /// suffix, factor
        var units = new Dictionary<SensorType, (string, double)>
        {
           { SensorType.Clock, ("hertz", 1000000)},                           //originally megahertz
           { SensorType.Conductivity, ("seconds_per_centimeter", 0.000001) }, //originally microseconds per centimeter
           { SensorType.Control, ("percent", 1) },
           { SensorType.Current, ("amperes", 1) },
           { SensorType.Data, ("bytes", 1000000000) },                        //originally GB
           { SensorType.Energy, ("watthour", 0.001) },
           { SensorType.Factor, ("", 1) },
           { SensorType.Fan, ("rpms", 1) },
           { SensorType.Flow, ("liters_per_hour", 1) },
           { SensorType.Frequency, ("hertz", 1) },
           { SensorType.Humidity, ("percent", 1) },
           { SensorType.Level, ("percent", 1) },
           { SensorType.Load, ("percent", 1) },
           { SensorType.Noise, ("decibels", 1) },
           { SensorType.Power, ("watts", 1) },
           { SensorType.SmallData, ("bytes", 1024*1024) },                    //originally MiB
           { SensorType.Temperature, ("celsius", 1) },
           { SensorType.Throughput, ("bytes_per_second", 1000) },             //originally KB
           { SensorType.TimeSpan, ("seconds", 1) },
           { SensorType.Timing, ("seconds", 0.000000001 ) },                  //originally nanoseconds
           { SensorType.Voltage, ("volts", 1) },
        };

        for (int i = 0; i < node.Nodes.Count; i++)
        {
            if (node.Nodes[i].GetType().Name == "HardwareNode")
            {
                responseStr += GeneratePrometheusResponse(node.Nodes[i]);
            }

            if (node.Nodes[i].GetType().Name == "TypeNode")
            {
                string prometheusHost = node.Parent.Text;
                foreach (SensorNode sensor in node.Nodes[i].Nodes)
                {
                    string tagHardware = (((HardwareNode)node).Hardware.Parent != null ? ((HardwareNode)node).Hardware.Parent.HardwareType.ToString() : ((HardwareNode)node).Hardware.HardwareType.ToString());
                    string tagSensorType = sensor.Sensor.SensorType.ToString();
                    string tagSensorUnits = (units[sensor.Sensor.SensorType].Item1.Length == 0 ? "" : "_" + units[sensor.Sensor.SensorType].Item1);
                    string tagName = $"lhm_{tagHardware}_{tagSensorType}{tagSensorUnits}";
                    tagName = tagName.ToLower();

                    string valueSensor = sensor.Text.Replace("#", "");
                    string valueHardware = node.Text;
                    string valueId = sensor.Sensor.Identifier.ToString().Split('/').Last();
                    string valueFamily = node.Nodes[i].Text;
                    string valueHost = _root.Text;

                    try
                    {

                        if (lastTagName != tagName)
                        {
                            responseStr += $"# TYPE {tagName} gauge\n";
                            lastTagName = tagName;
                        }

                        double? tagValue = units[sensor.Sensor.SensorType].Item2 * sensor.Sensor.Value;
                        responseStr += $$"""{{tagName}} {"sensor"="{{valueSensor}}" "hardware"="{{valueHardware}}" "id"="{{valueId}}" "family"="{{valueFamily}}" "host"="{{valueHost}}"} {{tagValue}}""" + "\n";
                    }
                    catch (Exception)
                    {
                        responseStr += $"# HELP {lastTagName} This Sensor type is not defined in the prometheus adapter [{sensor.Sensor.SensorType}]\n";
                        responseStr += $$"""{{tagName}} {"sensor"="{{valueSensor}}" "hardware"="{{valueHardware}}" "id"="{{valueId}}" "family"="{{valueFamily}}" "host"="{{valueHost}}"} {{sensor.Sensor.Value}}""" + "\n";
                    }
                }
            }
        }
        return responseStr;
    }

    private async Task SendPrometheusAsync(HttpListenerResponse response, HttpListenerRequest request = null)
    {
        string responseContent = GeneratePrometheusResponse(_root);
        response.AddHeader("Cache-Control", "no-cache");
        response.AddHeader("Access-Control-Allow-Origin", "*");
        await SendResponseAsync(response, responseContent, "text/plain");
    }

    private async Task SendJsonSensorAsync(HttpListenerResponse response, Dictionary<string, object> sensorData)
    {
        // Convert the JObject to a JSON string
        string responseContent = System.Text.Json.JsonSerializer.Serialize(sensorData);
        response.AddHeader("Cache-Control", "no-cache");
        response.AddHeader("Access-Control-Allow-Origin", "*");
        await SendResponseAsync(response, responseContent, "application/json");
    }
        
    private Dictionary<string, object> GenerateJsonForNode(Node n, ref int nodeIndex)
    {
        Dictionary<string, object> jsonNode = new()
        {
            ["id"] = nodeIndex++,
            ["Text"] = n.Text,
            ["Min"] = string.Empty,
            ["Value"] = string.Empty,
            ["Max"] = string.Empty
        };

        switch (n)
        {
            case SensorNode sensorNode:
                jsonNode["SensorId"] = sensorNode.Sensor.Identifier.ToString();
                jsonNode["Type"] = sensorNode.Sensor.SensorType.ToString();

                // Formatted values, e.g. Throughput will be measured in KB/s or MB/s depending on the value
                jsonNode["Min"] = sensorNode.Min;
                jsonNode["Value"] = sensorNode.Value;
                jsonNode["Max"] = sensorNode.Max;

                // Unformatted values for external systems to have consistent readings, e.g. Throughput will always be measured in B/s
                jsonNode["RawMin"] = string.Format(sensorNode.Format, sensorNode.Sensor.Min);
                jsonNode["RawValue"] = string.Format(sensorNode.Format, sensorNode.Sensor.Value);
                jsonNode["RawMax"] = string.Format(sensorNode.Format, sensorNode.Sensor.Max);

                jsonNode["ImageURL"] = "images/transparent.png";
                break;
            case HardwareNode hardwareNode:
                jsonNode["HardwareId"] = hardwareNode.Hardware.Identifier.ToString();
                jsonNode["ImageURL"] = "images_icon/" + GetHardwareImageFile(hardwareNode);
                break;
            case TypeNode typeNode:
                jsonNode["ImageURL"] = "images_icon/" + GetTypeImageFile(typeNode);
                break;
            default:
                jsonNode["ImageURL"] = "images_icon/computer.png";
                break;
        }

        List<object> children = new();
        foreach (Node child in n.Nodes)
        {
            children.Add(GenerateJsonForNode(child, ref nodeIndex));
        }

        jsonNode["Children"] = children;

        return jsonNode;
    }

    private static string GetContentType(string extension)
    {
        switch (extension)
        {
            case ".avi": return "video/x-msvideo";
            case ".css": return "text/css";
            case ".doc": return "application/msword";
            case ".gif": return "image/gif";
            case ".htm":
            case ".html": return "text/html";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".js": return "application/x-javascript";
            case ".mp3": return "audio/mpeg";
            case ".png": return "image/png";
            case ".pdf": return "application/pdf";
            case ".ppt": return "application/vnd.ms-powerpoint";
            case ".zip": return "application/zip";
            case ".txt": return "text/plain";
            default: return "application/octet-stream";
        }
    }
    private static string GetHardwareImageFile(HardwareNode hn)
    {
        switch (hn.Hardware.HardwareType)
        {
            case HardwareType.Cpu:
                return "cpu.png";
            case HardwareType.GpuNvidia:
                return "nvidia.png";
            case HardwareType.GpuAmd:
                return "ati.png";
            case HardwareType.GpuIntel:
                return "intel.png";
            case HardwareType.Storage:
                return "hdd.png";
            case HardwareType.Motherboard:
                return "mainboard.png";
            case HardwareType.SuperIO:
                return "chip.png";
            case HardwareType.Memory:
                return "ram.png";
            case HardwareType.Cooler:
                return "fan.png";
            case HardwareType.Network:
                return "nic.png";
            case HardwareType.Psu:
                return "power-supply.png";
            case HardwareType.Battery:
                return "battery.png";
            default:
                return "cpu.png";
        }
    }

    private static string GetTypeImageFile(TypeNode tn)
    {
        switch (tn.SensorType)
        {
            case SensorType.Voltage:
            case SensorType.Current:
                return "voltage.png";
            case SensorType.Clock:
                return "clock.png";
            case SensorType.Load:
                return "load.png";
            case SensorType.Temperature:
                return "temperature.png";
            case SensorType.Fan:
                return "fan.png";
            case SensorType.Flow:
                return "flow.png";
            case SensorType.Control:
                return "control.png";
            case SensorType.Level:
                return "level.png";
            case SensorType.Power:
                return "power.png";
            case SensorType.Noise:
                return "loudspeaker.png";
            case SensorType.Conductivity:
                return "voltage.png";
            case SensorType.Throughput:
                return "throughput.png";
            case SensorType.Humidity:
                return "flow.png";
            default:
                return "power.png";
        }
    }

    private string ComputeSHA256(string text)
    {
        using SHA256 hash = SHA256.Create();
        return string.Concat(hash
                            .ComputeHash(Encoding.UTF8.GetBytes(text))
                            .Select(item => item.ToString("x2")));
    }

    public void Quit()
    {
        if (PlatformNotSupported)
            return;

        StopHttpListener();
        try
        {
            _listener?.Abort();
        }
        catch { }
    }

    private static async Task SendResponseAsync(HttpListenerResponse response, string content, string contentType)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        response.ContentType = contentType;
        response.ContentLength64 = buffer.Length;

        try
        {
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
        catch (HttpListenerException)
        { }
    }

}
