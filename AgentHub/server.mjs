import { createReadStream } from "node:fs";
import { stat } from "node:fs/promises";
import { createServer } from "node:http";
import { extname, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { randomUUID } from "node:crypto";

const root = fileURLToPath(new URL(".", import.meta.url));
const publicRoot = resolve(root, "public");
const port = Number.parseInt(process.env.PORT ?? "4173", 10);
const connections = [];
const tasks = [];
const contentTypes = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".svg": "image/svg+xml",
};

function sendJson(response, statusCode, value) {
  response.writeHead(statusCode, {
    "Cache-Control": "no-store",
    "Content-Type": "application/json; charset=utf-8",
    "X-Content-Type-Options": "nosniff",
  });
  response.end(JSON.stringify(value));
}

async function readJson(request) {
  let body = "";
  for await (const chunk of request) {
    body += chunk;
    if (body.length > 16_384) {
      throw new Error("Request body is too large.");
    }
  }

  return JSON.parse(body);
}

async function serveFile(request, response, pathname) {
  const requestedPath = pathname === "/" ? "/index.html" : decodeURIComponent(pathname);
  const filePath = resolve(publicRoot, `.${requestedPath}`);
  if (!filePath.startsWith(`${publicRoot}${sep}`)) {
    sendJson(response, 404, { error: "Not found." });
    return;
  }

  try {
    const fileInfo = await stat(filePath);
    if (!fileInfo.isFile()) {
      sendJson(response, 404, { error: "Not found." });
      return;
    }

    response.writeHead(200, {
      "Cache-Control": "no-store",
      "Content-Type": contentTypes[extname(filePath)] ?? "application/octet-stream",
      "X-Content-Type-Options": "nosniff",
    });
    createReadStream(filePath).pipe(response);
  } catch {
    sendJson(response, 404, { error: "Not found." });
  }
}

const server = createServer(async (request, response) => {
  const url = new URL(request.url ?? "/", "http://localhost");
  if (request.method === "GET" && url.pathname === "/api/state") {
    sendJson(response, 200, { connections, tasks });
    return;
  }

  if (request.method === "POST" && url.pathname === "/api/connections") {
    try {
      const body = await readJson(request);
      const name = typeof body.name === "string" ? body.name.trim() : "";
      const kind = body.kind === "Tool" ? "Tool" : "Agent";
      if (!name || name.length > 80) {
        sendJson(response, 400, { error: "Enter a name between 1 and 80 characters." });
        return;
      }

      const connection = { id: randomUUID(), name, kind, status: "Not connected" };
      connections.unshift(connection);
      sendJson(response, 201, connection);
    } catch (error) {
      sendJson(response, 400, { error: error.message || "Invalid request." });
    }
    return;
  }

  const connectionMatch = url.pathname.match(/^\/api\/connections\/([0-9a-f-]+)$/i);
  if (request.method === "DELETE" && connectionMatch) {
    const index = connections.findIndex((connection) => connection.id === connectionMatch[1]);
    if (index === -1) {
      sendJson(response, 404, { error: "Connection not found." });
      return;
    }
    connections.splice(index, 1);
    response.writeHead(204);
    response.end();
    return;
  }

  if (request.method === "POST" && url.pathname === "/api/tasks") {
    try {
      const body = await readJson(request);
      const prompt = typeof body.prompt === "string" ? body.prompt.trim() : "";
      if (!prompt || prompt.length > 2_000) {
        sendJson(response, 400, { error: "Enter a task between 1 and 2,000 characters." });
        return;
      }

      const task = {
        id: randomUUID(),
        prompt,
        status: "Demo",
        result: "This starter does not call an LLM yet. Configure a model adapter to run this task for real.",
        createdAt: new Date().toISOString(),
      };
      tasks.unshift(task);
      sendJson(response, 201, task);
    } catch (error) {
      sendJson(response, 400, { error: error.message || "Invalid request." });
    }
    return;
  }

  if (request.method === "GET" && !url.pathname.startsWith("/api/")) {
    await serveFile(request, response, url.pathname);
    return;
  }

  sendJson(response, 404, { error: "Not found." });
});

server.listen(port, "127.0.0.1", () => {
  console.log(`Agent Hub is running at http://127.0.0.1:${port}`);
  console.log("Local-only starter: connections and tasks are held in memory.");
});
