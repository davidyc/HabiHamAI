import { useEffect, useMemo, useRef, useState } from "react";
import { BrowserRouter, Link, Navigate, Route, Routes, useNavigate } from "react-router-dom";
import TopNav from "./TopNav";
import ModalShell from "./shared/ui/ModalShell";

function AppContent() {
  const getTodayIsoDate = () => new Date().toISOString().slice(0, 10);
  const getIsoDateDaysAgo = (daysAgo) => {
    const date = new Date();
    date.setDate(date.getDate() - daysAgo);
    return date.toISOString().slice(0, 10);
  };

  const navigate = useNavigate();
  const [tab, setTab] = useState("workouts");
  const [baseUrl, setBaseUrl] = useState(import.meta.env.VITE_API_BASE_URL || "http://localhost:5193");
  const [accessToken, setAccessToken] = useState("");
  const [adminToken, setAdminToken] = useState("");
  const [aiToken, setAiToken] = useState("");
  const [currentUserName, setCurrentUserName] = useState("");
  const [currentUserRole, setCurrentUserRole] = useState("");
  const [errorView, setErrorView] = useState("Ошибок нет.");

  const [registerUsername, setRegisterUsername] = useState("user1");
  const [registerPassword, setRegisterPassword] = useState("user1234");
  const [registerError, setRegisterError] = useState("");
  const [username, setUsername] = useState("admin");
  const [password, setPassword] = useState("admin123");
  const [loginError, setLoginError] = useState("");
  const [isLoginLoading, setIsLoginLoading] = useState(false);

  const [dialogs, setDialogs] = useState([]);
  const [currentDialogId, setCurrentDialogId] = useState("");
  const [chatPrompt, setChatPrompt] = useState("");
  const [chatMessages, setChatMessages] = useState([
    { role: "assistant", content: "Привет! Войди через форму входа и отправь сообщение." }
  ]);
  const [workoutSessions, setWorkoutSessions] = useState([]);
  const [workoutsSubTab, setWorkoutsSubTab] = useState("manage");
  const [workoutsManageSubTab, setWorkoutsManageSubTab] = useState("add");
  const [workoutExerciseCatalog, setWorkoutExerciseCatalog] = useState([]);
  const [selectedCatalogExerciseId, setSelectedCatalogExerciseId] = useState("");
  const [newCatalogExerciseName, setNewCatalogExerciseName] = useState("");
  const [newCatalogExerciseMeta, setNewCatalogExerciseMeta] = useState("");
  const [isCreateExerciseModalOpen, setIsCreateExerciseModalOpen] = useState(false);
  const [programCode, setProgramCode] = useState("");
  const [programDay, setProgramDay] = useState("");
  const [programNotes, setProgramNotes] = useState("");
  const [programExercisesDraft, setProgramExercisesDraft] = useState([]);
  const [isProgramModalOpen, setIsProgramModalOpen] = useState(false);
  const [isProgramDeleteModalOpen, setIsProgramDeleteModalOpen] = useState(false);
  const [editingProgramId, setEditingProgramId] = useState("");
  const [pendingDeleteProgram, setPendingDeleteProgram] = useState(null);
  const [selectedProgramCode, setSelectedProgramCode] = useState("");
  const [currentWorkout, setCurrentWorkout] = useState(null);
  const [profileBirthDate, setProfileBirthDate] = useState("");
  const [profileHeightCm, setProfileHeightCm] = useState("");
  const [profileWeightKg, setProfileWeightKg] = useState("");
  const [profilePhone, setProfilePhone] = useState("");
  const [profileCity, setProfileCity] = useState("");
  const [profileAbout, setProfileAbout] = useState("");
  const [profileFirstName, setProfileFirstName] = useState("");
  const [profileLastName, setProfileLastName] = useState("");
  const [profileAiSummary, setProfileAiSummary] = useState("");

  const [users, setUsers] = useState([]);
  const [adminCreateUsername, setAdminCreateUsername] = useState("");
  const [adminCreatePassword, setAdminCreatePassword] = useState("");
  const [adminCreateRole, setAdminCreateRole] = useState("AiUser");
  const [isCreateUserModalOpen, setIsCreateUserModalOpen] = useState(false);
  const [isEditUserModalOpen, setIsEditUserModalOpen] = useState(false);
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedAdminUser, setSelectedAdminUser] = useState(null);
  const [adminManageUserId, setAdminManageUserId] = useState("");
  const [editUserName, setEditUserName] = useState("");
  const [editUserRole, setEditUserRole] = useState("User");
  const [newPasswordValue, setNewPasswordValue] = useState("");
  const [adminDialogUserId, setAdminDialogUserId] = useState("");
  const [adminDialogs, setAdminDialogs] = useState([]);
  const [adminCurrentDialogId, setAdminCurrentDialogId] = useState("");
  const [adminDialogMessages, setAdminDialogMessages] = useState([]);
  const [isProfileEditModalOpen, setIsProfileEditModalOpen] = useState(false);
  const [isActiveWorkoutModalOpen, setIsActiveWorkoutModalOpen] = useState(false);
  const [aiDialogModalKind, setAiDialogModalKind] = useState(null);
  const [aiDialogTitleDraft, setAiDialogTitleDraft] = useState("");
  const [adminDialogModalKind, setAdminDialogModalKind] = useState(null);
  const [adminDialogTitleDraft, setAdminDialogTitleDraft] = useState("");
  const [aiAssistants, setAiAssistants] = useState([]);
  /** Пробный чат с помощником без смены «Включить» (передаётся assistantId в POST /ai/chat); задаётся из админки. */
  const [chatAssistantPreviewId, setChatAssistantPreviewId] = useState(null);
  const adminAssistantTestChatPanelRef = useRef(null);
  const [adminAiAssistants, setAdminAiAssistants] = useState([]);
  const [assistantModalKind, setAssistantModalKind] = useState(null);
  const [assistantDraft, setAssistantDraft] = useState({
    id: "",
    name: "",
    description: "",
    systemPrompt: "",
    settingsJson: "",
    sortOrder: 0,
    isActive: true
  });
  const [pendingDeleteAssistantId, setPendingDeleteAssistantId] = useState(null);
  const [isAiExtraInfoModalOpen, setIsAiExtraInfoModalOpen] = useState(false);
  const [aiExtraInfoAssistantId, setAiExtraInfoAssistantId] = useState("");
  const [aiExtraInfoDefinitions, setAiExtraInfoDefinitions] = useState([]);
  const [aiExtraInfoValues, setAiExtraInfoValues] = useState({});
  const [adminExtraFieldsList, setAdminExtraFieldsList] = useState([]);
  const [adminExtraFieldModalKind, setAdminExtraFieldModalKind] = useState(null);
  const [adminExtraFieldDraft, setAdminExtraFieldDraft] = useState({
    id: "",
    fieldKey: "",
    label: "",
    fieldType: "text",
    sortOrder: 0,
    isRequired: false
  });
  const [pendingDeleteExtraField, setPendingDeleteExtraField] = useState(null);
  const [pendingDeleteCatalogExerciseId, setPendingDeleteCatalogExerciseId] = useState(null);
  const [pendingDeleteWorkoutSessionId, setPendingDeleteWorkoutSessionId] = useState(null);
  const [pendingDeleteCurrentWorkoutExerciseId, setPendingDeleteCurrentWorkoutExerciseId] = useState(null);
  const [selectedWorkoutHistorySession, setSelectedWorkoutHistorySession] = useState(null);
  const [historyDateFrom, setHistoryDateFrom] = useState(() => getIsoDateDaysAgo(6));
  const [historyDateTo, setHistoryDateTo] = useState(() => getTodayIsoDate());
  const [historyWorkoutLogs, setHistoryWorkoutLogs] = useState([]);
  const planningImportInputRef = useRef(null);
  const exercisesImportInputRef = useRef(null);

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
      return { status: 0, ok: false, data: { message: "Ошибка сети", detail: String(error) } };
    }
  }

  function handleResult(result) {
    setErrorView(result.ok ? "Ошибок нет." : JSON.stringify({
      status: result.status,
      message: result.data?.message ?? "Запрос завершился с ошибкой",
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

  function tryGetRoleFromToken(token) {
    try {
      const payloadPart = token.split(".")[1];
      if (!payloadPart) return "";
      const normalized = payloadPart.replace(/-/g, "+").replace(/_/g, "/");
      const padded = normalized + "=".repeat((4 - (normalized.length % 4)) % 4);
      const payload = JSON.parse(window.atob(padded));
      const rawRole = payload.role || payload.roles || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || "";
      if (Array.isArray(rawRole)) return rawRole[0] || "";
      return String(rawRole || "");
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
    if (isLoginLoading) return;
    setIsLoginLoading(true);
    setLoginError("");
    try {
      const result = await request("POST", "/auth/login", { username: username.trim(), password });
      handleResult(result);
      if (!result.ok) {
        const message = result.data?.message
          || (result.status === 401 ? "Неверный логин или пароль." : "")
          || (result.status === 0 ? "Сервер недоступен. Проверь подключение и адрес API." : "")
          || "Не удалось выполнить вход. Попробуй еще раз.";
        setLoginError(message);
        return;
      }
      const token = result.data?.accessToken || "";
      setAccessToken(token);
      setAdminToken(token);
      setAiToken(token);
      const nextRole = tryGetRoleFromToken(token);
      setCurrentUserName(tryGetUserNameFromToken(token) || username.trim());
      setCurrentUserRole(nextRole);
      setTab("workouts");
      if (nextRole === "Admin" || nextRole === "AiUser") {
        await loadDialogs(token);
        await loadAiAssistants(token);
      }
      await loadMyProfile(token);
      navigate("/app");
    } finally {
      setIsLoginLoading(false);
    }
  }

  async function sendChat() {
    const prompt = chatPrompt.trim();
    if (!prompt) return setErrorView("Введите сообщение для AI помощника.");
    if (!aiToken) return setErrorView("Сначала войдите в систему или вставьте JWT-токен в поле AI-токена.");

    setChatMessages((prev) => [...prev, { role: "user", content: prompt }]);
    setChatPrompt("");

    const previewMatch = chatAssistantPreviewId
      ? aiAssistants.find((x) => String(x.id) === String(chatAssistantPreviewId))
        ?? adminAiAssistants.find((x) => String(x.id) === String(chatAssistantPreviewId))
      : null;
    const selectedAssistant = aiAssistants.find((x) => x.selected);
    const assistantIdForChat = previewMatch?.id ?? selectedAssistant?.id ?? null;
    const result = await request(
      "POST",
      "/ai/chat",
      {
        prompt,
        dialogId: currentDialogId || null,
        assistantId: assistantIdForChat ?? null
      },
      aiToken
    );
    handleResult(result);
    if (!result.ok) {
      setChatMessages((prev) => [...prev, { role: "assistant", content: "AI request failed." }]);
      return;
    }

    const dialogId = result.data?.dialogId || currentDialogId;
    setCurrentDialogId(dialogId);
    setChatMessages((prev) => [...prev, { role: "assistant", content: result.data?.response || "Нет текста ответа." }]);
    await loadDialogs(aiToken, dialogId);
    await loadMyProfile(aiToken);
  }

  async function submitAiNewDialog() {
    const title = (aiDialogTitleDraft || "").trim() || "Новый диалог";
    const r = await request("POST", "/ai/dialogs", { title }, aiToken);
    handleResult(r);
    if (r.ok) {
      setAiDialogModalKind(null);
      setAiDialogTitleDraft("");
      await loadDialogs(aiToken, r.data?.id);
    }
  }

  async function submitAiRenameDialog() {
    if (!currentDialogId) return setErrorView("Нет выбранного диалога.");
    const title = (aiDialogTitleDraft || "").trim();
    if (!title) return setErrorView("Введи название.");
    const r = await request("PUT", `/ai/dialogs/${currentDialogId}`, { title }, aiToken);
    handleResult(r);
    if (r.ok) {
      setAiDialogModalKind(null);
      setAiDialogTitleDraft("");
      await loadDialogs(aiToken, currentDialogId);
    }
  }

  async function submitAiDeleteDialog() {
    if (!currentDialogId) return;
    const r = await request("DELETE", `/ai/dialogs/${currentDialogId}`, null, aiToken);
    handleResult(r);
    if (r.ok) {
      setAiDialogModalKind(null);
      await loadDialogs(aiToken);
    }
  }

  async function loadAiAssistants(token = aiToken) {
    if (!token) return;
    const result = await request("GET", "/ai/assistants", null, token);
    handleResult(result);
    if (!result.ok) return;
    const list = Array.isArray(result.data?.assistants) ? result.data.assistants : [];
    setAiAssistants(list);
  }

  async function selectAiAssistant(assistantId) {
    const r = await request("PUT", "/ai/assistants/selection", { assistantId }, aiToken);
    handleResult(r);
    if (r.ok) await loadAiAssistants(aiToken);
  }

  async function toggleAiAssistantRow(id, selected) {
    if (selected) {
      await selectAiAssistant(null);
    } else {
      await selectAiAssistant(id);
    }
  }

  async function loadAdminAiAssistants() {
    const result = await request("GET", "/admin/ai-assistants", null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    setAdminAiAssistants(Array.isArray(result.data) ? result.data : []);
  }

  function openAssistantCreateModal() {
    setAdminExtraFieldsList([]);
    setAssistantDraft({
      id: "",
      name: "",
      description: "",
      systemPrompt: "",
      settingsJson: "",
      sortOrder: adminAiAssistants.length ? Math.max(...adminAiAssistants.map((x) => x.sortOrder || 0)) + 1 : 0,
      isActive: true
    });
    setAssistantModalKind("create");
  }

  function openAssistantEditModal(row) {
    setAssistantDraft({
      id: row.id,
      name: row.name || "",
      description: row.description || "",
      systemPrompt: row.systemPrompt || "",
      settingsJson: row.settingsJson || "",
      sortOrder: row.sortOrder ?? 0,
      isActive: Boolean(row.isActive)
    });
    setAssistantModalKind("edit");
    void loadAdminExtraFields(row.id);
  }

  async function submitAssistantModal() {
    const name = (assistantDraft.name || "").trim();
    const systemPrompt = (assistantDraft.systemPrompt || "").trim();
    if (!name) return setErrorView("Укажи название помощника.");
    if (!systemPrompt) return setErrorView("Укажи системный промпт.");

    const body = {
      name,
      description: (assistantDraft.description || "").trim() || null,
      systemPrompt,
      settingsJson: (assistantDraft.settingsJson || "").trim() || null,
      sortOrder: Number(assistantDraft.sortOrder) || 0,
      isActive: Boolean(assistantDraft.isActive)
    };

    const modeBeforeSave = assistantModalKind;
    let r;
    if (assistantModalKind === "create") {
      r = await request("POST", "/admin/ai-assistants", body, adminToken);
    } else if (assistantModalKind === "edit" && assistantDraft.id) {
      r = await request("PUT", `/admin/ai-assistants/${assistantDraft.id}`, body, adminToken);
    } else {
      return;
    }
    handleResult(r);
    if (!r.ok) return;

    await loadAdminAiAssistants();
    if (hasAiAccess) await loadAiAssistants(aiToken);

    if (modeBeforeSave === "create" && r.data?.id) {
      setAssistantDraft((d) => ({ ...d, id: r.data.id }));
      setAssistantModalKind("edit");
      await loadAdminExtraFields(r.data.id);
    } else if (assistantDraft.id) {
      await loadAdminExtraFields(assistantDraft.id);
    }
  }

  async function confirmDeleteAssistant() {
    if (!pendingDeleteAssistantId) return;
    const r = await request("DELETE", `/admin/ai-assistants/${pendingDeleteAssistantId}`, null, adminToken);
    handleResult(r);
    if (r.ok) {
      setPendingDeleteAssistantId(null);
      await loadAdminAiAssistants();
      if (hasAiAccess) await loadAiAssistants(aiToken);
    }
  }

  async function openAiExtraInfoModal(assistantId) {
    if (!assistantId) return;
    setAiExtraInfoAssistantId(assistantId);
    setIsAiExtraInfoModalOpen(true);
    const r = await request(
      "GET",
      `/ai/assistant-extra-fields?assistantId=${encodeURIComponent(assistantId)}`,
      null,
      aiToken
    );
    handleResult(r);
    if (!r.ok) return;
    const defs = Array.isArray(r.data?.definitions) ? r.data.definitions : [];
    const vals = r.data?.values && typeof r.data.values === "object" ? r.data.values : {};
    setAiExtraInfoDefinitions(defs);
    const merged = {};
    for (const d of defs) {
      merged[d.fieldKey] = vals[d.fieldKey] ?? "";
    }
    setAiExtraInfoValues(merged);
  }

  async function submitAiExtraInfoModal() {
    if (!aiExtraInfoAssistantId) return;
    const r = await request(
      "PUT",
      "/ai/assistant-extra-fields",
      { assistantId: aiExtraInfoAssistantId, values: aiExtraInfoValues },
      aiToken
    );
    handleResult(r);
    if (r.ok) setIsAiExtraInfoModalOpen(false);
  }

  async function loadAdminExtraFields(assistantId) {
    const r = await request("GET", `/admin/ai-assistants/${assistantId}/extra-fields`, null, adminToken);
    handleResult(r);
    if (!r.ok) return;
    setAdminExtraFieldsList(Array.isArray(r.data) ? r.data : []);
  }

  function openAdminExtraFieldCreate() {
    setAdminExtraFieldDraft({
      id: "",
      fieldKey: "",
      label: "",
      fieldType: "text",
      sortOrder: adminExtraFieldsList.length ? Math.max(...adminExtraFieldsList.map((x) => x.sortOrder || 0)) + 1 : 0,
      isRequired: false
    });
    setAdminExtraFieldModalKind("create");
  }

  function openAdminExtraFieldEdit(row) {
    setAdminExtraFieldDraft({
      id: row.id,
      fieldKey: row.fieldKey || "",
      label: row.label || "",
      fieldType: row.fieldType || "text",
      sortOrder: row.sortOrder ?? 0,
      isRequired: Boolean(row.isRequired)
    });
    setAdminExtraFieldModalKind("edit");
  }

  async function submitAdminExtraFieldModal() {
    const aid = assistantDraft.id;
    if (!aid) return setErrorView("Сначала сохрани помощника.");
    const label = (adminExtraFieldDraft.label || "").trim();
    const rawKey = (adminExtraFieldDraft.fieldKey || "").trim().toLowerCase().replace(/\s+/g, "_");
    if (!rawKey || !label) return setErrorView("Укажи ключ поля (латиница, цифры, _) и подпись.");
    const body = {
      fieldKey: rawKey,
      label,
      fieldType: adminExtraFieldDraft.fieldType || "text",
      sortOrder: Number(adminExtraFieldDraft.sortOrder) || 0,
      isRequired: Boolean(adminExtraFieldDraft.isRequired)
    };
    let r;
    if (adminExtraFieldModalKind === "create") {
      r = await request("POST", `/admin/ai-assistants/${aid}/extra-fields`, body, adminToken);
    } else if (adminExtraFieldModalKind === "edit" && adminExtraFieldDraft.id) {
      r = await request(
        "PUT",
        `/admin/ai-assistants/${aid}/extra-fields/${adminExtraFieldDraft.id}`,
        body,
        adminToken
      );
    } else {
      return;
    }
    handleResult(r);
    if (r.ok) {
      setAdminExtraFieldModalKind(null);
      await loadAdminExtraFields(aid);
    }
  }

  async function confirmDeleteExtraField() {
    if (!pendingDeleteExtraField) return;
    const { assistantId, fieldId } = pendingDeleteExtraField;
    const r = await request(
      "DELETE",
      `/admin/ai-assistants/${assistantId}/extra-fields/${fieldId}`,
      null,
      adminToken
    );
    handleResult(r);
    if (r.ok) {
      setPendingDeleteExtraField(null);
      await loadAdminExtraFields(assistantId);
    }
  }

  async function loadUsers() {
    const result = await request("GET", "/admin/users", null, adminToken);
    handleResult(result);
    if (!result.ok) return;
    const incomingUsers = Array.isArray(result.data) ? result.data : [];
    setUsers(incomingUsers);
    if (!adminManageUserId && incomingUsers[0]?.id) {
      setAdminManageUserId(incomingUsers[0].id);
    }
    if (!adminDialogUserId && incomingUsers[0]?.id) {
      setAdminDialogUserId(incomingUsers[0].id);
    }
  }

  async function loadMyProfile(token = accessToken) {
    if (!token) return;
    const result = await request("GET", "/users/me", null, token);
    handleResult(result);
    if (!result.ok) return;

    const profile = result.data || {};
    setProfileBirthDate(profile.birthDate || "");
    setProfileHeightCm(profile.heightCm === null || profile.heightCm === undefined ? "" : String(profile.heightCm));
    setProfileWeightKg(profile.weightKg === null || profile.weightKg === undefined ? "" : String(profile.weightKg));
    setProfilePhone(profile.phone || "");
    setProfileCity(profile.city || "");
    setProfileAbout(profile.about || "");
    setProfileFirstName(profile.firstName || "");
    setProfileLastName(profile.lastName || "");
    setProfileAiSummary(profile.aiSummary || "");
  }

  async function loadWorkoutSessions(token = accessToken) {
    if (!token) return;
    const result = await request("GET", "/users/me/workouts?includeHistory=false", null, token);
    handleResult(result);
    if (!result.ok) return;
    setWorkoutSessions(Array.isArray(result.data) ? result.data : []);
  }

  async function loadWorkoutHistory(token = accessToken) {
    if (!token) return;
    const query = new URLSearchParams();
    if (historyDateFrom) query.set("from", historyDateFrom);
    if (historyDateTo) query.set("to", historyDateTo);
    const suffix = query.toString() ? `?${query}` : "";
    const result = await request("GET", `/users/me/workouts/history${suffix}`, null, token);
    handleResult(result);
    if (!result.ok) return;
    const logs = Array.isArray(result.data) ? result.data : [];
    const sortedLogs = [...logs].sort((a, b) => {
      const timeA = Date.parse(a?.date || a?._date || "") || 0;
      const timeB = Date.parse(b?.date || b?._date || "") || 0;
      return timeB - timeA;
    });
    setHistoryWorkoutLogs(sortedLogs);
  }

  async function loadWorkoutExerciseCatalog(token = accessToken) {
    if (!token) return;
    const result = await request("GET", "/users/me/workouts/exercises", null, token);
    handleResult(result);
    if (!result.ok) return;

    const incoming = Array.isArray(result.data) ? result.data : [];
    const unique = [];
    const seen = new Set();
    for (const item of incoming) {
      const key = (item.name || "").trim().toLowerCase();
      if (!key || seen.has(key)) continue;
      seen.add(key);
      unique.push(item);
    }
    setWorkoutExerciseCatalog(unique);
    if (!selectedCatalogExerciseId && unique[0]?.id) {
      setSelectedCatalogExerciseId(unique[0].id);
    }
  }

  async function createCatalogExercise() {
    if (!accessToken) return;
    const name = (newCatalogExerciseName || "").trim();
    if (!name) return setErrorView("Укажи название упражнения.");

    const seedCode = `catalog::${slugifyProgramCode(name)}-${Date.now()}`;
    const result = await request(
      "POST",
      "/users/me/workouts",
      {
        sessionCode: seedCode,
        date: new Date().toISOString().slice(0, 10),
        day: `Каталог: ${name}`,
        notes: "Служебная запись для каталога упражнений",
        exercises: [
          {
            name,
            meta: (newCatalogExerciseMeta || "").trim(),
            sets: []
          }
        ]
      },
      accessToken
    );
    handleResult(result);
    if (!result.ok) return;

    setNewCatalogExerciseName("");
    setNewCatalogExerciseMeta("");
    setIsCreateExerciseModalOpen(false);
    await loadWorkoutExerciseCatalog(accessToken);
  }

  function openDeleteCatalogExerciseModal(exerciseId) {
    if (!exerciseId) return;
    setPendingDeleteCatalogExerciseId(exerciseId);
  }

  async function confirmDeleteCatalogExercise() {
    const exerciseId = pendingDeleteCatalogExerciseId;
    if (!accessToken || !exerciseId) return;

    const result = await request("DELETE", `/users/me/workouts/exercises/${exerciseId}`, null, accessToken);
    handleResult(result);
    if (!result.ok) return;

    if (String(selectedCatalogExerciseId) === String(exerciseId)) {
      setSelectedCatalogExerciseId("");
    }
    setPendingDeleteCatalogExerciseId(null);
    await loadWorkoutExerciseCatalog(accessToken);
  }

  function slugifyProgramCode(value) {
    const normalized = value.trim().toLowerCase().replace(/\s+/g, "-").replace(/[^a-z0-9а-яё_-]/gi, "");
    return normalized || `program-${Date.now()}`;
  }

  function parseProgramExerciseMeta(rawMeta) {
    const raw = String(rawMeta || "").trim();
    if (!raw) {
      return { isStructured: false, sourceExerciseId: "", comment: "", legacy: "" };
    }

    try {
      const parsed = JSON.parse(raw);
      if (parsed && typeof parsed === "object" && ("sourceExerciseId" in parsed || "comment" in parsed)) {
        return {
          isStructured: true,
          sourceExerciseId: parsed?.sourceExerciseId ? String(parsed.sourceExerciseId) : "",
          comment: parsed?.comment ? String(parsed.comment) : "",
          legacy: ""
        };
      }
    } catch {
      // keep legacy plain text meta
    }

    return { isStructured: false, sourceExerciseId: "", comment: "", legacy: raw };
  }

  function addExerciseToProgram(exercise) {
    setProgramExercisesDraft((prev) => [
      ...prev,
      {
        id: `pex-${Date.now()}-${Math.random()}`,
        sourceExerciseId: exercise.id || "",
        name: exercise.name || "",
        comment: ""
      }
    ]);
  }

  function removeProgramExercise(exerciseId) {
    setProgramExercisesDraft((prev) => prev.filter((x) => x.id !== exerciseId));
  }

  function updateProgramExerciseComment(exerciseId, comment) {
    setProgramExercisesDraft((prev) =>
      prev.map((x) =>
        x.id === exerciseId
          ? { ...x, comment }
          : x
      )
    );
  }

  async function saveProgramToDb() {
    if (!accessToken) return;
    const normalizedDay = programDay.trim();
    if (!normalizedDay) return setErrorView("Укажи название программы.");

    const code = `program::${slugifyProgramCode(programCode || normalizedDay)}`;
    const result = await request(
      "POST",
      "/users/me/workouts",
      {
        sessionCode: code,
        date: new Date().toISOString().slice(0, 10),
        day: normalizedDay,
        notes: programNotes.trim(),
        isActive: false,
        exercises: programExercisesDraft.map((x) => ({
          name: (x.name || "").trim(),
          meta: JSON.stringify({
            sourceExerciseId: x.sourceExerciseId || null,
            comment: (x.comment || "").trim()
          }),
          sets: []
        }))
      },
      accessToken
    );
    handleResult(result);
    if (!result.ok) return;
    await loadWorkoutSessions(accessToken);
    setEditingProgramId("");
    setProgramCode("");
    setProgramDay("");
    setProgramNotes("");
    setProgramExercisesDraft([]);
    setIsProgramModalOpen(false);
  }

  function openProgramCreateModal() {
    setEditingProgramId("");
    setProgramCode("");
    setProgramDay("");
    setProgramNotes("");
    setProgramExercisesDraft([]);
    setIsProgramModalOpen(true);
  }

  function openProgramEditModal(program) {
    setEditingProgramId(program.id || "");
    setProgramCode((program.sessionCode || "").replace(/^program::/, ""));
    setProgramDay(program.day || "");
    setProgramNotes(program.notes || "");
    setProgramExercisesDraft(
      (program.exercises || []).map((x, idx) => ({
        ...parseProgramExerciseMeta(x.meta),
        id: `edit-${idx}-${Date.now()}`,
        name: x.name || ""
      }))
    );
    setIsProgramModalOpen(true);
  }

  function closeProgramModal() {
    setIsProgramModalOpen(false);
    setEditingProgramId("");
  }

  function openProgramDeleteModal(program) {
    setPendingDeleteProgram(program);
    setIsProgramDeleteModalOpen(true);
  }

  async function deleteProgramFromModal() {
    if (!pendingDeleteProgram?.id || !accessToken) return;
    const result = await request("DELETE", `/users/me/workouts/${pendingDeleteProgram.id}`, null, accessToken);
    handleResult(result);
    if (!result.ok) return;

    if (selectedProgramCode && pendingDeleteProgram.sessionCode === selectedProgramCode) {
      setSelectedProgramCode("");
    }
    setPendingDeleteProgram(null);
    setIsProgramDeleteModalOpen(false);
    await loadWorkoutSessions(accessToken);
  }

  function formatWorkoutDateLabel(iso) {
    if (!iso) return "";
    const raw = String(iso).slice(0, 10);
    try {
      const d = new Date(`${raw}T12:00:00`);
      if (Number.isNaN(d.getTime())) return raw;
      return d.toLocaleDateString("ru-RU", { day: "numeric", month: "long", year: "numeric" });
    } catch {
      return raw;
    }
  }

  function downloadJsonFile(fileName, payload) {
    const content = JSON.stringify(payload, null, 2);
    const blob = new Blob([content], { type: "application/json;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  function mapServerSessionToCurrentWorkout(session) {
    const dateStr = session.date || session._date;
    return {
      sessionId: session.id,
      sessionCode: session.sessionCode,
      day: session.day || "",
      date: dateStr ? String(dateStr).slice(0, 10) : new Date().toISOString().slice(0, 10),
      notes: session.notes || "",
      isActive: session.isActive !== false,
      exercises: (session.exercises || []).map((x) => {
        const setsSource = x.sets && x.sets.length > 0 ? x.sets : [{ weight: "", reps: "", rpe: "8" }];
        return {
          id: String(x.id),
          name: x.name || "",
          meta: x.meta || "",
          sets: setsSource.map((s) => ({
            weight: s.weight ?? "",
            reps: s.reps ?? "",
            rpe: s.rpe || "8"
          }))
        };
      })
    };
  }

  function startWorkoutFromProgram(program) {
    setCurrentWorkout({
      sessionCode: `workout::${Date.now()}`,
      day: program.day || "Тренировка",
      date: new Date().toISOString().slice(0, 10),
      notes: "",
      isActive: true,
      exercises: (program.exercises || []).map((x, idx) => {
        const parsedMeta = parseProgramExerciseMeta(x.meta);
        return {
          id: `cw-${idx}-${Date.now()}`,
          name: x.name || "",
          meta: parsedMeta.comment || parsedMeta.legacy,
          sets: [{ weight: "", reps: "", rpe: "" }]
        };
      })
    });
    setIsActiveWorkoutModalOpen(true);
  }

  function createWorkoutFromScratch() {
    setCurrentWorkout({
      sessionCode: `workout::${Date.now()}`,
      day: "Новая тренировка",
      date: new Date().toISOString().slice(0, 10),
      notes: "",
      isActive: true,
      exercises: []
    });
    setIsActiveWorkoutModalOpen(true);
  }

  async function downloadPlanningJsonTemplate() {
    if (!accessToken) return;
    const result = await request("GET", "/users/me/workouts/planning/import-template", null, accessToken);
    handleResult(result);
    if (!result.ok) return;

    const templateJson = JSON.stringify(result.data || {}, null, 2);
    const blob = new Blob([templateJson], { type: "application/json;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "workout-planning-template.json";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  async function downloadExercisesJsonTemplate() {
    if (!accessToken) return;
    const result = await request("GET", "/users/me/workouts/exercises/import-template", null, accessToken);
    handleResult(result);
    if (!result.ok) return;

    const templateJson = JSON.stringify(result.data || {}, null, 2);
    const blob = new Blob([templateJson], { type: "application/json;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "workout-exercises-template.json";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  async function exportExercisesIdNameJson() {
    if (!accessToken) return;
    const result = await request("GET", "/users/me/workouts/exercises/export", null, accessToken);
    handleResult(result);
    if (!result.ok) return;

    const payload = {
      exercises: Array.isArray(result.data?.exercises) ? result.data.exercises : []
    };

    const content = JSON.stringify(payload, null, 2);
    const blob = new Blob([content], { type: "application/json;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = "workout-exercises-id-name.json";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  function openPlanningImportDialog() {
    planningImportInputRef.current?.click();
  }

  function openExercisesImportDialog() {
    exercisesImportInputRef.current?.click();
  }

  async function importWorkoutPlanningFromJson(rawText) {
    let parsed;
    try {
      parsed = JSON.parse(rawText);
    } catch {
      setErrorView("Некорректный JSON. Проверь файл и попробуй снова.");
      return;
    }

    const payload = Array.isArray(parsed)
      ? { programs: parsed }
      : (Array.isArray(parsed?.programs) ? parsed : null);
    if (!payload) {
      setErrorView("Ожидается JSON с массивом программ: { \"programs\": [...] }.");
      return;
    }

    const result = await request("POST", "/users/me/workouts/planning/import", payload, accessToken);
    handleResult(result);
    if (!result.ok) return;

    await loadWorkoutSessions(accessToken);
    setWorkoutsSubTab("manage");
    setWorkoutsManageSubTab("add");
  }

  async function importWorkoutExercisesFromJson(rawText) {
    let parsed;
    try {
      parsed = JSON.parse(rawText);
    } catch {
      setErrorView("Некорректный JSON. Проверь файл и попробуй снова.");
      return;
    }

    const payload = Array.isArray(parsed)
      ? { exercises: parsed }
      : (Array.isArray(parsed?.exercises) ? parsed : null);
    if (!payload) {
      setErrorView("Ожидается JSON с массивом упражнений: { \"exercises\": [...] }.");
      return;
    }

    const result = await request("POST", "/users/me/workouts/exercises/import", payload, accessToken);
    handleResult(result);
    if (!result.ok) return;

    await loadWorkoutExerciseCatalog(accessToken);
    setWorkoutsSubTab("manage");
    setWorkoutsManageSubTab("exercises");
  }

  async function handlePlanningJsonImportChange(event) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    const text = await file.text();
    await importWorkoutPlanningFromJson(text);
  }

  async function handleExercisesJsonImportChange(event) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    const text = await file.text();
    await importWorkoutExercisesFromJson(text);
  }

  function hideActiveWorkoutModal() {
    setIsActiveWorkoutModalOpen(false);
  }

  function openOrResumeWorkoutModal() {
    if (!currentWorkout) {
      const fromList = workoutSessions.find(
        (s) => String(s.sessionCode || "").startsWith("workout::") && s.isActive === true
      );
      if (fromList) {
        setCurrentWorkout(mapServerSessionToCurrentWorkout(fromList));
      }
    }
    setIsActiveWorkoutModalOpen(true);
  }

  function updateCurrentWorkoutField(field, value) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return { ...prev, [field]: value };
    });
  }

  function addExerciseToCurrentWorkout() {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: [
          ...prev.exercises,
          {
            id: `cw-manual-${Date.now()}-${Math.random()}`,
            name: "",
            meta: "",
            sets: [{ weight: "", reps: "", rpe: "" }]
          }
        ]
      };
    });
  }

  function updateCurrentWorkoutExercise(exerciseId, field, value) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) => (x.id === exerciseId ? { ...x, [field]: value } : x))
      };
    });
  }

  function removeCurrentWorkoutExercise(exerciseId) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.filter((x) => x.id !== exerciseId)
      };
    });
  }

  function openDeleteCurrentWorkoutExerciseModal(exerciseId) {
    if (!exerciseId) return;
    setPendingDeleteCurrentWorkoutExerciseId(exerciseId);
  }

  function confirmDeleteCurrentWorkoutExercise() {
    if (!pendingDeleteCurrentWorkoutExerciseId) return;
    removeCurrentWorkoutExercise(pendingDeleteCurrentWorkoutExerciseId);
    setPendingDeleteCurrentWorkoutExerciseId(null);
  }

  function updateCurrentWorkoutSet(exerciseId, setIndex, field, value) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) =>
          x.id === exerciseId
            ? { ...x, sets: x.sets.map((s, idx) => (idx === setIndex ? { ...s, [field]: value } : s)) }
            : x
        )
      };
    });
  }

  function adjustCurrentWorkoutSetWeight(exerciseId, setIndex, delta) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) => {
          if (x.id !== exerciseId) return x;
          return {
            ...x,
            sets: x.sets.map((s, idx) => {
              if (idx !== setIndex) return s;
              const currentWeightRaw = String(s.weight ?? "").replace(",", ".").trim();
              const currentWeight = Number(currentWeightRaw);
              const base = Number.isNaN(currentWeight) ? 0 : currentWeight;
              const nextValue = Math.max(0, Math.round((base + delta) * 100) / 100);
              return { ...s, weight: String(nextValue) };
            })
          };
        })
      };
    });
  }

  function adjustCurrentWorkoutSetReps(exerciseId, setIndex, delta) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) => {
          if (x.id !== exerciseId) return x;
          return {
            ...x,
            sets: x.sets.map((s, idx) => {
              if (idx !== setIndex) return s;
              const currentRepsRaw = String(s.reps ?? "").trim();
              const currentReps = Number(currentRepsRaw);
              const base = Number.isNaN(currentReps) ? 0 : currentReps;
              const nextValue = Math.max(0, Math.round(base + delta));
              return { ...s, reps: String(nextValue) };
            })
          };
        })
      };
    });
  }

  function adjustCurrentWorkoutSetRpe(exerciseId, setIndex, delta) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) => {
          if (x.id !== exerciseId) return x;
          return {
            ...x,
            sets: x.sets.map((s, idx) => {
              if (idx !== setIndex) return s;
              const currentRpe = Number(String(s.rpe ?? "8").trim());
              const base = Number.isNaN(currentRpe) ? 8 : currentRpe;
              const nextValue = Math.min(10, Math.max(6, base + delta));
              return { ...s, rpe: String(nextValue) };
            })
          };
        })
      };
    });
  }

  function addCurrentWorkoutSet(exerciseId) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) =>
          x.id === exerciseId
            ? {
              ...x,
              sets: [
                ...x.sets,
                {
                  weight: x.sets[x.sets.length - 1]?.weight ?? "",
                  reps: x.sets[x.sets.length - 1]?.reps ?? "",
                  rpe: ""
                }
              ]
            }
            : x
        )
      };
    });
  }

  function removeCurrentWorkoutSet(exerciseId, setIndex) {
    setCurrentWorkout((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        exercises: prev.exercises.map((x) =>
          x.id === exerciseId
            ? { ...x, sets: x.sets.filter((_, idx) => idx !== setIndex) }
            : x
        )
      };
    });
  }

  async function persistCurrentWorkout(finish) {
    if (!accessToken || !currentWorkout) return;
    if (!(currentWorkout.day || "").trim()) return setErrorView("Укажи название тренировки.");
    const result = await request(
      "POST",
      "/users/me/workouts",
      {
        sessionCode: currentWorkout.sessionCode,
        date: (currentWorkout.date || new Date().toISOString().slice(0, 10)).slice(0, 10),
        day: currentWorkout.day.trim(),
        notes: currentWorkout.notes || "",
        isActive: !finish,
        exercises: currentWorkout.exercises.map((x) => ({
          name: (x.name || "").trim(),
          meta: x.meta,
          sets: x.sets.map((s) => ({ weight: s.weight, reps: s.reps, rpe: s.rpe }))
        })).filter((x) => x.name)
      },
      accessToken
    );
    handleResult(result);
    if (!result.ok) return;
    if (result.data) {
      setCurrentWorkout(mapServerSessionToCurrentWorkout(result.data));
    }
    if (finish) {
      setCurrentWorkout(null);
    }
    setIsActiveWorkoutModalOpen(false);
    await loadWorkoutSessions(accessToken);
  }

  function openDeleteWorkoutLogModal(sessionId) {
    if (!sessionId) return;
    setPendingDeleteWorkoutSessionId(sessionId);
  }

  function openWorkoutHistoryModal(session) {
    if (!session) return;
    setSelectedWorkoutHistorySession(session);
  }

  function closeWorkoutHistoryModal() {
    setSelectedWorkoutHistorySession(null);
  }

  function applyHistoryDatePreset(days) {
    if (!days || days < 1) return;
    const end = new Date();
    end.setHours(0, 0, 0, 0);
    const start = new Date(end);
    start.setDate(start.getDate() - (days - 1));
    setHistoryDateFrom(start.toISOString().slice(0, 10));
    setHistoryDateTo(end.toISOString().slice(0, 10));
  }

  function exportHistorySessionJson(session) {
    if (!session) return;
    const datePart = String(session.date || session._date || "").slice(0, 10) || "unknown-date";
    const idPart = String(session.id || "workout");
    downloadJsonFile(`workout-history-${datePart}-${idPart}.json`, session);
  }

  function exportHistorySelectionJson() {
    const fromPart = historyDateFrom || "all";
    const toPart = historyDateTo || "all";
    downloadJsonFile(`workout-history-${fromPart}-to-${toPart}.json`, {
      exportedAt: new Date().toISOString(),
      filters: {
        from: historyDateFrom || null,
        to: historyDateTo || null
      },
      total: historyWorkoutLogs.length,
      workouts: historyWorkoutLogs
    });
  }

  async function confirmDeleteWorkoutSession() {
    const sessionId = pendingDeleteWorkoutSessionId;
    if (!accessToken) return;
    if (!sessionId) return;

    const result = await request("DELETE", `/users/me/workouts/${sessionId}`, null, accessToken);
    handleResult(result);
    if (!result.ok) return;

    setPendingDeleteWorkoutSessionId(null);
    if (selectedWorkoutHistorySession?.id === sessionId) {
      setSelectedWorkoutHistorySession(null);
    }
    if (selectedProgramCode && workoutPrograms.some((x) => x.id === sessionId && x.sessionCode === selectedProgramCode)) {
      setSelectedProgramCode("");
    }
    await loadWorkoutSessions(accessToken);
    await loadWorkoutHistory(accessToken);
  }

  async function saveMyProfile() {
    if (!accessToken) return;

    const toNumberOrNull = (value) => {
      const raw = value.trim();
      if (!raw) return null;
      const parsed = Number(raw);
      return Number.isNaN(parsed) ? null : parsed;
    };

    const body = {
      birthDate: profileBirthDate || null,
      heightCm: toNumberOrNull(profileHeightCm),
      weightKg: toNumberOrNull(profileWeightKg),
      phone: profilePhone.trim() || null,
      city: profileCity.trim() || null,
      about: profileAbout.trim() || null,
      firstName: profileFirstName.trim() || null,
      lastName: profileLastName.trim() || null
    };

    const result = await request("PUT", "/users/me", body, accessToken);
    handleResult(result);
    if (result.ok) {
      setIsProfileEditModalOpen(false);
      await loadMyProfile(accessToken);
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

  async function submitCreateAdminDialog() {
    if (!adminDialogUserId) return setErrorView("Выбери пользователя для нового диалога.");
    const title = (adminDialogTitleDraft || "").trim() || "Новый диалог";
    const result = await request("POST", "/admin/dialogs", { userId: adminDialogUserId, title }, adminToken);
    handleResult(result);
    if (result.ok) {
      setAdminDialogModalKind(null);
      setAdminDialogTitleDraft("");
      await loadAdminDialogs(adminDialogUserId, result.data?.id);
    }
  }

  async function submitRenameAdminDialog() {
    if (!adminCurrentDialogId) return setErrorView("Сначала выбери диалог.");
    const title = (adminDialogTitleDraft || "").trim();
    if (!title) return setErrorView("Введи название.");
    const result = await request("PUT", `/admin/dialogs/${adminCurrentDialogId}`, { title }, adminToken);
    handleResult(result);
    if (result.ok) {
      setAdminDialogModalKind(null);
      setAdminDialogTitleDraft("");
      await loadAdminDialogs(adminDialogUserId, adminCurrentDialogId);
    }
  }

  async function submitDeleteAdminDialog() {
    if (!adminCurrentDialogId) return setErrorView("Сначала выбери диалог.");
    const deletingId = adminCurrentDialogId;
    const result = await request("DELETE", `/admin/dialogs/${deletingId}`, null, adminToken);
    handleResult(result);
    if (result.ok) {
      setAdminDialogModalKind(null);
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
    () => dialogs.map((d) => <option key={d.id} value={d.id}>{d.title || "Диалог"}</option>),
    [dialogs]
  );
  const adminUserOptions = useMemo(
    () => users.map((u) => <option key={u.id} value={u.id}>{u.username} ({u.role})</option>),
    [users]
  );
  const adminDialogOptions = useMemo(
    () => adminDialogs.map((d) => <option key={d.id} value={d.id}>{d.title || "Диалог"} ({d.username || "неизвестно"})</option>),
    [adminDialogs]
  );
  const adminManageSelectedUser = useMemo(
    () => users.find((u) => u.id === adminManageUserId) || null,
    [users, adminManageUserId]
  );
  const workoutPrograms = useMemo(
    () => workoutSessions.filter((x) => String(x.sessionCode || "").startsWith("program::")),
    [workoutSessions]
  );
  const activeWorkoutSession = useMemo(
    () => workoutSessions.find((x) => String(x.sessionCode || "").startsWith("workout::") && x.isActive === true),
    [workoutSessions]
  );
  useEffect(() => {
    if (tab !== "workouts" || workoutsSubTab !== "my-workout") return;
    const active = workoutSessions.find(
      (s) => String(s.sessionCode || "").startsWith("workout::") && s.isActive === true
    );
    if (active && !currentWorkout) {
      setCurrentWorkout(mapServerSessionToCurrentWorkout(active));
    }
  }, [tab, workoutsSubTab, workoutSessions, currentWorkout]);
  useEffect(() => {
    if (tab !== "workouts" || !accessToken) return;
    loadWorkoutSessions();
    loadWorkoutExerciseCatalog();
  }, [tab, accessToken]);
  useEffect(() => {
    if (tab !== "workouts" || workoutsSubTab !== "history") return;
    loadWorkoutHistory();
  }, [tab, workoutsSubTab]);
  useEffect(() => {
    if (tab !== "workouts" || workoutsSubTab !== "history") return;
    loadWorkoutHistory();
  }, [tab, workoutsSubTab, historyDateFrom, historyDateTo]);
  const selectedProgram = useMemo(
    () => workoutPrograms.find((x) => x.sessionCode === selectedProgramCode) || null,
    [workoutPrograms, selectedProgramCode]
  );
  const selectedCatalogExercise = useMemo(
    () => workoutExerciseCatalog.find((x) => String(x.id) === String(selectedCatalogExerciseId)) || null,
    [workoutExerciseCatalog, selectedCatalogExerciseId]
  );
  const historyMaxSetCount = useMemo(
    () => Math.max(
      0,
      ...(selectedWorkoutHistorySession?.exercises || []).map((exercise) => (exercise.sets || []).length)
    ),
    [selectedWorkoutHistorySession]
  );

  const previewAssistantRow = useMemo(() => {
    if (!chatAssistantPreviewId) return null;
    return (
      aiAssistants.find((x) => String(x.id) === String(chatAssistantPreviewId))
      ?? adminAiAssistants.find((x) => String(x.id) === String(chatAssistantPreviewId))
      ?? null
    );
  }, [chatAssistantPreviewId, aiAssistants, adminAiAssistants]);

  const isLoggedIn = Boolean(accessToken);
  const isAdmin = currentUserRole === "Admin";
  const hasAiAccess = currentUserRole === "Admin" || currentUserRole === "AiUser";

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
              <label>Логин</label>
              <input
                value={username}
                disabled={isLoginLoading}
                onChange={(e) => {
                  setUsername(e.target.value);
                  if (loginError) setLoginError("");
                }}
              />
              <label>Пароль</label>
              <input
                value={password}
                disabled={isLoginLoading}
                onChange={(e) => {
                  setPassword(e.target.value);
                  if (loginError) setLoginError("");
                }}
                type="password"
              />
              <button
                onClick={() => loginAndStore(navigate)}
                disabled={isLoginLoading}
                aria-busy={isLoginLoading}
              >
                {isLoginLoading ? (
                  <span className="btn-loading">
                    <span className="spinner" aria-hidden="true" />
                    Входим...
                  </span>
                ) : "Войти"}
              </button>
              {loginError && <p className="auth-error">{loginError}</p>}
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
              <label>Логин</label>
              <input
                value={registerUsername}
                onChange={(e) => {
                  setRegisterUsername(e.target.value);
                  if (registerError) setRegisterError("");
                }}
              />
              <label>Пароль</label>
              <input
                value={registerPassword}
                onChange={(e) => {
                  setRegisterPassword(e.target.value);
                  if (registerError) setRegisterError("");
                }}
                type="password"
              />
              <button
                onClick={async () => {
                  setRegisterError("");
                  const result = await request("POST", "/auth/register", { username: registerUsername.trim(), password: registerPassword });
                  handleResult(result);
                  if (result.ok) {
                    navigate("/login");
                    return;
                  }

                  const message = result.data?.message
                    || (result.status === 409 ? "Пользователь с таким логином уже существует." : "")
                    || (result.status === 400 ? "Проверь логин и пароль: данные не прошли валидацию." : "")
                    || (result.status === 0 ? "Сервер недоступен. Проверь подключение и адрес API." : "")
                    || "Не удалось зарегистрироваться. Попробуй еще раз.";
                  setRegisterError(message);
                }}
              >
                Зарегистрироваться
              </button>
              {registerError && <p className="auth-error">{registerError}</p>}
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
                currentUserName={currentUserName || username || "неизвестно"}
                isAdmin={isAdmin}
                hasAiAccess={hasAiAccess}
                onTabChange={(id) => {
                  if (id === "admin" && !isAdmin) return;
                  if (id === "ai" && !hasAiAccess) return;
                  setTab(id);
                  if (id === "profile") {
                    loadMyProfile();
                  }
                  if (id === "workouts") {
                    loadWorkoutSessions();
                    loadWorkoutExerciseCatalog();
                  }
                  if (id === "ai") {
                    loadAiAssistants(aiToken);
                  }
                  if (id === "admin") {
                    loadUsers();
                    loadAdminDialogs();
                    loadAdminAiAssistants();
                    if (hasAiAccess) loadAiAssistants(aiToken);
                  }
                }}
                onLogout={() => {
                  setAccessToken("");
                  setAdminToken("");
                  setAiToken("");
                  setCurrentUserName("");
                  setCurrentUserRole("");
                  setProfileBirthDate("");
                  setProfileHeightCm("");
                  setProfileWeightKg("");
                  setProfilePhone("");
                  setProfileCity("");
                  setProfileAbout("");
                  setProfileFirstName("");
                  setProfileLastName("");
                  setProfileAiSummary("");
                  navigate("/login");
                }}
              />

        {tab === "ai" && hasAiAccess && <section className="card-grid">
          <section className="card full-span">
            <h3>ИИ помощники</h3>
            {aiAssistants.length === 0 && (
              <p className="subtitle">Пока нет активных помощников. Администратор может добавить их во вкладке «Админ».</p>
            )}
            {aiAssistants.length > 0 && (
              <ul className="ai-assistant-list" style={{ listStyle: "none", padding: 0, margin: 0 }}>
                {aiAssistants.map((a) => (
                  <li
                    key={a.id}
                    style={{
                      display: "flex",
                      alignItems: "flex-start",
                      justifyContent: "space-between",
                      gap: "12px",
                      padding: "10px 0",
                      borderBottom: "1px solid var(--border-subtle, rgba(255,255,255,0.08))"
                    }}
                  >
                    <div>
                      <strong>{a.name}</strong>
                      {a.description ? <div className="subtitle" style={{ marginTop: 4 }}>{a.description}</div> : null}
                    </div>
                    <div style={{ display: "flex", flexDirection: "column", gap: 8, alignItems: "stretch", flexShrink: 0 }}>
                      <button
                        type="button"
                        className={a.selected ? "" : "ghost-btn"}
                        onClick={() => toggleAiAssistantRow(a.id, a.selected)}
                      >
                        {a.selected ? "Выключить" : "Включить"}
                      </button>
                      <button
                        type="button"
                        className="ghost-btn"
                        onClick={() => openAiExtraInfoModal(a.id)}
                        title="Заполнить дополнительные поля для этого помощника"
                      >
                        Доп. поля
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            )}

            <div
              className="ai-assistant-chat-panel"
              style={{ marginTop: 24, paddingTop: 20, borderTop: "1px solid var(--border-subtle, rgba(255,255,255,0.12))" }}
            >
              <h4 style={{ marginTop: 0 }}>Чат с помощником</h4>
              <p className="subtitle">
                {aiAssistants.some((x) => x.selected) ? (
                  <>
                    Сейчас ответы строятся с помощником «
                    <strong>{aiAssistants.find((x) => x.selected)?.name}</strong>
                    »: его системный промпт и заполненные для него доп. поля передаются в модель вместе с историей диалога.
                  </>
                ) : (
                  <>
                    Помощник не включён — модель видит только историю этого диалога без дополнительного системного промпта. Включи помощника кнопкой «Включить» выше.
                  </>
                )}
              </p>
              <p className="subtitle">Можно создать отдельный диалог или писать без выбранного диалога — тогда создастся новый.</p>
              <div className="row">
                <select value={currentDialogId} onChange={(e) => { setCurrentDialogId(e.target.value); loadDialogMessages(e.target.value); }}>
                  <option value="">Нет диалогов</option>
                  {dialogOptions}
                </select>
                <button
                  type="button"
                  onClick={() => {
                    setAiDialogTitleDraft("");
                    setAiDialogModalKind("new");
                  }}
                >
                  + Новый
                </button>
                <button
                  type="button"
                  onClick={() => {
                    if (!currentDialogId) return;
                    const current = dialogs.find((d) => d.id === currentDialogId);
                    setAiDialogTitleDraft(current?.title || "Новый диалог");
                    setAiDialogModalKind("rename");
                  }}
                  title="Переименовать"
                >
                  ✏️
                </button>
                <button
                  type="button"
                  className="danger-btn"
                  onClick={() => {
                    if (!currentDialogId) return;
                    setAiDialogModalKind("delete");
                  }}
                  title="Удалить"
                >
                  🗑️
                </button>
              </div>
              <div className="chat-messages">
                {chatMessages.map((m, i) => <div key={i} className={`chat-msg ${m.role === "user" ? "user" : "assistant"}`}>{m.content}</div>)}
              </div>
              <div className="row">
                <input value={chatPrompt} onChange={(e) => setChatPrompt(e.target.value)} placeholder="Введите сообщение..." />
                <button type="button" onClick={sendChat}>Отправить</button>
              </div>
            </div>
          </section>
        </section>}

        {tab === "workouts" && <section className="card-grid">
          <section className="card full-span">
            <h3>Тренировки</h3>
            <p className="subtitle">Управляй программами, своими упражнениями и фактическими тренировками.</p>
            <div className="workouts-subtabs">
              <button className={workoutsSubTab === "my-workout" ? "top-nav-tab active" : "top-nav-tab"} onClick={() => setWorkoutsSubTab("my-workout")}>Моя тренировка</button>
              <button className={workoutsSubTab === "history" ? "top-nav-tab active" : "top-nav-tab"} onClick={() => setWorkoutsSubTab("history")}>История</button>
              <button className={workoutsSubTab === "manage" ? "top-nav-tab active" : "top-nav-tab"} onClick={() => setWorkoutsSubTab("manage")}>Управление тренировкой</button>
            </div>

            {workoutsSubTab === "manage" && (
              <>
                <div className="workouts-subtabs">
                  <button
                    className={workoutsManageSubTab === "add" ? "top-nav-tab active" : "top-nav-tab"}
                    onClick={() => setWorkoutsManageSubTab("add")}
                  >
                    Программы
                  </button>
                  <button
                    className={workoutsManageSubTab === "exercises" ? "top-nav-tab active" : "top-nav-tab"}
                    onClick={() => setWorkoutsManageSubTab("exercises")}
                  >
                    Упражнения
                  </button>
                </div>
              </>
            )}

            {workoutsSubTab === "manage" && workoutsManageSubTab === "add" && (
              <>
                <div className="row">
                  <button onClick={openProgramCreateModal}>Новая программа</button>
                  <button className="ghost-btn" onClick={openPlanningImportDialog}>Импорт планирования JSON</button>
                  <button className="ghost-btn" onClick={downloadPlanningJsonTemplate}>Скачать шаблон планирования</button>
                </div>
                <input
                  ref={planningImportInputRef}
                  type="file"
                  accept=".json,application/json"
                  onChange={handlePlanningJsonImportChange}
                  style={{ display: "none" }}
                />

                <h4>Сохраненные программы</h4>
                <div className="workout-list">
                  {workoutPrograms.length === 0 && <div className="workout-empty">Пока нет программ.</div>}
                  {workoutPrograms.map((session) => (
                    <article key={session.id} className="workout-item">
                      <h4>{session.day}</h4>
                      <div className="workout-meta">
                        <span>Код: {session.sessionCode}</span>
                        <span>Упражнений: {session.exercises.length}</span>
                      </div>
                      <div className="row">
                        <button onClick={() => openProgramEditModal(session)} title="Редактировать">✏️</button>
                        <button className="ghost-btn" onClick={() => { setSelectedProgramCode(session.sessionCode); setWorkoutsSubTab("my-workout"); }}>Начать тренировку</button>
                        <button className="danger-btn" onClick={() => openProgramDeleteModal(session)} title="Удалить">🗑️</button>
                      </div>
                    </article>
                  ))}
                </div>
              </>
            )}

            {workoutsSubTab === "manage" && workoutsManageSubTab === "exercises" && (
              <>
                <p className="subtitle">Создай новое упражнение и используй его в программах и тренировках.</p>
                <div className="row">
                  <button onClick={() => setIsCreateExerciseModalOpen(true)}>Создать упражнение</button>
                  <button className="ghost-btn" onClick={openExercisesImportDialog}>Загрузить упражнения JSON</button>
                  <button className="ghost-btn" onClick={downloadExercisesJsonTemplate}>Скачать шаблон упражнений</button>
                  <button className="ghost-btn" onClick={exportExercisesIdNameJson}>Выгрузить ID + название</button>
                </div>
                <input
                  ref={exercisesImportInputRef}
                  type="file"
                  accept=".json,application/json"
                  onChange={handleExercisesJsonImportChange}
                  style={{ display: "none" }}
                />
                <div className="users-table-wrap">
                  <table className="users-table">
                    <thead>
                      <tr>
                        <th>Упражнение</th>
                        <th>Комментарий</th>
                        <th>Действия</th>
                      </tr>
                    </thead>
                    <tbody>
                      {workoutExerciseCatalog.length === 0 && (
                        <tr>
                          <td colSpan="3">Пока нет упражнений в базе.</td>
                        </tr>
                      )}
                      {workoutExerciseCatalog.map((exercise) => {
                        const parsedMeta = parseProgramExerciseMeta(exercise.meta);
                        return (
                          <tr key={exercise.id}>
                            <td>{exercise.name}</td>
                            <td>{!parsedMeta.isStructured ? parsedMeta.legacy || "-" : "-"}</td>
                            <td>
                              <button className="danger-btn" onClick={() => openDeleteCatalogExerciseModal(exercise.id)} title="Удалить">🗑️</button>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </>
            )}

            {workoutsSubTab === "my-workout" && (
              <>
                <label>Выбери программу</label>
                <select value={selectedProgramCode} onChange={(e) => setSelectedProgramCode(e.target.value)}>
                  <option value="">Нет выбранной программы</option>
                  {workoutPrograms.map((program) => (
                    <option key={program.id} value={program.sessionCode}>{program.day}</option>
                  ))}
                </select>
                <div className="row">
                  <button disabled={!selectedProgram} onClick={() => selectedProgram && startWorkoutFromProgram(selectedProgram)}>Начать тренировку</button>
                  <button className="ghost-btn" onClick={createWorkoutFromScratch}>Создать с нуля</button>
                </div>
                <p className="subtitle">Заполнение и сохранение тренировки открывается в окне поверх страницы. Завершенные тренировки смотри во вкладке «История тренировок».</p>
                {(activeWorkoutSession || currentWorkout) && (
                  <div className="active-workout-banner">
                    <div>
                      <span className="active-workout-badge">Активная</span>
                      <span className="active-workout-title">{currentWorkout?.day || activeWorkoutSession?.day || "Тренировка"}</span>
                      {(currentWorkout?.date || activeWorkoutSession?.date || activeWorkoutSession?._date) && (
                        <span className="active-workout-date">
                          {formatWorkoutDateLabel(currentWorkout?.date || activeWorkoutSession?.date || activeWorkoutSession?._date)}
                        </span>
                      )}
                    </div>
                    <button type="button" onClick={openOrResumeWorkoutModal}>Открыть тренировку</button>
                  </div>
                )}
              </>
            )}
            {workoutsSubTab === "history" && (
              <>
                <h4>История моих тренировок</h4>
                <div className="row">
                  <input
                    type="date"
                    value={historyDateFrom}
                    onChange={(e) => setHistoryDateFrom(e.target.value)}
                  />
                  <input
                    type="date"
                    value={historyDateTo}
                    onChange={(e) => setHistoryDateTo(e.target.value)}
                  />
                </div>
                <div className="row">
                  <button
                    type="button"
                    className="ghost-btn"
                    onClick={exportHistorySelectionJson}
                    disabled={historyWorkoutLogs.length === 0}
                  >
                    Выгрузить JSON (вся выборка)
                  </button>
                </div>
                <div className="history-presets">
                  <button type="button" className="ghost-btn" onClick={() => applyHistoryDatePreset(1)}>День</button>
                  <button type="button" className="ghost-btn" onClick={() => applyHistoryDatePreset(7)}>Неделя</button>
                  <button type="button" className="ghost-btn" onClick={() => applyHistoryDatePreset(10)}>10 дней</button>
                  <button type="button" className="ghost-btn" onClick={() => applyHistoryDatePreset(15)}>15 дней</button>
                  <button type="button" className="ghost-btn" onClick={() => applyHistoryDatePreset(30)}>Месяц</button>
                </div>
                <div className="workout-list">
                  {historyWorkoutLogs.length === 0 && <div className="workout-empty">Нет тренировок по выбранному фильтру.</div>}
                  {historyWorkoutLogs.map((session) => (
                    <article key={session.id} className="workout-item">
                      <h4>{session.day}</h4>
                      <div className="workout-meta">
                        <span>Дата: {formatWorkoutDateLabel(session.date || session._date) || "-"}</span>
                        <span>Упражнений: {session.exercises.length}</span>
                      </div>
                      <div className="row">
                        <button className="ghost-btn" onClick={() => openWorkoutHistoryModal(session)}>Подробнее</button>
                        <button className="ghost-btn" onClick={() => exportHistorySessionJson(session)}>JSON</button>
                        <button className="danger-btn" onClick={() => openDeleteWorkoutLogModal(session.id)} title="Удалить">🗑️</button>
                      </div>
                    </article>
                  ))}
                </div>
              </>
            )}
          </section>
        </section>}

        {tab === "profile" && <section className="card-grid">
          <section className="card full-span">
            <h3>Мой профиль</h3>
            <p className="subtitle">Все поля необязательные. Изменения вносятся в отдельном окне.</p>
            <div className="profile-readonly">
              <div className="profile-readonly-grid">
                <div className="profile-readonly-item">
                  <span className="profile-field-label">Имя</span>
                  <div className="profile-field-value">{profileFirstName || "—"}</div>
                </div>
                <div className="profile-readonly-item">
                  <span className="profile-field-label">Фамилия</span>
                  <div className="profile-field-value">{profileLastName || "—"}</div>
                </div>
                <div className="profile-readonly-item">
                  <span className="profile-field-label">Дата рождения</span>
                  <div className="profile-field-value">{profileBirthDate || "—"}</div>
                </div>
                <div className="profile-readonly-item">
                  <span className="profile-field-label">Рост (см)</span>
                  <div className="profile-field-value">{profileHeightCm || "—"}</div>
                </div>
                <div className="profile-readonly-item">
                  <span className="profile-field-label">Вес (кг)</span>
                  <div className="profile-field-value">{profileWeightKg || "—"}</div>
                </div>
                <div className="profile-readonly-item">
                  <span className="profile-field-label">Телефон</span>
                  <div className="profile-field-value">{profilePhone || "—"}</div>
                </div>
                <div className="profile-readonly-item">
                  <span className="profile-field-label">Город</span>
                  <div className="profile-field-value">{profileCity || "—"}</div>
                </div>
              </div>
              <div className="profile-readonly-item">
                <span className="profile-field-label">О себе</span>
                <div className="profile-field-value">{profileAbout || "—"}</div>
              </div>
              <div className="profile-readonly-item profile-ai-summary-block">
                <span className="profile-field-label">Саммари от ИИ</span>
                <div className="profile-field-value profile-ai-summary-text">{profileAiSummary || "Появится после ответов ассистента в чате."}</div>
              </div>
            </div>
            <div className="row">
              <button onClick={() => setIsProfileEditModalOpen(true)} title="Редактировать профиль">✏️</button>
              <button className="ghost-btn" onClick={() => loadMyProfile()}>Обновить с сервера</button>
            </div>
          </section>
        </section>}

        {tab === "admin" && isAdmin && <section className="card-grid">
          <section className="card full-span">
            <h3>Управление пользователем</h3>
            <div className="row">
              <button onClick={() => setIsCreateUserModalOpen(true)}>Новый пользователь</button>
            </div>
            <div className="users-table-wrap">
              <table className="users-table">
                <thead>
                  <tr>
                    <th>Логин</th>
                    <th>Роль</th>
                    <th>Действия</th>
                  </tr>
                </thead>
                <tbody>
                  {users.length === 0 && <tr><td colSpan="3">Пользователи не загружены</td></tr>}
                  {users.map((u) => (
                    <tr key={`manage-${u.id}`}>
                      <td>{u.username}</td>
                      <td>{u.role}</td>
                      <td className="admin-actions">
                        <button onClick={() => openEditModal(u)} title="Редактировать">✏️</button>
                        <button onClick={() => openPasswordModal(u)}>Пароль</button>
                        <button className="danger-btn" onClick={() => openDeleteModal(u)} title="Удалить">🗑️</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className="card full-span">
            <h3>Пользователи</h3>
            <div className="users-table-wrap">
              <table className="users-table">
                <thead>
                  <tr>
                    <th>Логин</th>
                    <th>Роль</th>
                    <th>Имя</th>
                    <th>Фамилия</th>
                    <th>Дата рождения</th>
                    <th>Рост (см)</th>
                    <th>Вес (кг)</th>
                    <th>Телефон</th>
                    <th>Город</th>
                    <th>О себе</th>
                    <th>AI-саммари</th>
                    <th>Создан</th>
                    <th>ID пользователя</th>
                  </tr>
                </thead>
                <tbody>
                  {users.length === 0 && <tr><td colSpan="13">Пользователи не загружены</td></tr>}
                  {users.map((u) => (
                    <tr key={u.id}>
                      <td>{u.username}</td>
                      <td>{u.role}</td>
                      <td>{u.firstName || "-"}</td>
                      <td>{u.lastName || "-"}</td>
                      <td>{u.birthDate || "-"}</td>
                      <td>{u.heightCm ?? "-"}</td>
                      <td>{u.weightKg ?? "-"}</td>
                      <td>{u.phone || "-"}</td>
                      <td>{u.city || "-"}</td>
                      <td>{u.about || "-"}</td>
                      <td className="admin-ai-summary-cell">{u.aiSummary ? `${String(u.aiSummary).slice(0, 120)}${String(u.aiSummary).length > 120 ? "…" : ""}` : "-"}</td>
                      <td>{u.createdAtUtc ? new Date(u.createdAtUtc).toLocaleString() : "-"}</td>
                      <td>{u.id}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="subtitle">Режим только чтение: редактирование данных пользователя для администратора отключено.</p>
          </section>

          <section className="card full-span">
            <h3>ИИ помощники</h3>
            <div className="row">
              <button type="button" onClick={openAssistantCreateModal}>Новый помощник</button>
              <button type="button" className="ghost-btn" onClick={() => loadAdminAiAssistants()}>Обновить</button>
            </div>
            <div className="users-table-wrap">
              <table className="users-table">
                <thead>
                  <tr>
                    <th>Название</th>
                    <th>Порядок</th>
                    <th>Активен</th>
                    <th>Промпт</th>
                    <th>Действия</th>
                  </tr>
                </thead>
                <tbody>
                  {adminAiAssistants.length === 0 && (
                    <tr><td colSpan="5">Нет записей</td></tr>
                  )}
                  {adminAiAssistants.map((row) => (
                    <tr key={row.id}>
                      <td>{row.name}</td>
                      <td>{row.sortOrder}</td>
                      <td>{row.isActive ? "Да" : "Нет"}</td>
                      <td className="admin-ai-summary-cell">{row.systemPrompt ? `${String(row.systemPrompt).slice(0, 80)}${String(row.systemPrompt).length > 80 ? "…" : ""}` : "—"}</td>
                      <td className="admin-actions">
                        <button type="button" onClick={() => openAssistantEditModal(row)} title="Редактировать">✏️</button>
                        <button
                          type="button"
                          className={String(chatAssistantPreviewId) === String(row.id) ? "" : "ghost-btn"}
                          disabled={!row.isActive}
                          title={
                            row.isActive
                              ? "Открыть пробный чат с этим помощником (без смены выбора на вкладке ИИ)"
                              : "Сначала отметьте помощника как активный"
                          }
                          onClick={() => {
                            if (!row.isActive) return;
                            setChatAssistantPreviewId(row.id);
                            requestAnimationFrame(() =>
                              adminAssistantTestChatPanelRef.current?.scrollIntoView({ behavior: "smooth", block: "nearest" })
                            );
                          }}
                        >
                          Тест чата
                        </button>
                        <button type="button" className="danger-btn" onClick={() => setPendingDeleteAssistantId(row.id)} title="Удалить">🗑️</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <section className="card full-span" ref={adminAssistantTestChatPanelRef}>
            <h3>Пробный чат с помощником</h3>
            <p className="subtitle">
              Проверка ответов под промптом помощника для твоего аккаунта. Не меняет пользовательский выбор «Включить» на вкладке ИИ. Для запроса API нужен{" "}
              <strong>активный</strong> помощник — кнопка «Тест чата» в таблице выше неактивна, пока в карточке помощника не стоит «Активен».
            </p>
            <p className="subtitle">
              {chatAssistantPreviewId && !previewAssistantRow ? (
                <>
                  Ссылка на помощника устарела.
                  <button type="button" className="ghost-btn" style={{ marginLeft: 8 }} onClick={() => setChatAssistantPreviewId(null)}>
                    Сбросить
                  </button>
                </>
              ) : previewAssistantRow ? (
                <>
                  <strong>Пробный режим:</strong> сообщения уходят с помощником «<strong>{previewAssistantRow.name}</strong>
                  ». Доп. поля подставляются как на вкладке ИИ для этого помощника.
                  <button type="button" className="ghost-btn" style={{ marginLeft: 8 }} onClick={() => setChatAssistantPreviewId(null)}>
                    Выйти из пробного режима
                  </button>
                </>
              ) : (
                <>Выбери помощника кнопкой «Тест чата» в таблице или пиши на вкладке «ИИ» с включённым помощником — там же заполняются доп. поля.</>
              )}
            </p>
            <p className="subtitle">Тот же список диалогов, что и на вкладке ИИ; можно создать отдельный диалог только для проверок.</p>
            <div className="row">
              <select value={currentDialogId} onChange={(e) => { setCurrentDialogId(e.target.value); loadDialogMessages(e.target.value); }}>
                <option value="">Нет диалогов</option>
                {dialogOptions}
              </select>
              <button
                type="button"
                onClick={() => {
                  setAiDialogTitleDraft("");
                  setAiDialogModalKind("new");
                }}
              >
                + Новый
              </button>
              <button
                type="button"
                onClick={() => {
                  if (!currentDialogId) return;
                  const current = dialogs.find((d) => d.id === currentDialogId);
                  setAiDialogTitleDraft(current?.title || "Новый диалог");
                  setAiDialogModalKind("rename");
                }}
                title="Переименовать"
              >
                ✏️
              </button>
              <button
                type="button"
                className="danger-btn"
                onClick={() => {
                  if (!currentDialogId) return;
                  setAiDialogModalKind("delete");
                }}
                title="Удалить"
              >
                🗑️
              </button>
            </div>
            <div className="chat-messages">
              {chatMessages.map((m, i) => <div key={i} className={`chat-msg ${m.role === "user" ? "user" : "assistant"}`}>{m.content}</div>)}
            </div>
            <div className="row">
              <input value={chatPrompt} onChange={(e) => setChatPrompt(e.target.value)} placeholder="Сообщение для проверки…" />
              <button type="button" onClick={sendChat}>Отправить</button>
            </div>
          </section>

          <section className="card full-span">
            <h3>Диалоги (Админ)</h3>
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
                <option value="">Все пользователи</option>
                {adminUserOptions}
              </select>
            </div>

            <label>Диалог</label>
            <div className="row">
              <select value={adminCurrentDialogId} onChange={(e) => { setAdminCurrentDialogId(e.target.value); loadAdminDialogMessages(e.target.value); }}>
                <option value="">Нет диалогов</option>
                {adminDialogOptions}
              </select>
              <button
                onClick={() => {
                  setAdminDialogTitleDraft("");
                  setAdminDialogModalKind("create");
                }}
              >
                Новый диалог
              </button>
              <button
                onClick={() => {
                  if (!adminCurrentDialogId) return;
                  const current = adminDialogs.find((d) => d.id === adminCurrentDialogId);
                  setAdminDialogTitleDraft(current?.title || "Новый диалог");
                  setAdminDialogModalKind("rename");
                }}
                title="Переименовать"
              >
                ✏️
              </button>
              <button
                className="danger-btn"
                onClick={() => {
                  if (!adminCurrentDialogId) return;
                  setAdminDialogModalKind("delete");
                }}
                title="Удалить"
              >
                🗑️
              </button>
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

        {tab === "admin" && isAdmin && isCreateUserModalOpen && (
          <ModalShell open={isCreateUserModalOpen} onClose={() => setIsCreateUserModalOpen(false)}>
              <h3>Создать пользователя</h3>
              <label>Логин</label>
              <input
                value={adminCreateUsername}
                onChange={(e) => setAdminCreateUsername(e.target.value)}
                placeholder="логин"
              />
              <label>Пароль</label>
              <input
                value={adminCreatePassword}
                onChange={(e) => setAdminCreatePassword(e.target.value)}
                placeholder="пароль"
                type="password"
              />
              <label>Роль</label>
              <select value={adminCreateRole} onChange={(e) => setAdminCreateRole(e.target.value)}>
                <option value="User">Пользователь</option><option value="AiUser">AI-пользователь</option><option value="Admin">Администратор</option>
              </select>
              <div className="row">
                <button onClick={createAdminUser}>Создать</button>
                <button className="ghost-btn" onClick={() => setIsCreateUserModalOpen(false)}>Отмена</button>
              </div>
          </ModalShell>
        )}
        {tab === "admin" && isAdmin && isEditUserModalOpen && selectedAdminUser && (
          <ModalShell open={isEditUserModalOpen} onClose={() => setIsEditUserModalOpen(false)}>
              <h3>Редактировать пользователя</h3>
              <label>Логин</label>
              <input value={editUserName} onChange={(e) => setEditUserName(e.target.value)} />
              <label>Роль</label>
              <select value={editUserRole} onChange={(e) => setEditUserRole(e.target.value)}>
                <option value="User">Пользователь</option><option value="AiUser">AI-пользователь</option><option value="Admin">Администратор</option>
              </select>
              <div className="row">
                <button onClick={saveAdminUserFromModal}>Сохранить</button>
                <button className="ghost-btn" onClick={() => setIsEditUserModalOpen(false)}>Отмена</button>
              </div>
          </ModalShell>
        )}
        {tab === "admin" && isAdmin && isPasswordModalOpen && selectedAdminUser && (
          <ModalShell open={isPasswordModalOpen} onClose={() => setIsPasswordModalOpen(false)}>
              <h3>Сменить пароль</h3>
              <p className="subtitle">{selectedAdminUser.username}</p>
              <label>Новый пароль</label>
              <input
                value={newPasswordValue}
                onChange={(e) => setNewPasswordValue(e.target.value)}
                type="password"
                placeholder="новый пароль"
              />
              <div className="row">
                <button onClick={saveAdminPasswordFromModal}>Сохранить пароль</button>
                <button className="ghost-btn" onClick={() => setIsPasswordModalOpen(false)}>Отмена</button>
              </div>
          </ModalShell>
        )}
        {tab === "admin" && isAdmin && isDeleteModalOpen && selectedAdminUser && (
          <ModalShell open={isDeleteModalOpen} onClose={() => setIsDeleteModalOpen(false)}>
              <h3>Удалить пользователя</h3>
              <p className="subtitle">Ты точно хочешь удалить: <b>{selectedAdminUser.username}</b>?</p>
              <div className="row">
                <button className="danger-btn" onClick={deleteAdminUserFromModal} title="Удалить">🗑️</button>
                <button className="ghost-btn" onClick={() => setIsDeleteModalOpen(false)}>Отмена</button>
              </div>
          </ModalShell>
        )}
        {tab === "admin" && isAdmin && (assistantModalKind === "create" || assistantModalKind === "edit") && (
          <ModalShell
            open={Boolean(assistantModalKind)}
            onClose={() => {
              setAssistantModalKind(null);
              setAdminExtraFieldsList([]);
            }}
            wide
            scroll
          >
            <h3>{assistantModalKind === "create" ? "Новый ИИ помощник" : "ИИ помощник"}</h3>
            <p className="subtitle">Системный промпт, настройки и дополнительные поля для пользователей — в одном окне.</p>
            <label>Название</label>
            <input
              value={assistantDraft.name}
              onChange={(e) => setAssistantDraft((d) => ({ ...d, name: e.target.value }))}
              placeholder="Краткое имя"
            />
            <label>Описание (для панели пользователя)</label>
            <input
              value={assistantDraft.description}
              onChange={(e) => setAssistantDraft((d) => ({ ...d, description: e.target.value }))}
              placeholder="Необязательно"
            />
            <label>Системный промпт</label>
            <textarea
              value={assistantDraft.systemPrompt}
              onChange={(e) => setAssistantDraft((d) => ({ ...d, systemPrompt: e.target.value }))}
              rows={8}
              placeholder="Инструкции для модели"
            />
            <label>Настройки (JSON, необязательно)</label>
            <textarea
              value={assistantDraft.settingsJson}
              onChange={(e) => setAssistantDraft((d) => ({ ...d, settingsJson: e.target.value }))}
              rows={3}
              placeholder='Например: {"temperature": 0.7}'
            />
            <label>Порядок сортировки</label>
            <input
              type="number"
              value={assistantDraft.sortOrder}
              onChange={(e) => setAssistantDraft((d) => ({ ...d, sortOrder: Number(e.target.value) }))}
            />
            <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <input
                type="checkbox"
                checked={assistantDraft.isActive}
                onChange={(e) => setAssistantDraft((d) => ({ ...d, isActive: e.target.checked }))}
              />
              Активен (доступен пользователям)
            </label>

            <div style={{ marginTop: 20, paddingTop: 16, borderTop: "1px solid var(--border-subtle, rgba(255,255,255,0.12))" }}>
              <h4 style={{ marginTop: 0 }}>Дополнительные поля</h4>
              <p className="subtitle">
                Ключ — латиница, цифры и подчёркивание. Тип: текст, многострочный текст или число. Поля заполняют пользователи во вкладке ИИ.
              </p>
              {!assistantDraft.id ? (
                <p className="subtitle">Нажми «Сохранить помощника» ниже — после создания записи здесь можно добавить поля.</p>
              ) : (
                <>
                  <div className="row">
                    <button
                      type="button"
                      onClick={openAdminExtraFieldCreate}
                    >
                      Новое поле
                    </button>
                    <button
                      type="button"
                      className="ghost-btn"
                      onClick={() => loadAdminExtraFields(assistantDraft.id)}
                    >
                      Обновить список полей
                    </button>
                  </div>
                  <div className="users-table-wrap">
                    <table className="users-table">
                      <thead>
                        <tr>
                          <th>Ключ</th>
                          <th>Подпись</th>
                          <th>Тип</th>
                          <th>Порядок</th>
                          <th>Обяз.</th>
                          <th />
                        </tr>
                      </thead>
                      <tbody>
                        {adminExtraFieldsList.length === 0 && (
                          <tr><td colSpan="6">Нет полей</td></tr>
                        )}
                        {adminExtraFieldsList.map((f) => (
                          <tr key={f.id}>
                            <td>{f.fieldKey}</td>
                            <td>{f.label}</td>
                            <td>{f.fieldType}</td>
                            <td>{f.sortOrder}</td>
                            <td>{f.isRequired ? "Да" : "Нет"}</td>
                            <td className="admin-actions">
                              <button type="button" onClick={() => openAdminExtraFieldEdit(f)} title="Редактировать">✏️</button>
                              <button
                                type="button"
                                className="danger-btn"
                                onClick={() =>
                                  setPendingDeleteExtraField({ assistantId: assistantDraft.id, fieldId: f.id })
                                }
                                title="Удалить"
                              >
                                🗑️
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </>
              )}
            </div>

            <div className="row">
              <button type="button" onClick={submitAssistantModal}>Сохранить помощника</button>
              <button
                type="button"
                className="ghost-btn"
                onClick={() => {
                  setAssistantModalKind(null);
                  setAdminExtraFieldsList([]);
                }}
              >
                Закрыть
              </button>
            </div>
          </ModalShell>
        )}
        {tab === "admin" && isAdmin && pendingDeleteAssistantId && (
          <ModalShell open={Boolean(pendingDeleteAssistantId)} onClose={() => setPendingDeleteAssistantId(null)}>
            <h3>Удалить помощника</h3>
            <p className="subtitle">Удалить эту запись? У пользователей с выбранным помощником выбор будет сброшен.</p>
            <div className="row">
              <button type="button" className="danger-btn" onClick={confirmDeleteAssistant} title="Удалить">🗑️</button>
              <button type="button" className="ghost-btn" onClick={() => setPendingDeleteAssistantId(null)}>Отмена</button>
            </div>
          </ModalShell>
        )}
        {tab === "admin" && isAdmin && (adminExtraFieldModalKind === "create" || adminExtraFieldModalKind === "edit") && (
          <ModalShell open={Boolean(adminExtraFieldModalKind)} onClose={() => setAdminExtraFieldModalKind(null)}>
            <h3>{adminExtraFieldModalKind === "create" ? "Новое поле" : "Редактировать поле"}</h3>
            <label>Ключ поля</label>
            <input
              value={adminExtraFieldDraft.fieldKey}
              onChange={(e) => setAdminExtraFieldDraft((d) => ({ ...d, fieldKey: e.target.value }))}
              placeholder="например training_goal"
              disabled={adminExtraFieldModalKind === "edit"}
            />
            {adminExtraFieldModalKind === "edit" && (
              <p className="subtitle">Ключ после создания не меняется.</p>
            )}
            <label>Подпись для пользователя</label>
            <input
              value={adminExtraFieldDraft.label}
              onChange={(e) => setAdminExtraFieldDraft((d) => ({ ...d, label: e.target.value }))}
              placeholder="Отображаемое название"
            />
            <label>Тип</label>
            <select
              value={adminExtraFieldDraft.fieldType}
              onChange={(e) => setAdminExtraFieldDraft((d) => ({ ...d, fieldType: e.target.value }))}
            >
              <option value="text">Текст</option>
              <option value="textarea">Многострочный текст</option>
              <option value="number">Число</option>
            </select>
            <label>Порядок</label>
            <input
              type="number"
              value={adminExtraFieldDraft.sortOrder}
              onChange={(e) => setAdminExtraFieldDraft((d) => ({ ...d, sortOrder: Number(e.target.value) }))}
            />
            <label style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <input
                type="checkbox"
                checked={adminExtraFieldDraft.isRequired}
                onChange={(e) => setAdminExtraFieldDraft((d) => ({ ...d, isRequired: e.target.checked }))}
              />
              Обязательное поле
            </label>
            <div className="row">
              <button type="button" onClick={submitAdminExtraFieldModal}>Сохранить</button>
              <button type="button" className="ghost-btn" onClick={() => setAdminExtraFieldModalKind(null)}>
                Отмена
              </button>
            </div>
          </ModalShell>
        )}
        {tab === "admin" && isAdmin && pendingDeleteExtraField && (
          <ModalShell open={Boolean(pendingDeleteExtraField)} onClose={() => setPendingDeleteExtraField(null)}>
            <h3>Удалить поле</h3>
            <p className="subtitle">Значения пользователей для этого ключа перестанут использоваться.</p>
            <div className="row">
              <button type="button" className="danger-btn" onClick={confirmDeleteExtraField} title="Удалить">
                🗑️
              </button>
              <button type="button" className="ghost-btn" onClick={() => setPendingDeleteExtraField(null)}>
                Отмена
              </button>
            </div>
          </ModalShell>
        )}

        {tab === "workouts" && workoutsSubTab === "manage" && workoutsManageSubTab === "add" && isProgramModalOpen && (
          <div className="modal-backdrop">
            <div className="modal-card modal-card--wide modal-card--scroll">
              <h3>{editingProgramId ? "Редактирование программы" : "Новая программа"}</h3>
              <label>Код программы (латиницей, опционально)</label>
              <input value={programCode} onChange={(e) => setProgramCode(e.target.value)} placeholder="например, upper-body-a" />
              <label>Название программы</label>
              <input value={programDay} onChange={(e) => setProgramDay(e.target.value)} placeholder="Верх тела A" />
              <label>Заметки</label>
              <textarea value={programNotes} onChange={(e) => setProgramNotes(e.target.value)} rows={3} placeholder="Комментарий к программе" />
              <div className="row">
                <select value={selectedCatalogExerciseId} onChange={(e) => setSelectedCatalogExerciseId(e.target.value)}>
                  <option value="">Выбери упражнение из общего списка</option>
                  {workoutExerciseCatalog.map((exercise) => (
                    <option key={exercise.id} value={exercise.id}>{exercise.name}</option>
                  ))}
                </select>
                <button type="button" onClick={() => selectedCatalogExercise && addExerciseToProgram(selectedCatalogExercise)} disabled={!selectedCatalogExercise}>Добавить в программу</button>
              </div>
              <h4>Упражнения в программе</h4>
              <div className="users-table-wrap program-draft-table-wrap">
                <table className="users-table program-draft-table">
                  <thead>
                    <tr>
                      <th>Упражнение</th>
                      <th>Комментарий</th>
                      <th>Действия</th>
                    </tr>
                  </thead>
                  <tbody>
                    {programExercisesDraft.length === 0 && (
                      <tr>
                        <td colSpan="3" className="program-draft-empty">Нет строк. Выбери упражнение выше и нажми «Добавить в программу» — появится новая строка.</td>
                      </tr>
                    )}
                    {programExercisesDraft.map((exercise) => (
                      <tr key={exercise.id}>
                        <td className="program-draft-name">{exercise.name}</td>
                        <td>
                          <textarea
                            className="program-draft-comment"
                            value={exercise.comment || ""}
                            onChange={(e) => updateProgramExerciseComment(exercise.id, e.target.value)}
                            rows={2}
                            placeholder="Например: техника, темп, акцент"
                          />
                        </td>
                        <td>
                          <button type="button" className="danger-btn" onClick={() => removeProgramExercise(exercise.id)} title="Удалить">🗑️</button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="row">
                <button onClick={saveProgramToDb}>Сохранить программу</button>
                <button className="ghost-btn" onClick={closeProgramModal}>Отмена</button>
              </div>
            </div>
          </div>
        )}

        {tab === "workouts" && workoutsSubTab === "manage" && workoutsManageSubTab === "add" && isProgramDeleteModalOpen && pendingDeleteProgram && (
          <div className="modal-backdrop">
            <div className="modal-card">
              <h3>Удалить программу</h3>
              <p className="subtitle">Ты точно хочешь удалить программу: <b>{pendingDeleteProgram.day || pendingDeleteProgram.sessionCode}</b>?</p>
              <div className="row">
                <button className="danger-btn" onClick={deleteProgramFromModal} title="Удалить">🗑️</button>
                <button
                  className="ghost-btn"
                  onClick={() => {
                    setIsProgramDeleteModalOpen(false);
                    setPendingDeleteProgram(null);
                  }}
                >
                  Отмена
                </button>
              </div>
            </div>
          </div>
        )}
        {tab === "profile" && isProfileEditModalOpen && (
          <ModalShell open={isProfileEditModalOpen} onClose={() => setIsProfileEditModalOpen(false)} scroll titleId="profile-edit-title">
              <h3 id="profile-edit-title">Редактировать профиль</h3>
              <label>Имя</label>
              <input value={profileFirstName} onChange={(e) => setProfileFirstName(e.target.value)} placeholder="Имя" />
              <label>Фамилия</label>
              <input value={profileLastName} onChange={(e) => setProfileLastName(e.target.value)} placeholder="Фамилия" />
              <div className="row profile-row">
                <div>
                  <label>Дата рождения</label>
                  <input type="date" value={profileBirthDate} onChange={(e) => setProfileBirthDate(e.target.value)} />
                </div>
                <div>
                  <label>Рост (см)</label>
                  <input
                    type="number"
                    value={profileHeightCm}
                    onChange={(e) => setProfileHeightCm(e.target.value)}
                    placeholder="например, 175"
                    min="0"
                    max="300"
                    step="0.01"
                  />
                </div>
                <div>
                  <label>Вес (кг)</label>
                  <input
                    type="number"
                    value={profileWeightKg}
                    onChange={(e) => setProfileWeightKg(e.target.value)}
                    placeholder="например, 72.5"
                    min="0"
                    max="700"
                    step="0.01"
                  />
                </div>
              </div>
              <label>Телефон</label>
              <input value={profilePhone} onChange={(e) => setProfilePhone(e.target.value)} placeholder="+7..." />
              <label>Город</label>
              <input value={profileCity} onChange={(e) => setProfileCity(e.target.value)} placeholder="Алматы" />
              <label>О себе</label>
              <textarea
                value={profileAbout}
                onChange={(e) => setProfileAbout(e.target.value)}
                placeholder="Любая полезная информация"
                rows={4}
              />
              <div className="row">
                <button onClick={saveMyProfile}>Сохранить</button>
                <button className="ghost-btn" onClick={() => setIsProfileEditModalOpen(false)}>Отмена</button>
              </div>
          </ModalShell>
        )}

        {(tab === "ai" || tab === "admin") && hasAiAccess && aiDialogModalKind === "new" && (
          <ModalShell
            open={aiDialogModalKind === "new"}
            onClose={() => { setAiDialogModalKind(null); setAiDialogTitleDraft(""); }}
          >
              <h3>Новый диалог</h3>
              <label>Название</label>
              <input
                value={aiDialogTitleDraft}
                onChange={(e) => setAiDialogTitleDraft(e.target.value)}
                placeholder="Новый диалог"
              />
              <div className="row">
                <button onClick={submitAiNewDialog}>Создать</button>
                <button className="ghost-btn" onClick={() => { setAiDialogModalKind(null); setAiDialogTitleDraft(""); }}>Отмена</button>
              </div>
          </ModalShell>
        )}
        {(tab === "ai" || tab === "admin") && hasAiAccess && aiDialogModalKind === "rename" && (
          <ModalShell
            open={aiDialogModalKind === "rename"}
            onClose={() => { setAiDialogModalKind(null); setAiDialogTitleDraft(""); }}
          >
              <h3>Переименовать диалог</h3>
              <label>Название</label>
              <input
                value={aiDialogTitleDraft}
                onChange={(e) => setAiDialogTitleDraft(e.target.value)}
                placeholder="Новый диалог"
              />
              <div className="row">
                <button onClick={submitAiRenameDialog}>Сохранить</button>
                <button className="ghost-btn" onClick={() => { setAiDialogModalKind(null); setAiDialogTitleDraft(""); }}>Отмена</button>
              </div>
          </ModalShell>
        )}
        {(tab === "ai" || tab === "admin") && hasAiAccess && aiDialogModalKind === "delete" && (
          <ModalShell open={aiDialogModalKind === "delete"} onClose={() => setAiDialogModalKind(null)}>
              <h3>Удалить диалог</h3>
              <p className="subtitle">Удалить текущий выбранный диалог без восстановления?</p>
              <div className="row">
                <button className="danger-btn" onClick={submitAiDeleteDialog} title="Удалить">🗑️</button>
                <button className="ghost-btn" onClick={() => setAiDialogModalKind(null)}>Отмена</button>
              </div>
          </ModalShell>
        )}
        {hasAiAccess && isAiExtraInfoModalOpen && (
          <ModalShell open={isAiExtraInfoModalOpen} onClose={() => setIsAiExtraInfoModalOpen(false)} wide scroll>
            <h3>Дополнительная информация</h3>
            <p className="subtitle">
              Помощник:{" "}
              <strong>{aiAssistants.find((x) => String(x.id) === String(aiExtraInfoAssistantId))?.name || "—"}</strong>
              . Поля задаёт администратор для каждого помощника отдельно; при чате с выбранным помощником они попадают в контекст модели.
            </p>
            {aiExtraInfoDefinitions.length === 0 ? (
              <p className="subtitle">Для этого помощника пока нет дополнительных полей.</p>
            ) : (
              aiExtraInfoDefinitions.map((def) => (
                <div key={def.id || def.fieldKey}>
                  <label>
                    {def.label}
                    {def.isRequired ? " *" : ""}
                    <span className="subtitle" style={{ marginLeft: 6 }}>({def.fieldKey})</span>
                  </label>
                  {def.fieldType === "textarea" ? (
                    <textarea
                      rows={4}
                      value={aiExtraInfoValues[def.fieldKey] ?? ""}
                      onChange={(e) =>
                        setAiExtraInfoValues((prev) => ({ ...prev, [def.fieldKey]: e.target.value }))
                      }
                    />
                  ) : def.fieldType === "number" ? (
                    <input
                      type="number"
                      value={aiExtraInfoValues[def.fieldKey] ?? ""}
                      onChange={(e) =>
                        setAiExtraInfoValues((prev) => ({ ...prev, [def.fieldKey]: e.target.value }))
                      }
                    />
                  ) : (
                    <input
                      type="text"
                      value={aiExtraInfoValues[def.fieldKey] ?? ""}
                      onChange={(e) =>
                        setAiExtraInfoValues((prev) => ({ ...prev, [def.fieldKey]: e.target.value }))
                      }
                    />
                  )}
                </div>
              ))
            )}
            <div className="row">
              <button type="button" onClick={submitAiExtraInfoModal} disabled={aiExtraInfoDefinitions.length === 0}>
                Сохранить
              </button>
              <button type="button" className="ghost-btn" onClick={() => setIsAiExtraInfoModalOpen(false)}>
                Отмена
              </button>
            </div>
          </ModalShell>
        )}

        {tab === "workouts" && workoutsSubTab === "my-workout" && isActiveWorkoutModalOpen && currentWorkout && (
          <ModalShell open={isActiveWorkoutModalOpen} onClose={hideActiveWorkoutModal} wide scroll>
              <h3>Активная тренировка: {currentWorkout.day}</h3>
              <p className="subtitle">
                Дата: {formatWorkoutDateLabel(currentWorkout.date) || "—"} · можно сохранять черновик и править упражнения до завершения.
              </p>
              <label>Название тренировки</label>
              <input
                value={currentWorkout.day || ""}
                onChange={(e) => updateCurrentWorkoutField("day", e.target.value)}
                placeholder="Например: День ног"
              />
              <label>Дата тренировки</label>
              <input
                type="date"
                value={currentWorkout.date || ""}
                onChange={(e) => updateCurrentWorkoutField("date", e.target.value)}
              />
              <label>Заметки</label>
              <textarea
                value={currentWorkout.notes || ""}
                onChange={(e) => updateCurrentWorkoutField("notes", e.target.value)}
                rows={2}
                placeholder="Опционально"
              />
              <div className="row">
                <button className="ghost-btn" onClick={addExerciseToCurrentWorkout}>Добавить упражнение</button>
              </div>
              <div className="users-table-wrap">
                <table className="users-table">
                  <thead>
                    <tr>
                      <th>Упражнение</th>
                      <th>Комментарий</th>
                    </tr>
                  </thead>
                  <tbody>
                    {currentWorkout.exercises.length === 0 && (
                      <tr>
                        <td colSpan="2">Нет упражнений. Добавь первое упражнение.</td>
                      </tr>
                    )}
                    {currentWorkout.exercises.map((exercise) => ([
                        <tr key={`${exercise.id}-head`}>
                          <td colSpan="2">
                            <b>{exercise.name || "—"}</b>
                            {" · "}
                            <span>{exercise.meta || "—"}</span>
                          </td>
                        </tr>,
                        <tr key={`${exercise.id}-sets`}>
                          <td colSpan="2">
                            <div className="workout-sets">
                              {exercise.sets.map((setItem, setIdx) => (
                                <div key={`${exercise.id}-active-set-${setIdx}`} className="row workout-set-row">
                                  <div className="workout-set-weight-control">
                                    <input
                                      type="number"
                                      inputMode="decimal"
                                      min="0"
                                      step="0.5"
                                      value={setItem.weight}
                                      onChange={(e) => updateCurrentWorkoutSet(exercise.id, setIdx, "weight", e.target.value)}
                                      placeholder="Вес"
                                    />
                                    <div className="workout-set-weight-steps">
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetWeight(exercise.id, setIdx, -5)}>-5</button>
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetWeight(exercise.id, setIdx, -2.5)}>-2,5</button>
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetWeight(exercise.id, setIdx, 2.5)}>+2,5</button>
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetWeight(exercise.id, setIdx, 5)}>+5</button>
                                    </div>
                                  </div>
                                  <div className="workout-set-weight-control">
                                    <input
                                      type="number"
                                      inputMode="numeric"
                                      min="0"
                                      step="1"
                                      value={setItem.reps}
                                      onChange={(e) => updateCurrentWorkoutSet(exercise.id, setIdx, "reps", e.target.value)}
                                      placeholder="Повт."
                                    />
                                    <div className="workout-set-weight-steps">
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetReps(exercise.id, setIdx, -2)}>-2</button>
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetReps(exercise.id, setIdx, -1)}>-1</button>
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetReps(exercise.id, setIdx, 1)}>+1</button>
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetReps(exercise.id, setIdx, 2)}>+2</button>
                                    </div>
                                  </div>
                                  <div className="workout-set-weight-control">
                                    <select
                                      value={setItem.rpe || "8"}
                                      onChange={(e) => updateCurrentWorkoutSet(exercise.id, setIdx, "rpe", e.target.value)}
                                    >
                                      <option value="6">6</option>
                                      <option value="7">7</option>
                                      <option value="8">8</option>
                                      <option value="9">9</option>
                                      <option value="10">10</option>
                                    </select>
                                    <div className="workout-set-weight-steps">
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetRpe(exercise.id, setIdx, -1)}>-</button>
                                      <button type="button" className="ghost-btn" onClick={() => adjustCurrentWorkoutSetRpe(exercise.id, setIdx, 1)}>+</button>
                                    </div>
                                  </div>
                                  <button
                                    className="danger-btn"
                                    onClick={() => removeCurrentWorkoutSet(exercise.id, setIdx)}
                                    disabled={exercise.sets.length <= 1}
                                  >
                                    🗑️
                                  </button>
                                </div>
                              ))}
                            </div>
                            <div className="row">
                              <button className="ghost-btn" onClick={() => addCurrentWorkoutSet(exercise.id)}>+</button>
                              <button className="danger-btn" onClick={() => openDeleteCurrentWorkoutExerciseModal(exercise.id)} title="Удалить">✕</button>
                            </div>
                          </td>
                        </tr>
                      ]))}
                  </tbody>
                </table>
              </div>
              <div className="row">
                <button type="button" onClick={() => persistCurrentWorkout(false)}>Сохранить черновик</button>
                <button type="button" className="ghost-btn" onClick={() => persistCurrentWorkout(true)}>Завершить тренировку</button>
                <button type="button" className="ghost-btn" onClick={hideActiveWorkoutModal}>Закрыть</button>
              </div>
          </ModalShell>
        )}
        {tab === "workouts" && workoutsSubTab === "history" && selectedWorkoutHistorySession && (
          <ModalShell open={Boolean(selectedWorkoutHistorySession)} onClose={closeWorkoutHistoryModal} wide scroll>
              <h3>{selectedWorkoutHistorySession.day || "Тренировка"}</h3>
              <p className="subtitle">
                Дата: {formatWorkoutDateLabel(selectedWorkoutHistorySession.date || selectedWorkoutHistorySession._date) || "—"}
              </p>
              <p className="subtitle">
                Заметки: {selectedWorkoutHistorySession.notes || "—"}
              </p>
              <div className="users-table-wrap">
                <table className="users-table">
                  <thead>
                    <tr>
                      <th>Упражнение</th>
                      {Array.from({ length: historyMaxSetCount }, (_, idx) => (
                        <th key={`history-set-col-${idx}`}>{`Подход ${idx + 1}`}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {(selectedWorkoutHistorySession.exercises || []).length === 0 && (
                      <tr>
                        <td colSpan={historyMaxSetCount + 1}>В этой тренировке нет упражнений.</td>
                      </tr>
                    )}
                    {(selectedWorkoutHistorySession.exercises || []).map((exercise) => (
                      <tr key={exercise.id}>
                        <td>{exercise.name || "-"}</td>
                        {Array.from({ length: historyMaxSetCount }, (_, idx) => {
                          const setItem = (exercise.sets || [])[idx];
                          return (
                            <td key={`${exercise.id}-set-${idx}`}>
                              {setItem
                                ? `${setItem.weight || "-"} кг × ${setItem.reps || "-"} (RPE ${setItem.rpe || "-"})`
                                : "—"}
                            </td>
                          );
                        })}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="row">
                <button className="ghost-btn" onClick={closeWorkoutHistoryModal}>Закрыть</button>
              </div>
          </ModalShell>
        )}

        {tab === "admin" && isAdmin && adminDialogModalKind === "create" && (
          <ModalShell
            open={adminDialogModalKind === "create"}
            onClose={() => { setAdminDialogModalKind(null); setAdminDialogTitleDraft(""); }}
          >
              <h3>Новый диалог</h3>
              <label>Название</label>
              <input
                value={adminDialogTitleDraft}
                onChange={(e) => setAdminDialogTitleDraft(e.target.value)}
                placeholder="Новый диалог"
              />
              <div className="row">
                <button onClick={submitCreateAdminDialog}>Создать</button>
                <button className="ghost-btn" onClick={() => { setAdminDialogModalKind(null); setAdminDialogTitleDraft(""); }}>Отмена</button>
              </div>
          </ModalShell>
        )}
        {tab === "admin" && isAdmin && adminDialogModalKind === "rename" && (
          <ModalShell
            open={adminDialogModalKind === "rename"}
            onClose={() => { setAdminDialogModalKind(null); setAdminDialogTitleDraft(""); }}
          >
              <h3>Переименовать диалог</h3>
              <label>Название</label>
              <input
                value={adminDialogTitleDraft}
                onChange={(e) => setAdminDialogTitleDraft(e.target.value)}
                placeholder="Новый диалог"
              />
              <div className="row">
                <button onClick={submitRenameAdminDialog}>Сохранить</button>
                <button className="ghost-btn" onClick={() => { setAdminDialogModalKind(null); setAdminDialogTitleDraft(""); }}>Отмена</button>
              </div>
          </ModalShell>
        )}
        {tab === "admin" && isAdmin && adminDialogModalKind === "delete" && (
          <ModalShell open={adminDialogModalKind === "delete"} onClose={() => setAdminDialogModalKind(null)}>
              <h3>Удалить диалог</h3>
              <p className="subtitle">Удалить выбранный диалог без восстановления?</p>
              <div className="row">
                <button className="danger-btn" onClick={submitDeleteAdminDialog} title="Удалить">🗑️</button>
                <button className="ghost-btn" onClick={() => setAdminDialogModalKind(null)}>Отмена</button>
              </div>
          </ModalShell>
        )}

        {tab === "workouts" && workoutsSubTab === "manage" && workoutsManageSubTab === "exercises" && pendingDeleteCatalogExerciseId && (
          <ModalShell open={Boolean(pendingDeleteCatalogExerciseId)} onClose={() => setPendingDeleteCatalogExerciseId(null)}>
              <h3>Удалить упражнение</h3>
              <p className="subtitle">
                Удалить «{workoutExerciseCatalog.find((x) => String(x.id) === String(pendingDeleteCatalogExerciseId))?.name || "упражнение"}» из каталога?
              </p>
              <div className="row">
                <button className="danger-btn" onClick={confirmDeleteCatalogExercise} title="Удалить">🗑️</button>
                <button className="ghost-btn" onClick={() => setPendingDeleteCatalogExerciseId(null)}>Отмена</button>
              </div>
          </ModalShell>
        )}

        {tab === "workouts" && workoutsSubTab === "history" && pendingDeleteWorkoutSessionId && (
          <ModalShell open={Boolean(pendingDeleteWorkoutSessionId)} onClose={() => setPendingDeleteWorkoutSessionId(null)}>
              <h3>Удалить тренировку</h3>
              <p className="subtitle">
                Удалить «{historyWorkoutLogs.find((x) => x.id === pendingDeleteWorkoutSessionId)?.day || "тренировку"}» из истории?
              </p>
              <div className="row">
                <button className="danger-btn" onClick={confirmDeleteWorkoutSession} title="Удалить">🗑️</button>
                <button className="ghost-btn" onClick={() => setPendingDeleteWorkoutSessionId(null)}>Отмена</button>
              </div>
          </ModalShell>
        )}

        {tab === "workouts" && workoutsSubTab === "my-workout" && pendingDeleteCurrentWorkoutExerciseId && currentWorkout && (
          <ModalShell open={Boolean(pendingDeleteCurrentWorkoutExerciseId)} onClose={() => setPendingDeleteCurrentWorkoutExerciseId(null)}>
              <h3>Удалить упражнение</h3>
              <p className="subtitle">
                Удалить «{currentWorkout.exercises.find((x) => x.id === pendingDeleteCurrentWorkoutExerciseId)?.name || "упражнение"}» из текущей тренировки?
              </p>
              <div className="row">
                <button className="danger-btn" onClick={confirmDeleteCurrentWorkoutExercise} title="Удалить">🗑️ Удалить</button>
                <button className="ghost-btn" onClick={() => setPendingDeleteCurrentWorkoutExerciseId(null)}>Отмена</button>
              </div>
          </ModalShell>
        )}

        {tab === "workouts" && workoutsSubTab === "manage" && workoutsManageSubTab === "exercises" && isCreateExerciseModalOpen && (
          <ModalShell open={isCreateExerciseModalOpen} onClose={() => setIsCreateExerciseModalOpen(false)}>
              <h3>Создать упражнение</h3>
              <label>Название упражнения</label>
              <input
                value={newCatalogExerciseName}
                onChange={(e) => setNewCatalogExerciseName(e.target.value)}
                placeholder="Например: Жим лежа"
              />
              <label>Комментарий (опционально)</label>
              <input
                value={newCatalogExerciseMeta}
                onChange={(e) => setNewCatalogExerciseMeta(e.target.value)}
                placeholder="Например: гриф + 20 кг"
              />
              <div className="row">
                <button onClick={createCatalogExercise}>Создать</button>
                <button className="ghost-btn" onClick={() => setIsCreateExerciseModalOpen(false)}>Отмена</button>
              </div>
          </ModalShell>
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
