import { useMemo, useState } from "react";
import { BrowserRouter, Link, Navigate, Route, Routes, useNavigate } from "react-router-dom";
import TopNav from "./TopNav";

function AppContent() {
  const navigate = useNavigate();
  const [tab, setTab] = useState("ai");
  const [baseUrl, setBaseUrl] = useState("http://localhost:5193");
  const [accessToken, setAccessToken] = useState("");
  const [adminToken, setAdminToken] = useState("");
  const [aiToken, setAiToken] = useState("");
  const [currentUserName, setCurrentUserName] = useState("");
  const [errorView, setErrorView] = useState("No errors.");

  const [registerUsername, setRegisterUsername] = useState("user1");
  const [registerPassword, setRegisterPassword] = useState("user1234");
  const [username, setUsername] = useState("admin");
  const [password, setPassword] = useState("admin123");

  const [dialogs, setDialogs] = useState([]);
  const [currentDialogId, setCurrentDialogId] = useState("");
  const [chatPrompt, setChatPrompt] = useState("");
  const [chatMessages, setChatMessages] = useState([
    { role: "assistant", content: "Привет! Войди через Login и отправь сообщение." }
  ]);

  const [users, setUsers] = useState([]);
  const [adminCreateUsername, setAdminCreateUsername] = useState("");
  const [adminCreatePassword, setAdminCreatePassword] = useState("");
  const [adminCreateRole, setAdminCreateRole] = useState("AiUser");
  const [isCreateUserModalOpen, setIsCreateUserModalOpen] = useState(false);
  const [isEditUserModalOpen, setIsEditUserModalOpen] = useState(false);
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedAdminUser, setSelectedAdminUser] = useState(null);
  const [editUserName, setEditUserName] = useState("");
  const [editUserRole, setEditUserRole] = useState("User");
  const [newPasswordValue, setNewPasswordValue] = useState("");
  const [adminDialogUserId, setAdminDialogUserId] = useState("");
  const [adminDialogs, setAdminDialogs] = useState([]);
  const [adminCurrentDialogId, setAdminCurrentDialogId] = useState("");
  const [adminDialogMessages, setAdminDialogMessages] = useState([]);

  async function request(method, path, body, token) {
    try {
      const response = await fetch(baseUrl.trim().replace(/\/+$/, "") + path, {
        method,
        headers: {
          ...(body ? { "Content-Type": "application/json" } : {}),
          ...(token ? { Authorization: "Bearer " + token } : {})
        },
        ...(body ? { body: JSON.stringify(body) } : {})
      });
      const data = await response.json().catch(() => ({}));
      return { status: response.status, ok: response.ok, data };
    } catch (error) {
      return { status: 0, ok: false, data: { message: "Network error", detail: String(error) } };
    }
  }

  function handleResult(result) {
    setErrorView(result.ok ? "No errors." : JSON.stringify({
      status: result.status,
      message: result.data?.message ?? "Request failed",
      detail: result.data
    }, null, 2));
  }

  function tryGetUserNameFromToken(token) {
    try {
      const payloadPart = token.split(".")[1];
      if (!payloadPart) return "";
      const normalized = payloadPart.replace(/-/g, "+").replace(/_/g, "/");
      const padded = normalized + "=".repeat((4 - (normalized.length % 4)) % 4);
      const payload = JSON.parse(window.atob(padded));
      return payload.unique_name || payload.name || payload.sub || "";
    } catch {
      return "";
    }
  }

  async function loadDialogs(token = aiToken, forceDialogId = "") {
    if (!token) return;
    const result = await request("GET", "/ai/dialogs", null, token);
    handleResult(result);
    if (!result.ok) return;
    const incoming = Array.isArray(result.data) ? result.data : [];
    setDialogs(incoming);
    const nextDialogId = forceDialogId || (incoming.some((d) => d.id === currentDialogId) ? currentDialogId : incoming[0]?.id || "");
    setCurrentDialogId(nextDialogId);
    if (nextDialogId) {
      await loadDialogMessages(nextDialogId, token);
    } else {
      setChatMessages([{ role: "assistant", content: "Создай диалог, чтобы начать чат." }]);
    }
  }

  async function loadDialogMessages(dialogId, token = aiToken) {
    if (!dialogId) return;
    const result = await request("GET", `/ai/dialogs/${dialogId}/messages`, null, token);
    handleResult(result);
    if (!result.ok) return;
    const messages = Array.isArray(result.data) ? result.data : [];
    setChatMessages(messages.length ? messages : [{ role: "assistant", content: "Пустой диалог. Отправь первое сообщение." }]);
  }

  async function loginAndStore(navigate) {
    const result = await request("POST", "/auth/login", { username: username.trim(), password });
    handleResult(result);
    if (!result.ok) return;
    const token = result.data?.accessToken || "";
    setAccessToken(token);
    setAdminToken(token);
    setAiToken(token);
    setCurrentUserName(tryGetUserNameFromToken(token) || username.trim());
    await loadDialogs(token);
    navigate("/app");
  }

  async function sendChat() {
    const prompt = chatPrompt.trim();
    if (!prompt) return setErrorView("Enter a message for AI chat.");
    if (!aiToken) return setErrorView("Login first or paste JWT token in AI Token field.");

    setChatMessages((prev) => [...prev, { role: "user", content: prompt }]);
    setChatPrompt("");

    const result = await request("POST", "/ai/chat", { prompt, dialogId: currentDialogId || null }, aiToken);
    handleResult(result);
    if (!result.ok) {
      setChatMessages((prev) => [...prev, { role: "assistant", content: "AI request failed." }]);
      return;
    }

    const dialogId = result.data?.dialogId || currentDialogId;
    setCurrentDialogId(dialogId);
    setChatMessages((prev) => [...prev, { role: "assistant", content: result.data?.response || "No response text." }]);
    await loadDialogs(aiToken, dialogId);
  }

  async function loadUsers() {
    const result = await request("GET", "/admin/users", null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    const incomingUsers = Array.isArray(result.data) ? result.data : [];
    setUsers(incomingUsers);
    if (!adminDialogUserId && incomingUsers[0]?.id) {
      setAdminDialogUserId(incomingUsers[0].id);
    }
  }

  async function loadAdminDialogs(forceUserId = adminDialogUserId, forceDialogId = "") {
    if (!adminToken) return;
    const query = forceUserId ? `?userId=${encodeURIComponent(forceUserId)}` : "";
    const result = await request("GET", `/admin/dialogs${query}`, null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    const incoming = Array.isArray(result.data) ? result.data : [];
    setAdminDialogs(incoming);
    const nextDialogId = forceDialogId || (incoming.some((d) => d.id === adminCurrentDialogId) ? adminCurrentDialogId : incoming[0]?.id || "");
    setAdminCurrentDialogId(nextDialogId);
    if (nextDialogId) {
      await loadAdminDialogMessages(nextDialogId);
    } else {
      setAdminDialogMessages([]);
    }
  }

  async function loadAdminDialogMessages(dialogId = adminCurrentDialogId) {
    if (!dialogId) {
      setAdminDialogMessages([]);
      return;
    }
    const result = await request("GET", `/admin/dialogs/${dialogId}/messages`, null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    setAdminDialogMessages(Array.isArray(result.data) ? result.data : []);
  }

  async function createAdminDialog() {
    if (!adminDialogUserId) return setErrorView("Выбери пользователя для нового диалога.");
    const title = window.prompt("Название нового диалога:", "Новый диалог");
    if (title === null) return;
    const result = await request("POST", "/admin/dialogs", { userId: adminDialogUserId, title }, adminToken);
    handleResult(result);
    if (result.ok) {
      await loadAdminDialogs(adminDialogUserId, result.data?.id);
    }
  }

  async function renameAdminDialog() {
    if (!adminCurrentDialogId) return setErrorView("Сначала выбери диалог.");
    const current = adminDialogs.find((d) => d.id === adminCurrentDialogId);
    const title = window.prompt("Новое название диалога:", current?.title || "Новый диалог");
    if (!title || !title.trim()) return;
    const result = await request("PUT", `/admin/dialogs/${adminCurrentDialogId}`, { title }, adminToken);
    handleResult(result);
    if (result.ok) {
      await loadAdminDialogs(adminDialogUserId, adminCurrentDialogId);
    }
  }

  async function deleteAdminDialog() {
    if (!adminCurrentDialogId) return setErrorView("Сначала выбери диалог.");
    if (!window.confirm("Удалить выбранный диалог?")) return;
    const deletingId = adminCurrentDialogId;
    const result = await request("DELETE", `/admin/dialogs/${deletingId}`, null, adminToken);
    handleResult(result);
    if (result.ok) {
      await loadAdminDialogs(adminDialogUserId);
    }
  }

  async function createAdminUser() {
    const result = await request(
      "POST",
      "/admin/users",
      { username: adminCreateUsername.trim(), password: adminCreatePassword, role: adminCreateRole },
      adminToken
    );
    handleResult(result);
    if (result.ok) {
      setAdminCreateUsername("");
      setAdminCreatePassword("");
      setAdminCreateRole("AiUser");
      setIsCreateUserModalOpen(false);
      await loadUsers();
    }
  }

  async function saveAdminUserFromModal() {
    if (!selectedAdminUser) return;
    const result = await request(
      "PUT",
      `/admin/users/${selectedAdminUser.id}`,
      { username: editUserName.trim(), role: editUserRole },
      adminToken
    );
    handleResult(result);
    if (result.ok) {
      setIsEditUserModalOpen(false);
      setSelectedAdminUser(null);
      await loadUsers();
    }
  }

  async function saveAdminPasswordFromModal() {
    if (!selectedAdminUser) return;
    if (!newPasswordValue.trim()) return setErrorView("Введи новый пароль.");
    const result = await request(
      "PUT",
      `/admin/users/${selectedAdminUser.id}/password`,
      { password: newPasswordValue },
      adminToken
    );
    handleResult(result);
    if (result.ok) {
      setNewPasswordValue("");
      setIsPasswordModalOpen(false);
      setSelectedAdminUser(null);
    }
  }

  async function deleteAdminUserFromModal() {
    if (!selectedAdminUser) return;
    const result = await request("DELETE", `/admin/users/${selectedAdminUser.id}`, null, adminToken);
    handleResult(result);
    if (result.ok) {
      setIsDeleteModalOpen(false);
      setSelectedAdminUser(null);
      await loadUsers();
    }
  }

  function openEditModal(user) {
    setSelectedAdminUser(user);
    setEditUserName(user.username || "");
    setEditUserRole(user.role || "User");
    setIsEditUserModalOpen(true);
  }

  function openPasswordModal(user) {
    setSelectedAdminUser(user);
    setNewPasswordValue("");
    setIsPasswordModalOpen(true);
  }

  function openDeleteModal(user) {
    setSelectedAdminUser(user);
    setIsDeleteModalOpen(true);
  }

  const dialogOptions = useMemo(
    () => dialogs.map((d) => <option key={d.id} value={d.id}>{d.title || "Dialog"}</option>),
    [dialogs]
  );
  const adminUserOptions = useMemo(
    () => users.map((u) => <option key={u.id} value={u.id}>{u.username} ({u.role})</option>),
    [users]
  );
  const adminDialogOptions = useMemo(
    () => adminDialogs.map((d) => <option key={d.id} value={d.id}>{d.title || "Dialog"} ({d.username || "unknown"})</option>),
    [adminDialogs]
  );

  const isLoggedIn = Boolean(accessToken);

  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route
        path="/login"
        element={
          <main className="auth-page">
            <section className="auth-card">
              <h1>Вход</h1>
              <p className="subtitle">Авторизуйся для доступа к приложению</p>
              <label>Username</label>
              <input value={username} onChange={(e) => setUsername(e.target.value)} />
              <label>Password</label>
              <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" />
              <button onClick={() => loginAndStore(navigate)}>Войти</button>
              <p className="auth-link">Нет аккаунта? <Link to="/register">Регистрация</Link></p>
            </section>
          </main>
        }
      />
      <Route
        path="/register"
        element={
          <main className="auth-page">
            <section className="auth-card">
              <h1>Регистрация</h1>
              <p className="subtitle">Создай новый аккаунт пользователя</p>
              <label>Username</label>
              <input value={registerUsername} onChange={(e) => setRegisterUsername(e.target.value)} />
              <label>Password</label>
              <input value={registerPassword} onChange={(e) => setRegisterPassword(e.target.value)} type="password" />
              <button
                onClick={async () => {
                  const result = await request("POST", "/auth/register", { username: registerUsername.trim(), password: registerPassword });
                  handleResult(result);
                  if (result.ok) navigate("/login");
                }}
              >
                Зарегистрироваться
              </button>
              <p className="auth-link">Уже есть аккаунт? <Link to="/login">Вход</Link></p>
            </section>
          </main>
        }
      />
      <Route
        path="/app"
        element={
          !isLoggedIn ? <Navigate to="/login" replace /> : (
            <main className="app">
              <TopNav
                tab={tab}
                currentUserName={currentUserName || username || "unknown"}
                onTabChange={(id) => {
                  setTab(id);
                  if (id === "admin") {
                    loadUsers();
                    loadAdminDialogs();
                  }
                }}
                onLogout={() => {
                  setAccessToken("");
                  setAdminToken("");
                  setAiToken("");
                  setCurrentUserName("");
                  navigate("/login");
                }}
              />

        {tab === "ai" && <section className="card-grid">
          <section className="card">
            <h3>AI Chat</h3>
            <div className="row">
              <select value={currentDialogId} onChange={(e) => { setCurrentDialogId(e.target.value); loadDialogMessages(e.target.value); }}>
                <option value="">No dialogs</option>
                {dialogOptions}
              </select>
              <button onClick={async () => { const title = window.prompt("Введите название диалога:", "Новый диалог") || ""; const r = await request("POST", "/ai/dialogs", { title }, aiToken); handleResult(r); if (r.ok) await loadDialogs(aiToken, r.data?.id); }}>New</button>
              <button onClick={async () => { if (!currentDialogId) return; const current = dialogs.find((d) => d.id === currentDialogId); const title = window.prompt("Новое название:", current?.title || "Новый диалог"); if (!title) return; const r = await request("PUT", `/ai/dialogs/${currentDialogId}`, { title }, aiToken); handleResult(r); if (r.ok) await loadDialogs(aiToken, currentDialogId); }}>Rename</button>
              <button onClick={async () => { if (!currentDialogId) return; if (!window.confirm("Удалить диалог?")) return; const r = await request("DELETE", `/ai/dialogs/${currentDialogId}`, null, aiToken); handleResult(r); if (r.ok) await loadDialogs(aiToken); }}>Delete</button>
            </div>
            <div className="chat-messages">
              {chatMessages.map((m, i) => <div key={i} className={`chat-msg ${m.role === "user" ? "user" : "assistant"}`}>{m.content}</div>)}
            </div>
            <div className="row">
              <input value={chatPrompt} onChange={(e) => setChatPrompt(e.target.value)} placeholder="Type your message..." />
              <button onClick={sendChat}>Send</button>
            </div>
          </section>
        </section>}

        {tab === "admin" && <section className="card-grid">
          <section className="card full-span">
            <h3>Пользователи</h3>
            <div className="row">
              <button onClick={() => setIsCreateUserModalOpen(true)}>New User</button>
              <button onClick={loadUsers}>Reload</button>
            </div>
            <div className="users-table-wrap">
              <table className="users-table">
                <thead>
                  <tr>
                    <th>Username</th>
                    <th>Role</th>
                    <th>New Password</th>
                    <th>Created</th>
                    <th>User ID</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.length === 0 && <tr><td colSpan="6">No users loaded</td></tr>}
                  {users.map((u) => (
                    <tr key={u.id}>
                      <td>{u.username}</td>
                      <td>{u.role}</td>
                      <td>••••••••</td>
                      <td>{u.createdAtUtc ? new Date(u.createdAtUtc).toLocaleString() : "-"}</td>
                      <td>{u.id}</td>
                      <td className="admin-actions">
                        <button onClick={() => openEditModal(u)}>Edit</button>
                        <button onClick={() => openPasswordModal(u)}>Password</button>
                        <button className="danger-btn" onClick={() => openDeleteModal(u)}>Delete</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className="card full-span">
            <h3>Диалоги (Admin)</h3>
            <label>Пользователь</label>
            <div className="row">
              <select
                value={adminDialogUserId}
                onChange={(e) => {
                  const nextUserId = e.target.value;
                  setAdminDialogUserId(nextUserId);
                  setAdminCurrentDialogId("");
                  setAdminDialogMessages([]);
                  loadAdminDialogs(nextUserId);
                }}
              >
                <option value="">All users</option>
                {adminUserOptions}
              </select>
            </div>

            <label>Диалог</label>
            <div className="row">
              <select value={adminCurrentDialogId} onChange={(e) => { setAdminCurrentDialogId(e.target.value); loadAdminDialogMessages(e.target.value); }}>
                <option value="">No dialogs</option>
                {adminDialogOptions}
              </select>
              <button onClick={renameAdminDialog}>Rename</button>
              <button className="danger-btn" onClick={deleteAdminDialog}>Delete</button>
            </div>

            <div className="chat-messages small">
              {adminDialogMessages.length === 0 && <div className="chat-msg assistant">Нет сообщений для выбранного диалога.</div>}
              {adminDialogMessages.map((m) => (
                <div key={m.id} className={`chat-msg ${m.role === "user" ? "user" : "assistant"}`}>
                  {m.content}
                </div>
              ))}
            </div>
          </section>
        </section>}

        {tab === "admin" && isCreateUserModalOpen && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Создать пользователя</h3>
              <label>Username</label>
              <input
                value={adminCreateUsername}
                onChange={(e) => setAdminCreateUsername(e.target.value)}
                placeholder="username"
              />
              <label>Password</label>
              <input
                value={adminCreatePassword}
                onChange={(e) => setAdminCreatePassword(e.target.value)}
                placeholder="password"
                type="password"
              />
              <label>Role</label>
              <select value={adminCreateRole} onChange={(e) => setAdminCreateRole(e.target.value)}>
                <option>User</option><option>AiUser</option><option>Admin</option>
              </select>
              <div className="row">
                <button onClick={createAdminUser}>Create</button>
                <button className="ghost-btn" onClick={() => setIsCreateUserModalOpen(false)}>Cancel</button>
              </div>
            </div>
          </div>
        )}
        {tab === "admin" && isEditUserModalOpen && selectedAdminUser && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Редактировать пользователя</h3>
              <label>Username</label>
              <input value={editUserName} onChange={(e) => setEditUserName(e.target.value)} />
              <label>Role</label>
              <select value={editUserRole} onChange={(e) => setEditUserRole(e.target.value)}>
                <option>User</option><option>AiUser</option><option>Admin</option>
              </select>
              <div className="row">
                <button onClick={saveAdminUserFromModal}>Save</button>
                <button className="ghost-btn" onClick={() => setIsEditUserModalOpen(false)}>Cancel</button>
              </div>
            </div>
          </div>
        )}
        {tab === "admin" && isPasswordModalOpen && selectedAdminUser && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Сменить пароль</h3>
              <p className="subtitle">{selectedAdminUser.username}</p>
              <label>New Password</label>
              <input
                value={newPasswordValue}
                onChange={(e) => setNewPasswordValue(e.target.value)}
                type="password"
                placeholder="new password"
              />
              <div className="row">
                <button onClick={saveAdminPasswordFromModal}>Save Password</button>
                <button className="ghost-btn" onClick={() => setIsPasswordModalOpen(false)}>Cancel</button>
              </div>
            </div>
          </div>
        )}
        {tab === "admin" && isDeleteModalOpen && selectedAdminUser && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Удалить пользователя</h3>
              <p className="subtitle">Ты точно хочешь удалить: <b>{selectedAdminUser.username}</b>?</p>
              <div className="row">
                <button className="danger-btn" onClick={deleteAdminUserFromModal}>Delete</button>
                <button className="ghost-btn" onClick={() => setIsDeleteModalOpen(false)}>Cancel</button>
              </div>
            </div>
          </div>
        )}

            </main>
          )
        }
      />
    </Routes>
  );
}

function AppShell() {
  return (
    <BrowserRouter>
      <AppContent />
    </BrowserRouter>
  );
}

export default AppShell;
