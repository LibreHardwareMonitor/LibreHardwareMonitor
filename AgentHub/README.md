# Agent Hub Starter

A standalone, local-first starter for an AI agent and tool coordination dashboard. It uses only Node.js built-ins and has no package dependencies.

## Run

Requires Node.js 20 or newer.

```sh
npm start
```

Open [http://127.0.0.1:4173](http://127.0.0.1:4173). Set `PORT` to use a different local port.

## Current functionality

- Responsive dashboard layout for desktop and mobile-sized screens.
- Add and remove agent/tool connection placeholders.
- Create and review demo tasks.
- Local-only HTTP server bound to `127.0.0.1`.

The app is explicitly a UI and API starter: it does not connect to LLM providers, execute tools, implement MCP or A2A, persist data, or support access from other devices. Connections and tasks are in memory and disappear when the server restarts. Do not expose this starter to a network; authentication, device pairing, authorization, persistence, and approval workflows must be implemented before remote access or real tool execution is added.

## Next implementation milestones

1. Add provider adapters for a selected OpenAI-compatible endpoint and local model server.
2. Add agent profiles and hub-mediated delegation.
3. Add an MCP client with explicit per-tool permissions and user approval.
4. Persist non-secret settings and task history; keep credentials in a protected secret store.
5. Add authentication and device pairing before enabling LAN or VPN access.
