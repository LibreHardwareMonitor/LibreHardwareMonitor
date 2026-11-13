# LibreHardwareMonitor MCP connector

This read-only MCP server lets an MCP-compatible OrderDesk program query sensor data from LibreHardwareMonitor. It runs on the OrderDesk machine and reads the monitored PC/server's existing `data.json` endpoint.

## Setup

1. On the monitored PC/server, run LibreHardwareMonitor and enable its web server from the **Web Server** menu. Select the network interface and port that the OrderDesk machine can reach.
2. Set `LHM_BASE_URL` on the machine running this connector to the server's base URL, for example `http://192.168.1.25:8085/`. It defaults to `http://127.0.0.1:8085/`.
3. If LibreHardwareMonitor authentication is enabled, set both `LHM_USERNAME` and `LHM_PASSWORD`.
4. From the repository root, publish the connector with `dotnet publish .\LibreHardwareMonitor.Mcp\LibreHardwareMonitor.Mcp.csproj -c Release`.
5. Configure the published DLL as an MCP stdio server in OrderDesk. For example:

```json
{
  "mcpServers": {
    "libre-hardware-monitor": {
      "command": "dotnet",
      "args": [
        "C:\\path\\to\\LibreHardwareMonitor.Mcp\\bin\\Release\\net10.0\\LibreHardwareMonitor.Mcp.dll"
      ],
      "env": {
        "LHM_BASE_URL": "http://192.168.1.25:8085/",
        "LHM_USERNAME": "monitor",
        "LHM_PASSWORD": "replace-with-your-password"
      }
    }
  }
}
```

The credentials are optional when server authentication is disabled. Never commit real credentials to configuration files.

## Tools

- `list_hardware_sensors` returns all available sensors, or filters by a substring of the hardware or sensor name. Each result includes the hardware name, sensor ID and type, raw current/min/max values, and formatted values.
- `get_hardware_sensor` returns one sensor by its exact sensor ID.

Both tools only read `data.json`; they do not call LibreHardwareMonitor's sensor-control endpoints.

## Network security

The built-in LibreHardwareMonitor web server uses HTTP. Basic authentication over HTTP does not encrypt credentials, and the server also exposes endpoints beyond this connector's read-only requests. Keep it on a trusted network, restrict access with the host firewall or a VPN, and use a TLS-terminating reverse proxy if credentials must cross an untrusted network.
