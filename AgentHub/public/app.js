const connectionList = document.querySelector("#connection-list");
const connectionCount = document.querySelector("#connection-count");
const connectionsEmpty = document.querySelector("#connections-empty");
const connectionForm = document.querySelector("#connection-form");
const taskList = document.querySelector("#task-list");

function escapeHtml(value) {
  return value.replace(/[&<>"']/g, (character) => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;",
  })[character]);
}

async function request(path, options) {
  const response = await fetch(path, {
    ...options,
    headers: { "Content-Type": "application/json", ...options?.headers },
  });
  if (!response.ok) {
    const result = await response.json().catch(() => ({}));
    throw new Error(result.error ?? `Request failed (${response.status}).`);
  }
  return response.status === 204 ? null : response.json();
}

function renderState({ connections, tasks }) {
  connectionCount.textContent = String(connections.length);
  connectionsEmpty.classList.toggle("hidden", connections.length > 0);
  connectionList.innerHTML = connections.map((connection) => `
    <div class="connection-row">
      <div class="connection-main">
        <div class="connection-icon">${connection.kind === "Tool" ? "⌘" : "✧"}</div>
        <div>
          <div class="connection-name">${escapeHtml(connection.name)}</div>
          <div class="connection-type">${connection.kind}</div>
        </div>
      </div>
      <div class="connection-main">
        <span class="connection-status">${connection.status}</span>
        <button class="remove-button" data-remove="${connection.id}" aria-label="Remove ${escapeHtml(connection.name)}">Remove</button>
      </div>
    </div>
  `).join("");

  taskList.innerHTML = tasks.map((task) => `
    <article class="task-row">
      <div class="task-row-top">
        <p>${escapeHtml(task.prompt)}</p>
        <span class="task-time">${new Date(task.createdAt).toLocaleString()}</span>
      </div>
      <p class="task-result">${escapeHtml(task.result)}</p>
    </article>
  `).join("");
}

async function refresh() {
  renderState(await request("/api/state"));
}

function openConnectionForm() {
  connectionForm.classList.remove("hidden");
  document.querySelector("#connection-name").focus();
}

document.querySelector("#open-connection-form").addEventListener("click", openConnectionForm);
document.querySelector("#empty-add-connection").addEventListener("click", openConnectionForm);
document.querySelector("#cancel-connection-form").addEventListener("click", () => {
  connectionForm.reset();
  connectionForm.classList.add("hidden");
});

connectionForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const submitButton = connectionForm.querySelector('button[type="submit"]');
  submitButton.disabled = true;
  try {
    await request("/api/connections", {
      method: "POST",
      body: JSON.stringify({
        name: document.querySelector("#connection-name").value,
        kind: document.querySelector("#connection-kind").value,
      }),
    });
    connectionForm.reset();
    connectionForm.classList.add("hidden");
    await refresh();
  } catch (error) {
    window.alert(error.message);
  } finally {
    submitButton.disabled = false;
  }
});

connectionList.addEventListener("click", async (event) => {
  const button = event.target.closest("[data-remove]");
  if (!button) return;
  try {
    await request(`/api/connections/${encodeURIComponent(button.dataset.remove)}`, { method: "DELETE" });
    await refresh();
  } catch (error) {
    window.alert(error.message);
  }
});

document.querySelector("#task-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const prompt = document.querySelector("#task-prompt");
  const submitButton = event.currentTarget.querySelector('button[type="submit"]');
  submitButton.disabled = true;
  try {
    await request("/api/tasks", { method: "POST", body: JSON.stringify({ prompt: prompt.value }) });
    prompt.value = "";
    await refresh();
  } catch (error) {
    window.alert(error.message);
  } finally {
    submitButton.disabled = false;
  }
});

refresh().catch((error) => window.alert(`Could not load Agent Hub: ${error.message}`));
