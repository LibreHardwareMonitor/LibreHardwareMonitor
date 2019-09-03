/*

  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.

	Copyright (C) 2012 Prince Samuel <prince.samuel@gmail.com>
  Copyright (C) 2012-2013 Michael Möller <mmoeller@openhardwaremonitor.org>

*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenHardwareMonitor.GUI;
using OpenHardwareMonitor.Hardware;
using System.Web;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace OpenHardwareMonitor.Utilities {

  public class HttpServer {
    private HttpListener listener;
    private int listenerPort;
    private Thread listenerThread;
    private Node root;

    public HttpServer(Node node, int port) {
      root = node;
      listenerPort = port;

      try {
        listener = new HttpListener();
        listener.IgnoreWriteExceptions = true;
      } catch (PlatformNotSupportedException) {
        listener = null;
      }
    }

    public bool PlatformNotSupported {
      get {
        return listener == null;
      }
    }

    public Boolean StartHTTPListener() {
      if (PlatformNotSupported)
        return false;

      try {
        if (listener.IsListening)
          return true;

        string prefix = "http://+:" + listenerPort + "/";
        listener.Prefixes.Clear();
        listener.Prefixes.Add(prefix);
        listener.Start();

        if (listenerThread == null) {
          listenerThread = new Thread(HandleRequests);
          listenerThread.Start();
        }
      } catch (Exception) {
        return false;
      }

      return true;
    }

    public Boolean StopHTTPListener() {
      if (PlatformNotSupported)
        return false;

      try {
        if (listenerThread != null)
          listenerThread.Abort();
        listener.Stop();
        listenerThread = null;
      } catch (HttpListenerException) {
      } catch (ThreadAbortException) {
      } catch (NullReferenceException) {
      } catch (Exception) {
      }
      return true;
    }

    private void HandleRequests() {

      while (listener.IsListening) {
        var context = listener.BeginGetContext(
          new AsyncCallback(ListenerCallback), listener);
        context.AsyncWaitHandle.WaitOne();
      }
    }

    public static IDictionary<string, string> ToDictionary(NameValueCollection col) {
      IDictionary<string, string> dict = new Dictionary<string, string>();
      foreach (var k in col.AllKeys) {
        dict.Add(k, col[k]);
      }
      return dict;
    }

    public SensorNode FindSensor(Node node, string id) {
      if (node is SensorNode) {
        SensorNode sNode = (SensorNode)node;
        if (sNode.Sensor.Identifier.ToString() == id)
          return sNode;
      }
      foreach (Node child in node.Nodes) {
        SensorNode s = FindSensor(child, id);
        if (s != null) {
          return s;
        }
      }
      return null;
    }

    public void SetSensorControlValue(SensorNode sNode, string value) {
      if(sNode.Sensor.Control == null) {
        throw new ArgumentException("Specified sensor '" + sNode.Sensor.Identifier + "' can not be set");
      }
      if (value == "null") {
        sNode.Sensor.Control.SetDefault();
      }
      else {
        sNode.Sensor.Control.SetSoftware(float.Parse(value, CultureInfo.InvariantCulture));
      }
    }

    /*
     * Handles "/Sensor" requests.
     * Parameters are taken from the query part of the URL.
     * Get:
     * http://localhost:8085/Sensor?action=Get&id=/some/node/path/0
     * The output is either:
     * {"result":"fail","message":"Some error message"}
     * or:
     * {"result":"ok","value":42.0, "format":"{0:F2} RPM"}
     *
     * Set:
     * http://localhost:8085/Sensor?action=Set&id=/some/node/path/0&value=42.0
     * http://localhost:8085/Sensor?action=Set&id=/some/node/path/0&value=null
     * The output is either:
     * {"result":"fail","message":"Some error message"}
     * or:
     * {"result":"ok"}
     */
    private void HandleSensorRequest(HttpListenerRequest request, JObject result) {
      IDictionary<string, string> dict = ToDictionary(HttpUtility.ParseQueryString(request.Url.Query));

      if (dict.ContainsKey("action")) {
        if (dict.ContainsKey("id")) {
          SensorNode sNode = FindSensor(root, dict["id"]);

          if(sNode == null) {
            throw new ArgumentException("Unknown id " + dict["id"] + " specified");
          }

          if (dict["action"] == "Set") {
            if (dict.ContainsKey("value")) {
              SetSensorControlValue(sNode, dict["value"]);
            }
            else {
              throw new ArgumentNullException("No value provided");
            }
          }
          else if (dict["action"] == "Get") {
            result["value"] = sNode.Sensor.Value;
            result["format"] = sNode.format;
          }
          else {
            throw new ArgumentException("Unknown action type " + dict["action"]);
          }
        }
        else {
          throw new ArgumentNullException("No id provided");
        }
      }
      else {
        throw new ArgumentNullException("No action provided");
      }
    }

    /*
     * Handles http POST requests in a REST like manner.
     * Currently the only supported base URL is http://localhost:8085/Sensor.
     */
    private string HandlePostRequest(HttpListenerRequest request) {
      JObject result = new JObject();

      result["result"] = "ok";

      try {
        if (request.Url.Segments.Length == 2) {
          if (request.Url.Segments[1] == "Sensor") {
            HandleSensorRequest(request, result);
          }
          else
            throw new ArgumentException("Invalid URL ('" + request.Url.Segments[1] + "'), possible values: ['Sensor']");
        }
        else
          throw new ArgumentException("Empty URL, possible values: ['Sensor']");
      }
      catch(Exception e) {
        result["result"] = "fail";
        result["message"] = e.ToString();
      }
#if DEBUG
      return result.ToString(Newtonsoft.Json.Formatting.Indented);
#else
      return result.ToString(Newtonsoft.Json.Formatting.None);
#endif
    }

    private void ListenerCallback(IAsyncResult result) {
      HttpListener listener = (HttpListener)result.AsyncState;
      if (listener == null || !listener.IsListening)
        return;

      // Call EndGetContext to complete the asynchronous operation.
      HttpListenerContext context;
      try {
        context = listener.EndGetContext(result);
      } catch (Exception) {
        return;
      }

      HttpListenerRequest request = context.Request;

      if (request.HttpMethod == "POST") {
        string postResult = HandlePostRequest(request);

        Stream output = context.Response.OutputStream;
        byte[] utfBytes = Encoding.UTF8.GetBytes(postResult);

        context.Response.AddHeader("Cache-Control", "no-cache");
        context.Response.ContentLength64 = utfBytes.Length;
        context.Response.ContentType = "application/json";

        output.Write(utfBytes, 0, utfBytes.Length);
        output.Close();
      }
      else if(request.HttpMethod == "GET") {
        var requestedFile = request.RawUrl.Substring(1);

        if (requestedFile == "data.json") {
          SendJSON(context.Response);
          return;
        }

        if (requestedFile.Contains("images_icon")) {
          ServeResourceImage(context.Response,
            requestedFile.Replace("images_icon/", ""));
          return;
        }

        // default file to be served
        if (string.IsNullOrEmpty(requestedFile))
          requestedFile = "index.html";

        string[] splits = requestedFile.Split('.');
        string ext = splits[splits.Length - 1];
        ServeResourceFile(context.Response, "Web." + requestedFile.Replace('/', '.'), ext);
      }
      else {
        context.Response.StatusCode = 404;
        context.Response.Close();
      }
    }

    private void ServeResourceFile(HttpListenerResponse response, string name,
      string ext)
    {
      // resource names do not support the hyphen
      name = "OpenHardwareMonitor.Resources." +
        name.Replace("custom-theme", "custom_theme");

      string[] names =
        Assembly.GetExecutingAssembly().GetManifestResourceNames();
      for (int i = 0; i < names.Length; i++) {
        if (names[i].Replace('\\', '.') == name) {
          using (Stream stream = Assembly.GetExecutingAssembly().
            GetManifestResourceStream(names[i])) {
            response.ContentType = GetContentType("." + ext);
            response.ContentLength64 = stream.Length;
            byte[] buffer = new byte[512 * 1024];
            int len;
            try {
              Stream output = response.OutputStream;
              while ((len = stream.Read(buffer, 0, buffer.Length)) > 0) {
                output.Write(buffer, 0, len);
              }
              output.Flush();
              output.Close();
              response.Close();
            } catch (HttpListenerException) {
            } catch (InvalidOperationException) {
            }
            return;
          }
        }
      }

      response.StatusCode = 404;
      response.Close();
    }

    private void ServeResourceImage(HttpListenerResponse response, string name) {
      name = "OpenHardwareMonitor.Resources." + name;

      string[] names =
        Assembly.GetExecutingAssembly().GetManifestResourceNames();
      for (int i = 0; i < names.Length; i++) {
        if (names[i].Replace('\\', '.') == name) {
          using (Stream stream = Assembly.GetExecutingAssembly().
            GetManifestResourceStream(names[i])) {

            Image image = Image.FromStream(stream);
            response.ContentType = "image/png";
            try {
              Stream output = response.OutputStream;
              using (MemoryStream ms = new MemoryStream()) {
                image.Save(ms, ImageFormat.Png);
                ms.WriteTo(output);
              }
              output.Close();
            } catch (HttpListenerException) {
            }
            image.Dispose();
            response.Close();
            return;
          }
        }
      }

      response.StatusCode = 404;
      response.Close();
    }

    private void SendJSON(HttpListenerResponse response) {
      JObject json = new JObject();

      int nodeIndex = 0;

      json["id"] = nodeIndex++;
      json["Text"] = "Sensor";
      json["Min"] = "Min";
      json["Value"] = "Value";
      json["Max"] = "Max";
      json["ImageURL"] = "";

      JArray children = new JArray();
      children.Add(GenerateJSONForNode(root, ref nodeIndex));
      json["Children"] = children;
#if DEBUG
      var responseContent = json.ToString(Newtonsoft.Json.Formatting.Indented);
#else
      var responseContent = json.ToString(Newtonsoft.Json.Formatting.None);
#endif
      byte[] buffer = Encoding.UTF8.GetBytes(responseContent);

      response.AddHeader("Cache-Control", "no-cache");
      response.ContentLength64 = buffer.Length;
      response.ContentType = "application/json";

      try {
        Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
      } catch (HttpListenerException) {
      }

      response.Close();
    }

    private JObject GenerateJSONForNode(Node n, ref int nodeIndex) {
      JObject jsonNode = new JObject();
      jsonNode["id"] = nodeIndex++;
      jsonNode["Text"] = n.Text;
      jsonNode["Min"] = "";
      jsonNode["Value"] = "";
      jsonNode["Max"] = "";

      if (n is SensorNode) {
        jsonNode["SensorId"] = ((SensorNode)n).Sensor.Identifier.ToString();
        jsonNode["Type"] = ((SensorNode)n).Sensor.SensorType.ToString();
        jsonNode["Min"] = ((SensorNode)n).Min;
        jsonNode["Value"] = ((SensorNode)n).Value;
        jsonNode["Max"] = ((SensorNode)n).Max;
        jsonNode["ImageURL"] = "images/transparent.png";
      }
      else if (n is HardwareNode) {
        jsonNode["ImageURL"] = "images_icon/" + GetHardwareImageFile((HardwareNode)n);
      }
      else if (n is TypeNode) {
        jsonNode["ImageURL"] = "images_icon/" + GetTypeImageFile((TypeNode)n);
      } else {
        jsonNode["ImageURL"] = "images_icon/computer.png";
      }

      JArray children = new JArray();
      foreach (Node child in n.Nodes) {
        children.Add(GenerateJSONForNode(child, ref nodeIndex));
      }

      jsonNode["Children"] = children;

      return jsonNode;
    }

    private static void ReturnFile(HttpListenerContext context, string filePath)
    {
      context.Response.ContentType =
        GetContentType(Path.GetExtension(filePath));
      const int bufferSize = 1024 * 512; //512KB
      var buffer = new byte[bufferSize];
      using (var fs = File.OpenRead(filePath)) {

        context.Response.ContentLength64 = fs.Length;
        int read;
        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
          context.Response.OutputStream.Write(buffer, 0, read);
      }

      context.Response.OutputStream.Close();
    }

    private static string GetContentType(string extension) {
      switch (extension) {
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

    private static string GetHardwareImageFile(HardwareNode hn) {

      switch (hn.Hardware.HardwareType) {
        case HardwareType.CPU:
          return "cpu.png";
        case HardwareType.GpuNvidia:
          return "nvidia.png";
        case HardwareType.GpuAti:
          return "ati.png";
        case HardwareType.HDD:
          return "hdd.png";
        case HardwareType.Heatmaster:
          return "bigng.png";
        case HardwareType.Mainboard:
          return "mainboard.png";
        case HardwareType.SuperIO:
          return "chip.png";
        case HardwareType.TBalancer:
          return "bigng.png";
        case HardwareType.RAM:
          return "ram.png";
        case HardwareType.Aquacomputer:
          return "acicon.png";
        case HardwareType.NIC:
          return "nic.png";
        default:
          return "cpu.png";
      }

    }

    private static string GetTypeImageFile(TypeNode tn) {

      switch (tn.SensorType) {
        case SensorType.Voltage:
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
        case SensorType.Throughput:
          return "internetspeed.png";
        default:
          return "power.png";
      }

    }

    public int ListenerPort {
      get { return listenerPort; }
      set { listenerPort = value; }
    }

    ~HttpServer() {
      if (PlatformNotSupported)
        return;

      StopHTTPListener();
      listener.Abort();
    }

    public void Quit() {
      if (PlatformNotSupported)
        return;

      StopHTTPListener();
      listener.Abort();
    }
  }
}
